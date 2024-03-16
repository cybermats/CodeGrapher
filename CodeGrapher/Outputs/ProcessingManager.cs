using System.Threading.Channels;
using CodeGrapher.Entities;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class ProcessingManager(IEnumerable<Triple> triples, Neo4jProcessor processor)
{
    public int TotalItems { get; set; }

    public async Task ProcessAsync()
    {
        
        
        using var progressBar = new ProgressBar(1, "Write to database...");

        await processor.WriteNodesAsync(triples);
        await processor.WriteRelationships(triples);
    }
}