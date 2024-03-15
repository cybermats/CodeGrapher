using System.Threading.Channels;
using CodeGrapher.Entities;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class ProcessingManager(Channel<Relationship> channel)
{
    public IEnumerable<IProcessor> Processors { get; init; } = new List<IProcessor>();
    public int TotalItems { get; set; }

    public async Task ProcessAsync()
    {
        using var progressBar = new ProgressBar(1, "Write to database...");
        while (await channel.Reader.WaitToReadAsync())
        {
            var message = await channel.Reader.ReadAsync();
            foreach (var processor in Processors)
                await processor.WriteAsync(message);
            progressBar.Tick();
            progressBar.MaxTicks = TotalItems;
        }
    }
}