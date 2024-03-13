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

        var classDeclaredIn =
            classSymbol.Locations
                .Where(l => l.Kind == LocationKind.SourceFile)
                .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                .Select(filepath => $"({classSymbol} : Class) -DECLARED_IN-> ({filepath} : File)")
                .Select(m => new Relationship(m));

        Items.AddRange(classDeclaredIn);


        var classHaveMembers =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .Select(m => $"({classSymbol} : Class) -HAVE-> ({m} : {m.Label()})")
                .Select(m => new Relationship(m));

        Items.AddRange(classHaveMembers);

        var membersDeclaredIn =
            classSymbol.GetMembers().OfType<IMethodSymbol>()
                .SelectMany(member => member.Locations
                    .Where(l => l.Kind == LocationKind.SourceFile)
                    .Select(l => Path.GetRelativePath(projectRootPath, l.SourceTree?.FilePath ?? ""))
                    .Select(filepath => $"({member} : {member.Label()}) -DECLARED_IN-> ({filepath} : File)"))
                .Select(m => new Relationship(m));

        Items.AddRange(membersDeclaredIn);

        var classOfType =
            classSymbol.AllInterfaces
                .Select(i => $"({i} : Interface) -OF_TYPE-> ({classSymbol} : Class)")
                .Select(m => new Relationship(m));

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
                .Select(o => $"({o.InterfaceMember} : {o.InterfaceMember.Label()}) -IMPLEMENTED_BY-> ({o.ImplementedMethod} : {o.ImplementedMethod!.Label()})")
                .Select(m => new Relationship(m));

        Items.AddRange(membersImplementedBy);

        base.VisitClassDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        base.VisitStructDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        var symbol = models[node.SyntaxTree].GetDeclaredSymbol(node);
        var members = symbol.GetMembers().OfType<IMethodSymbol>();

        var relationships = members
            .Select(member => $"({symbol} : Interface) -HAVE-> ({member} : {member.Label()})")
            .Select(m => new Relationship(m));

        Items.AddRange(relationships);

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
                .SelectMany(si => si.Symbol.DeclaringSyntaxReferences
                    .Select(declSynt => declSynt.GetSyntax() as MethodDeclarationSyntax)
                    .Where(ms => ms is not null)
                )
                .Select(ms => models[ms.SyntaxTree].GetDeclaredSymbol(ms))
                .Select(ms =>
                    $"({methodDeclaration} : {methodDeclaration.Label()}) -INVOKE-> ({ms} : {ms.Label()})")
                .Select(m => new Relationship(m));
            
        Items.AddRange(methodInvokes);

        var methodConstructs =
            node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Select(expr => model.GetSymbolInfo(expr))
                .Where(si => si.Symbol is not null)
                .SelectMany(si =>
                {
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
                    {
                        return new[]
                        {
                            $"({methodDeclaration} : {methodDeclaration.Label()}) -INVOKE-> ({symbol} : Constructor)",
                            $"({methodDeclaration} : {methodDeclaration.Label()}) -CONSTRUCT-> ({symbol.ContainingType} : Class)",
                        };
                    }
                    if (syntax is ClassDeclarationSyntax)
                    {
                        return new[]
                        {
                            $"({methodDeclaration} : {methodDeclaration.Label()}) -CONSTRUCT-> ({symbol} : Class)",
                        };
                    }

                    return Array.Empty<string>();
                })
                .Select(m => new Relationship(m));
        
        Items.AddRange(methodConstructs);
        
        
        base.VisitMethodDeclaration(node);
    }
}