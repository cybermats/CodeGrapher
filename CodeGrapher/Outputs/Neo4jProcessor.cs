using CodeGrapher.Entities;
using Neo4j.Driver;

namespace CodeGrapher.Outputs;

public class Neo4jProcessor : IProcessor, IDisposable, IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly IAsyncSession _session;
    
    
    public Neo4jProcessor(string uri, string user, string password)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        _session = _driver.AsyncSession();
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Deleting old nodes...");
        await _session.RunAsync("MATCH (n) DETACH DELETE n;");
        Console.WriteLine("Old nodes deleted.");
    }


    public async Task WriteAsync(Relationship relationship)
    {
        await _session.RunAsync(relationship.ToString());
    }

    void IDisposable.Dispose()
    {
        _session.Dispose();
        _driver.Dispose();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _session.DisposeAsync();
        await _driver.DisposeAsync();
    }
}