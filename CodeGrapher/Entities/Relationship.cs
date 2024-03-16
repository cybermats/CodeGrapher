namespace CodeGrapher.Entities;

public class Relationship(Relationship.RelationshipType label)
{
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
        Inherits
    }

    public static Relationship DependsOn() => new Relationship(RelationshipType.DependsOn);
    public static Relationship Contains() => new Relationship(RelationshipType.Contains);
    public static Relationship DeclaredIn() => new Relationship(RelationshipType.DeclaredIn);
    public static Relationship Have() => new Relationship(RelationshipType.Have);
    public static Relationship OfType() => new Relationship(RelationshipType.OfType);
    public static Relationship ImplementedAs() => new Relationship(RelationshipType.ImplementedAs);
    public static Relationship Invoke() => new Relationship(RelationshipType.Invoke);
    public static Relationship Construct() => new Relationship(RelationshipType.Construct);
    public static Relationship Inherits() => new Relationship(RelationshipType.Inherits);
    
    public RelationshipType Label { get; } = label;
    
    
    public string ToCypher(string variable)
    {
        var label = Label switch
        {
            Relationship.RelationshipType.DependsOn => "DEPENDS_ON",
            Relationship.RelationshipType.Contains => "CONTAINS",
            Relationship.RelationshipType.DeclaredIn => "DECLARED_IN",
            Relationship.RelationshipType.Have => "HAVE",
            Relationship.RelationshipType.OfType => "OF_TYPE",
            Relationship.RelationshipType.ImplementedAs => "IMPLEMENTED_AS",
            Relationship.RelationshipType.Invoke => "INVOKE",
            Relationship.RelationshipType.Construct => "CONSTRUCT",
            Relationship.RelationshipType.Inherits => "INHERITS",
            _ => throw new ArgumentOutOfRangeException(nameof(Label), Label, null)
        };
        return $"-[{variable}:{label}]->";
    }
}