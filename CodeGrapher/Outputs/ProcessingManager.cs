using System.Threading.Channels;
using CodeGrapher.Entities;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class ProcessingManager(Channel<Relationship> channel, Neo4jProcessor processor)
{
    public int TotalItems { get; set; }

    public async Task ProcessAsync()
    {
        using var progressBar = new ProgressBar(1, "Write to database...");
        while (await channel.Reader.WaitToReadAsync())
        {
            var message = await channel.Reader.ReadAsync();
            await processor.WriteAsync(message);
            progressBar.Tick();
            progressBar.MaxTicks = TotalItems;
        }
    }
}