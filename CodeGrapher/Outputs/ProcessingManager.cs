using System.Threading.Channels;
using CodeGrapher.Entities;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class ProcessingManager(IEnumerable<Relationship> relationships, Neo4jProcessor processor)
{
    public int TotalItems { get; set; }

    public async Task ProcessAsync()
    {
        var nodes = relationships
            .SelectMany<Relationship, Node>(relationship => [relationship.From, relationship.To])
            .DistinctBy(node => node.FullName)
            .ToDictionary(node => $"{node.Label}:{node.FullName}");
        
        
        using var progressBar = new ProgressBar(1, "Write to database...");

        await processor.WriteNodesAsync(nodes.Values);
        await processor.WriteRelationships(relationships);
    }
}