using System.Threading.Channels;

namespace CodeGrapher;

public class LinkConsoleWriter(Channel<string> channel) : ILinkConsumer
{
    private readonly ChannelReader<string> _channelReader = channel.Reader;

    public async Task RunAsync()
    {
        while (await _channelReader.WaitToReadAsync())
        {
            var message = await _channelReader.ReadAsync();
            Console.WriteLine(message);
        }
    }
}