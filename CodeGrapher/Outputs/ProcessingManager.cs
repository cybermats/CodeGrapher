using System.Threading.Channels;
using CodeGrapher.Entities;

namespace CodeGrapher.Outputs;

public class ProcessingManager(Channel<Relationship> channel)
{
    public IEnumerable<IProcessor> Processors { get; init; } = new List<IProcessor>();
    
    public async Task ProcessAsync()
    {
        while (await channel.Reader.WaitToReadAsync())
        {
            var message = await channel.Reader.ReadAsync();
            foreach (var processor in Processors)
                await processor.WriteAsync(message);
        }

    }
}