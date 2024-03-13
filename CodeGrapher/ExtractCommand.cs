using System.Threading.Channels;
using Spectre.Console.Cli;

namespace CodeGrapher;

public class ExtractCommand : AsyncCommand<ExtractCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<filepath>")]
        public string? FilePath { get; init; }
    }


    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        Console.WriteLine($"File path: {settings.FilePath}");


        var channel = Channel.CreateUnbounded<string>();
        using var analyser = new Analyzer(channel, settings.FilePath);
        await analyser.RunAsync();
        await new LinkConsoleWriter(channel).RunAsync();
        await channel.Reader.Completion;
        return 0;
    }
}