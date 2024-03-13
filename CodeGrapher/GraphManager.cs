using System.ComponentModel;
using System.Threading.Channels;
using CodeGrapher.Analysis;
using CodeGrapher.Entities;
using CodeGrapher.LinkConsumers;
using Spectre.Console.Cli;

namespace CodeGrapher;

public class GraphManager : AsyncCommand<GraphManager.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<filepath>")]
        public string? FilePath { get; init; }
        
        [CommandOption("--to-console")]
        [DefaultValue(false)]
        public bool PrintToConsole { get; init; }
    }

    private readonly Channel<Relationship> _channel = Channel.CreateUnbounded<Relationship>();
    
    
    

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        Console.WriteLine($"File path: {settings.FilePath}");

        using var analyser = new Analyzer(_channel, settings.FilePath);
        var analysis = analyser.RunAsync();

        ILinkConsumer? consumer = null;
        if (settings.PrintToConsole)
            consumer = new LinkConsoleWriter(_channel);

        if (consumer is null)
            return 1;
        
        var consumption = consumer.RunAsync();

        await Task.WhenAll(analysis, consumption, _channel.Reader.Completion);
        return 0;
    }
}