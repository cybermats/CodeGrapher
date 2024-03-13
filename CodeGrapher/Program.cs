using Spectre.Console.Cli;

namespace CodeGrapher;

class Program
{
    static int Main(string[] args)
    {
        var app = new CommandApp<ExtractCommand>();
        return app.Run(args);
    }
}