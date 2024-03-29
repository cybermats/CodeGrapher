﻿using System.Text;
using CodeGrapher.Utils;
using Microsoft.CodeAnalysis;

namespace CodeGrapher.Entities;

public abstract class Node(string? label, string? fullName, string? name)
{
    protected bool Equals(Node other)
    {
        return FullName == other.FullName && Label == other.Label;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Node)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FullName, Label);
    }

    public string FullName { get; } = fullName ?? throw new ArgumentNullException(nameof(fullName));
    public string Label { get; } = label ?? throw new ArgumentNullException(nameof(label));
    protected string Name = name ?? throw new ArgumentNullException(nameof(name));

    protected virtual StringBuilder FetchProperties(StringBuilder sb)
    {
        sb.Append("fullname: \"");
        sb.Append(FullName);
        sb.Append("\", name: \"");
        sb.Append(Name);
        sb.Append("\"");
        return sb;
    }

    public string ToFullString(string variable)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        return $"({variable}:{Label} {{ {FetchProperties(new StringBuilder())} }})";
    }

    public string ToShortString(string variable)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        return $"({variable}:{Label} {{ fullname: \"{FullName}\" }})";
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
}

public class ProjectNode : Node
{
    public ProjectNode(string Name) : base("Project", Name, Name)
    {
    }

    public ProjectNode(Project project) : base("Project", project.Name, project.Name)
    {
    }
}

public class SymbolNode : Node
{
    private readonly string _attributes;
    private readonly string _namespace;

    protected SymbolNode(string? label, ISymbol? symbol) : base(label, symbol?.ToString(), symbol?.Name)
    {
        if (label is null)
            throw new ArgumentNullException(nameof(label));
        
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        if (symbol is IMethodSymbol methodSymbol)
            if (methodSymbol.MethodKind == MethodKind.Constructor)
                Name = symbol.ContainingType.Name;

        var attributes = symbol.GetAttributes();
        _attributes = string.Join(",", attributes.Select(a => $"\"{a?.AttributeClass?.Name ?? "Unknown"}\"")
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

public class InterfaceNode(INamedTypeSymbol symbol) : SymbolNode("Interface", symbol);

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