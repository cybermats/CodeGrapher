namespace CodeGrapher.Entities;

public class Triple(Node from, Node to, Relationship relationship)
{
    public Node From { get; } = from;
    public Node To { get; } = to;
    public Relationship Relationship { get; } = relationship;

    public string ToMergeString()
    {
        return
            $"MATCH {From.ToShortString("from")}, {To.ToShortString("to")} MERGE (from) {Relationship.ToCypher("r")} (to);";
    }

    public string ToShortString(string from, string to, string rel)
    {
        return
            $"({from}) {Relationship.ToCypher(rel)} ({to})";
    }
}