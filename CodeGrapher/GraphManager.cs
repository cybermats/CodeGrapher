using System.Threading.Channels;
using CodeGrapher.Analysis;
using CodeGrapher.Entities;
using CodeGrapher.Outputs;
using ShellProgressBar;

namespace CodeGrapher;

public class GraphManager
{
    private readonly string _filepath;
    private readonly string _password;
    private readonly string _uri;
    private readonly string _user;
    private readonly IndeterminateProgressBar _progressBar;

    public GraphManager(string? filepath, string? uri, string? user, string? password)
    {
        _filepath = filepath ?? throw new ArgumentNullException(nameof(filepath));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _progressBar = new IndeterminateProgressBar("Starting up...");
    }

    public async Task RunAsync()
    {
        
        using var analyser = new Analyzer(_filepath, _progressBar);
        var analysis = analyser.RunAsync();

        using var neo4JProcessor = new Neo4jProcessor(_uri, _user, _password, _progressBar);
        var neo4JInit = neo4JProcessor.InitializeAsync();

        await Task.WhenAll(analysis, neo4JInit);

        try
        {
            var mgr = new ProcessingManager(analyser.Relationships, neo4JProcessor);
            await mgr.ProcessAsync();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        _progressBar.Finished();
    }
}