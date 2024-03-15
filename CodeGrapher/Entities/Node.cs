using System.Text;
using CodeGrapher.Utils;
using Microsoft.CodeAnalysis;

namespace CodeGrapher.Entities;

public abstract class Node
{
    protected readonly string FullName;
    protected readonly string Label;
    protected string Name;

    protected Node(string? label, string? fullName, string? name)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    protected virtual int Pk => FullName.GetHashCode();

    protected virtual StringBuilder FetchProperties(StringBuilder sb)
    {
        sb.Append("pk: ");
        sb.Append(Pk);
        sb.Append(", fullname: \"");
        sb.Append(FullName);
        sb.Append("\", name: \"");
        sb.Append(Name);
        sb.Append("\"");
        return sb;
    }

    public string ToString(string variable = "")
    {
        return $"({variable}:{Label} {{ {FetchProperties(new StringBuilder())} }})";
    }

    public override string ToString()
    {
        return $"{nameof(Label)}: {Label}, {nameof(FullName)}: {FullName}";
    }
}

public class FileNode(string fullName) : Node("File", fullName, Path.GetFileName(fullName))
{
}

public class SolutionNode : Node
{
    public SolutionNode(string? name) : base("Solution", name, name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));
    }

    protected override StringBuilder FetchProperties(StringBuilder sb)
    {
        sb.Append("pk: ");
        sb.Append(Pk);
        sb.Append(", name: \"");
        sb.Append(Name);
        sb.Append("\"");
        return sb;
    }
}

public class ProjectNode : Node
{
    public ProjectNode(string Name) : base("Project", Name, Name)
    {
    }

    public ProjectNode(Project project) : base("Project", project.Name, project.Name)
    {
    }

    protected override StringBuilder FetchProperties(StringBuilder sb)
    {
        sb.Append("pk: ");
        sb.Append(Pk);
        sb.Append(", name: \"");
        sb.Append(Name);
        sb.Append("\"");
        return sb;
    }
}

public class SymbolNode : Node
{
    private readonly string _attributes;
    private readonly string _namespace;

    public SymbolNode(string label, ISymbol? symbol) : base(label, symbol?.ToString(), symbol?.Name)
    {
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        if (symbol is IMethodSymbol methodSymbol)
            if (methodSymbol.MethodKind == MethodKind.Constructor)
                Name = symbol.ContainingType.Name;

        var attributes = symbol.GetAttributes();
        _attributes = string.Join(",", attributes.Select(a => $"\"{a.AttributeClass.Name}\"")
            .Select(a => a.Replace("Attribute", "")));
        _namespace = symbol.ContainingNamespace.Name;
    }

    protected override StringBuilder FetchProperties(StringBuilder sb)
    {
        base.FetchProperties(sb);
        sb.Append(", attributes: [");
        sb.Append(_attributes);
        sb.Append("], namespace: \"");
        sb.Append(_namespace);
        sb.Append("\"");
        return sb;
    }
}

public class ClassNode : SymbolNode
{
    public ClassNode(INamedTypeSymbol? classSymbol) : base("Class", classSymbol)
    {
        if (classSymbol is null)
            throw new ArgumentNullException(nameof(classSymbol));
    }
}

public class InterfaceNode : SymbolNode
{
    public InterfaceNode(INamedTypeSymbol symbol) : base("Interface", symbol)
    {
    }
}

public class MethodNode : SymbolNode
{
    private readonly string _arguments;
    private readonly string _returnType;

    public MethodNode(IMethodSymbol? methodSymbol) : base(methodSymbol?.Label(), methodSymbol)
    {
        if (methodSymbol is null)
            throw new ArgumentNullException(nameof(methodSymbol));

        _arguments = string.Join(",", methodSymbol.Parameters.Select(p => $"\"{p.Type} {p.Name}\""));
        _returnType = methodSymbol.ReturnType?.ToString() ??
                      throw new ArgumentNullException(nameof(methodSymbol), "methodSymbol.ReturnType returned null");
    }

    protected override int Pk => $"{FullName}{_arguments}{_returnType}".GetHashCode();

    protected override StringBuilder FetchProperties(StringBuilder sb)
    {
        base.FetchProperties(sb);
        sb.Append(", returnType: \"");
        sb.Append(_returnType);
        sb.Append("\", arguments: [");
        sb.Append(_arguments);
        sb.Append("]");
        return sb;
    }
}