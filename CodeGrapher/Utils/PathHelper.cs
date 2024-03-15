namespace CodeGrapher.Utils;

public static class PathHelper
{
    public static string? ContainingDirectory(this string filepath)
    {
        return Directory.GetParent(filepath)?.FullName;
    }
}