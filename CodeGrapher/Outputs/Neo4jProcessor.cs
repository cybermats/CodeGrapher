using System.Drawing.Printing;
using CodeGrapher.Entities;
using Neo4j.Driver;
using ShellProgressBar;

namespace CodeGrapher.Outputs;

public class Neo4jProcessor : IDisposable, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly IProgressBar _mainProgressBar;


    public Neo4jProcessor(string uri, string user, string password, string database, IProgressBar mainProgressBar)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        _mainProgressBar = mainProgressBar;
        _database = database;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _driver.DisposeAsync();
    }

    void IDisposable.Dispose()
    {
        _driver.Dispose();
    }

    public async Task WriteNodesAsync(IEnumerable<Triple> triples)
    {
        _mainProgressBar.Tick("Saving nodes...");

        var nodes = triples
            .SelectMany<Triple, Node>(relationship => [relationship.From, relationship.To])
            .DistinctBy(node => node.FullName)
            .ToDictionary(node => $"{node.Label}:{node.FullName}")
            .Values
            .ToList();
       
        using var pbar = _mainProgressBar.Spawn(nodes.Count(), "Saving nodes...");
        const int batchSize = 50;
        var processed = 0;
        

        foreach (var chunk in nodes.Chunk(batchSize))
        {
            await using var session = _driver.AsyncSession(o => o.WithDatabase(_database));
            
            var creates = chunk
                .Select(n => n.ToFullString(""));
            var full = string.Join(", ", creates);
            var query = $"CREATE {full}";
            await session.RunAsync(query);
            pbar.Tick(processed += chunk.Count());
        
        }
        
    }

    public async Task WriteRelationships(IEnumerable<Triple> triples)
    {
        _mainProgressBar.Tick("Saving relationships...");

        
        using var pbar = _mainProgressBar.Spawn(triples.Count(), "Saving relationships...");

        var groupedBy = triples
            .GroupBy(triple => triple.From);

        var processed = 0;
        const int batchSize = 50;
        

        
        foreach (var group in groupedBy)
        {
            pbar.Tick(processed, $"{group.Key.ToString()} - {group.Count()}");

            var fromQuery = group.Key.ToShortString("from");
            

            foreach (var chunk in group.Chunk(batchSize))
            {
                var toQueries = new List<string>();
                var relQueries = new List<string>();

                foreach (var (triple, i) in chunk.Select((triple, i) => (triple, i)))
                {
                    var toVariable = $"to{i}";
                    var relVariable = $"rel{i}";
                    toQueries.Add(triple.To.ToShortString(toVariable));
                    relQueries.Add(triple.ToShortString("from", toVariable, relVariable));
                }

                var toQuery = string.Join(",", toQueries);
                var relQuery = string.Join(",", relQueries);

                var query = $"MATCH {fromQuery}, {toQuery} CREATE {relQuery}";
                try
                {
                    await using var session = _driver.AsyncSession(o => o.WithDatabase(_database));
                    await session.RunAsync(query);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(query);
                    Console.Error.WriteLine(e);
                    throw;
                }

            }
            processed += group.Count();                
        }

    }

    public async Task InitializeAsync()
    {
        _mainProgressBar.Tick("Deleting old nodes...");

        await using var session = _driver.AsyncSession(o => o.WithDatabase(_database));
        await session.RunAsync("MATCH (n) DETACH DELETE n;");
    }
}