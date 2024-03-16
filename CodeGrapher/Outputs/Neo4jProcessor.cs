using CodeGrapher.Entities;
using Neo4j.Driver;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class Neo4jProcessor : IDisposable, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly IAsyncSession _session;
    private readonly ProgressBar _progressBar = new ProgressBar(4, "Saving to database...");


    public Neo4jProcessor(string uri, string user, string password)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        _session = _driver.AsyncSession();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _session.DisposeAsync();
        await _driver.DisposeAsync();
    }

    void IDisposable.Dispose()
    {
        _session.Dispose();
        _driver.Dispose();
    }

    public async Task WriteNodesAsync(IEnumerable<Triple> triples)
    {
        var nodes = triples
            .SelectMany<Triple, Node>(relationship => [relationship.From, relationship.To])
            .DistinctBy(node => node.FullName)
            .ToDictionary(node => $"{node.Label}:{node.FullName}")
            .Values
            .ToList();
       
        using var pbar = _progressBar.Spawn(nodes.Count(), "Saving nodes...");
        const int batchSize = 50;
        var processed = 0;
        while (nodes.Any())
        {
            var creates = nodes
                .Take(batchSize)
                .Select(n => n.ToFullString(""));
            var full = string.Join(", ", creates);
            var query = $"CREATE {full}";
            await _session.RunAsync(query);
            nodes = nodes.Skip(batchSize).ToList();
            pbar.Tick(processed += creates.Count());
        }
        _progressBar.Tick();
    }

    public async Task WriteRelationships(IEnumerable<Triple> relationships)
    {
        using var pbar = _progressBar.Spawn(relationships.Count(), "Saving relationships...");
        
        foreach (var relationship in relationships)
        {
            await _session.RunAsync(relationship.ToMergeString());
            pbar.Tick();
        }
        _progressBar.Tick();
    }

    public async Task WriteAsync(Triple triple)
    {
        await _session.RunAsync(triple.ToString());
    }

    public async Task InitializeAsync()
    {
        _progressBar.Tick("Deleting old nodes...");
        await _session.RunAsync("MATCH (n) DETACH DELETE n;");
        _progressBar.Tick("Old nodes deleted.");
    }
}