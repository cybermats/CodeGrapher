using Spectre.Console.Cli;

namespace CodeGrapher;

public class ExtractCommand : Command<ExtractCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<filepath>")]
        public string? FilePath { get; init; }
    }


    public override int Execute(CommandContext context, Settings settings)
    {
        Console.WriteLine($"File path: {settings.FilePath}");
        return 0;
    }
}