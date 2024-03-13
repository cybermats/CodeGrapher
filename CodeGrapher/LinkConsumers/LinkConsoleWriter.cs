using System.Threading.Channels;
using CodeGrapher.Entities;

namespace CodeGrapher.LinkConsumers;

public class LinkConsoleWriter(Channel<Relationship> channel) : ILinkConsumer
{
    private readonly ChannelReader<Relationship> _channelReader = channel.Reader;

    public async Task RunAsync()
    {
        while (await _channelReader.WaitToReadAsync())
        {
            var message = await _channelReader.ReadAsync();
            Console.WriteLine(message.name);
        }
    }
}