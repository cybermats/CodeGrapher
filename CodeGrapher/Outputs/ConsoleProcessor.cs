using CodeGrapher.Entities;

namespace CodeGrapher.Outputs;

public class ConsoleProcessor : IProcessor
{
    public Task WriteAsync(Relationship relationship)
    {
        Console.WriteLine(relationship.ToString());
        return Task.CompletedTask;
    }
}