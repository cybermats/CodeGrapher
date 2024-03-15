namespace CodeGrapher.Entities;

public enum RelationshipType
{
    DependsOn,
    Contains,
    DeclaredIn,
    Have,
    OfType,
    ImplementedAs,
    Invoke,
    Construct,
}

public static class RelationshipTypeExtensions
{
    public static string ToCypher(this RelationshipType relationship)
    {
        return relationship switch
        {
            RelationshipType.DependsOn => "DEPENDS_ON",
            RelationshipType.Contains => "CONTAINS",
            RelationshipType.DeclaredIn => "DECLARED_IN",
            RelationshipType.Have => "HAVE",
            RelationshipType.OfType => "OF_TYPE",
            RelationshipType.ImplementedAs => "IMPLEMENTED_AS",
            RelationshipType.Invoke => "INVOKE",
            RelationshipType.Construct => "CONSTRUCT",
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };
    }
}