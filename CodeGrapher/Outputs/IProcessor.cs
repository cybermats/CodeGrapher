using CodeGrapher.Entities;

namespace CodeGrapher.Outputs;

public interface IProcessor
{
    Task WriteAsync(Relationship relationship);
}