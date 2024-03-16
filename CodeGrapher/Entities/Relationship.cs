namespace CodeGrapher.Entities;

public class Relationship(Node from, Node to, RelationshipType verb)
{
    public Node From { get; } = from;
    public Node To { get; } = to;

    public override string? ToString()
    {
        return $"MERGE {From.ToFullString("a")} MERGE {To.ToFullString("b")} MERGE (a)-[r:{verb.ToCypher()}]->(b)";
    }

    public string ToMergeString()
    {
        return
            $"MATCH {From.ToShortString("from")}, {To.ToShortString("to")} MERGE (from)-[r:{verb.ToCypher()}]->(to);";
    }
}