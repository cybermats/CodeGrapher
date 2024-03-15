using System.Threading.Channels;
using CodeGrapher.Entities;
using CodeGrapher.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGrapher.Analysis;

public class TypeWalker(Dictionary<SyntaxTree, SemanticModel> models, string projectRootPath) : CSharpSyntaxWalker
{
    public List<Relationship> Items { get; } = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var classSymbol = models[node.SyntaxTree].GetDeclaredSymbol(node);
        if (classSymbol is null)
        {
            base.VisitClassDeclaration(node);
            return;
        }

        if (classSymbol.BaseType is not null && classSymbol.BaseType.BaseType is not null)
        {
            var baseType = classSymbol.BaseType;

            var relation = new Relationship(new ClassNode(classSymbol), new ClassNode(baseType),
                RelationshipType.Inherits);
            Items.Add(relation);
        }

        var classDeclaredIn =
            classSymbol.Locations
                .Where(l => l.Kind == LocationKind.SourceFile)
                .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                .Select(filepath => new Relationship(new ClassNode(classSymbol), new FileNode(filepath),
                    RelationshipType.DeclaredIn));

        Items.AddRange(classDeclaredIn);

        var classHaveMembers =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Select(m => new Relationship(new ClassNode(classSymbol), new MethodNode(m), RelationshipType.Have));

        Items.AddRange(classHaveMembers);


        var membersDeclaredIn =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .SelectMany(member => member.Locations
                    .Where(l => l.Kind == LocationKind.SourceFile)
                    .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                    .Select(filepath =>
                        new Relationship(new MethodNode(member), new FileNode(filepath), RelationshipType.DeclaredIn)));

        Items.AddRange(membersDeclaredIn);


        var classOfType =
            classSymbol.AllInterfaces
                .Select(interfaceType => new Relationship(new InterfaceNode(interfaceType), new ClassNode(classSymbol),
                    RelationshipType.OfType));

        Items.AddRange(classOfType);


        var membersImplementedBy =
            classSymbol.AllInterfaces
                .SelectMany(interfaceType =>
                    interfaceType.GetMembers().OfType<IMethodSymbol>()
                        .Select(interfaceMember =>
                            new
                            {
                                InterfaceMember = interfaceMember,
                                ImplementedMethod =
                                    classSymbol.FindImplementationForInterfaceMember(interfaceMember) as IMethodSymbol
                            })
                        .Where(o => o.ImplementedMethod != null))
                .Select(o => new Relationship(new MethodNode(o.InterfaceMember), new MethodNode(o.ImplementedMethod),
                    RelationshipType.ImplementedAs));

        Items.AddRange(membersImplementedBy);
        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var interfaceSymbol = models[node.SyntaxTree].GetDeclaredSymbol(node);
        if (interfaceSymbol is not null)
        {
            var members = interfaceSymbol.GetMembers().OfType<IMethodSymbol>();

            var relationships = members
                .Select(member => new Relationship(new InterfaceNode(interfaceSymbol), new MethodNode(member),
                    RelationshipType.Have));

            Items.AddRange(relationships);
        }

        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        base.VisitRecordDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var model = models[node.SyntaxTree];
        var methodDeclaration = model.GetDeclaredSymbol(node);

        var methodInvokes =
            node.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(expr => model.GetSymbolInfo(expr))
                .Where(si => si.Symbol is not null)
                .SelectMany(si =>
                    (si.Symbol?.DeclaringSyntaxReferences ?? throw new InvalidOperationException("No symbol found"))
                    .Select(declSynt => declSynt.GetSyntax() as MethodDeclarationSyntax)
                    .Where(ms => ms is not null)
                )
                .Select(ms => models[ms!.SyntaxTree].GetDeclaredSymbol(ms))
                .Select(ms =>
                    new Relationship(new MethodNode(methodDeclaration), new MethodNode(ms), RelationshipType.Invoke));

        Items.AddRange(methodInvokes);


        var methodConstructs =
            node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Select(expr => model.GetSymbolInfo(expr))
                .Where(si => si.Symbol is not null)
                .SelectMany(si =>
                {
                    if (si.Symbol is null)
                        throw new Exception("could not find Symbol");
                    var references = si.Symbol.DeclaringSyntaxReferences;
                    if (references.IsEmpty)
                        references = si.Symbol.ContainingType.DeclaringSyntaxReferences;
                    return references;
                })
                .Select(declRef => declRef.GetSyntax())
                .SelectMany(syntax =>
                {
                    var symbol = models[syntax.SyntaxTree].GetDeclaredSymbol(syntax);
                    if (syntax is ConstructorDeclarationSyntax)
                        return new[]
                        {
                            new Relationship(new MethodNode(methodDeclaration), new MethodNode(symbol as IMethodSymbol),
                                RelationshipType.Invoke),
                            new Relationship(new MethodNode(methodDeclaration), new ClassNode(symbol?.ContainingType),
                                RelationshipType.Construct)
                        };

                    if (syntax is ClassDeclarationSyntax)
                        return new[]
                        {
                            new Relationship(new MethodNode(methodDeclaration),
                                new ClassNode(symbol as INamedTypeSymbol), RelationshipType.Construct)
                        };

                    return Array.Empty<Relationship>();
                });

        Items.AddRange(methodConstructs);

        base.VisitMethodDeclaration(node);
    }
}