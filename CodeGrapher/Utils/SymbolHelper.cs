using Microsoft.CodeAnalysis;

namespace CodeGrapher.Utils;

public static class SymbolHelper
{
    public static string Label(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.MethodKind switch
        {
            MethodKind.Ordinary => "Method",
            _ => methodSymbol.MethodKind.ToString()
        };
    }
}