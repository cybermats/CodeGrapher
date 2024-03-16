using CodeGrapher.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGrapher.Analysis;

public class TypeWalker(Dictionary<SyntaxTree, SemanticModel> models, string projectRootPath) : CSharpSyntaxWalker
{
    public List<Triple> Items { get; } = new();

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

            var relation = new Triple(new ClassNode(classSymbol), new ClassNode(baseType),
                Relationship.Inherits());
            Items.Add(relation);
        }

        var classDeclaredIn =
            classSymbol.Locations
                .Where(l => l.Kind == LocationKind.SourceFile)
                .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                .Select(filepath => new Triple(new ClassNode(classSymbol), new FileNode(filepath),
                    Relationship.DeclaredIn()));

        Items.AddRange(classDeclaredIn);

        var classHaveMembers =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Select(m => new Triple(new ClassNode(classSymbol), new MethodNode(m), Relationship.Have()));

        Items.AddRange(classHaveMembers);


        var membersDeclaredIn =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .SelectMany(member => member.Locations
                    .Where(l => l.Kind == LocationKind.SourceFile)
                    .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                    .Select(filepath =>
                        new Triple(new MethodNode(member), new FileNode(filepath), Relationship.DeclaredIn())));

        Items.AddRange(membersDeclaredIn);


        var classOfType =
            classSymbol.AllInterfaces
                .Select(interfaceType => new Triple(new ClassNode(classSymbol),new InterfaceNode(interfaceType), 
                    Relationship.OfType()));

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
                .Select(o => new Triple(new MethodNode(o.InterfaceMember), new MethodNode(o.ImplementedMethod),
                    Relationship.ImplementedAs()));

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
                .Select(member => new Triple(new InterfaceNode(interfaceSymbol), new MethodNode(member),
                    Relationship.Have()));

            Items.AddRange(relationships);
            
            
            var interfaceDeclaredIn =
                interfaceSymbol.Locations
                    .Where(l => l.Kind == LocationKind.SourceFile)
                    .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                    .Select(filepath => new Triple(new InterfaceNode(interfaceSymbol), new FileNode(filepath),
                        Relationship.DeclaredIn()));

            Items.AddRange(interfaceDeclaredIn);
            
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
                    new Triple(new MethodNode(methodDeclaration), new MethodNode(ms), Relationship.Invoke()));

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
                            new Triple(new MethodNode(methodDeclaration), new MethodNode(symbol as IMethodSymbol),
                                Relationship.Invoke()),
                            new Triple(new MethodNode(methodDeclaration), new ClassNode(symbol?.ContainingType),
                                Relationship.Construct())
                        };

                    if (syntax is ClassDeclarationSyntax)
                        return new[]
                        {
                            new Triple(new MethodNode(methodDeclaration),
                                new ClassNode(symbol as INamedTypeSymbol), Relationship.Construct())
                        };

                    return Array.Empty<Triple>();
                });

        Items.AddRange(methodConstructs);

        base.VisitMethodDeclaration(node);
    }
}