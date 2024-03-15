namespace CodeGrapher.Entities;

public class Relationship(Node from, Node to, RelationshipType verb)
{
    public override string? ToString()
    {
        return $"MERGE {from.ToString("a")} MERGE {to.ToString("b")} MERGE (a)-[r:{verb.ToCypher()}]->(b)";
    }
}