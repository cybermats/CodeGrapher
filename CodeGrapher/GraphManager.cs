using System.Threading.Channels;
using CodeGrapher.Analysis;
using CodeGrapher.Entities;
using CodeGrapher.Outputs;
using ShellProgressBar;

namespace CodeGrapher;

public class GraphManager: IDisposable
{
    private readonly string _filepath;
    private readonly string _password;
    private readonly string _uri;
    private readonly string _user;
    private readonly string _database;
    private readonly ProgressBar _progressBar;

    public GraphManager(string? filepath, string? uri, string? user, string? password, string? database)
    {
        _filepath = filepath ?? throw new ArgumentNullException(nameof(filepath));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        
        
        _progressBar = new ProgressBar(7, "Starting up...", new ProgressBarOptions()
        {
            DisplayTimeInRealTime = false,
            CollapseWhenFinished = true,
            ProgressCharacter = '-',
        });
    }

    public async Task RunAsync()
    {
        using var analyser = new Analyzer(_filepath, _progressBar);
        var analysis = analyser.RunAsync();

        using var neo4JProcessor = new Neo4jProcessor(_uri, _user, _password, _database, _progressBar);
        var neo4JInit = neo4JProcessor.InitializeAsync();

        await Task.WhenAll(analysis, neo4JInit);

        try
        {
            await neo4JProcessor.WriteNodesAsync(analyser.Relationships);
            await neo4JProcessor.WriteRelationships(analyser.Relationships);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
        
        _progressBar.Tick(_progressBar.MaxTicks, "Done");
    }

    void IDisposable.Dispose()
    {
        _progressBar.Dispose();
    }
}