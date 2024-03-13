using System.Threading.Channels;
using CodeGrapher.Entities;
using CodeGrapher.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeGrapher.Analysis;

public sealed class Analyzer : IDisposable
{
    private readonly MSBuildWorkspace _workspace = MSBuildWorkspace.Create();
    private Solution? _solution;
    private IEnumerable<Project> _projects = Array.Empty<Project>();
    private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();
    private readonly Dictionary<ProjectId, string> _projectNameLookup = new();

    private readonly ChannelWriter<Relationship> _channelWriter;
    private readonly string? _filename;

    public Analyzer(Channel<Relationship> channel, string? filename)
    {
        MSBuildLocator.RegisterDefaults();
        _channelWriter = channel.Writer;
        _filename = filename;
    }

    private async Task OpenAsync()
    {
        if (string.IsNullOrWhiteSpace(_filename))
            throw new InvalidOperationException("No filename specified");

        if (_solution is not null)
            return;

        if (_filename.EndsWith("sln"))
        {
            _solution = await _workspace.OpenSolutionAsync(_filename);
            _projects = _solution.Projects;
        }
        else if (_filename.EndsWith("csproj"))
        {
            var project = await _workspace.OpenProjectAsync(_filename);
            _solution = project.Solution;
            _projects = _solution.Projects;
        }
        else
        {
            throw new Exception("Unknown file type");
        }
    }

    private async Task PrepareAsync()
    {
        if (_projects is null)
            throw new InvalidOperationException("Open haven't been called");
        foreach (var project in _projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
                continue;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                _models[syntaxTree] = compilation.GetSemanticModel(syntaxTree);
            }

            _projectNameLookup[project.Id] = project.Name;
        }
    }

    private async Task Analyze()
    {
        foreach (var project in _projects)
        {
            var projectDirectory = project.FilePath?.ContainingDirectory() ?? "";

            foreach (var projectReference in project.ProjectReferences)
            {
                var referencedProjectName = _projectNameLookup[projectReference.ProjectId];
                var message = $"({project.Name} : Project) -DEPENDS_ON-> ({referencedProjectName} : Project)";
                await _channelWriter.WriteAsync(new Relationship(message));
            }

            foreach (var document in project.Documents)
            {
                var filepath = Path.GetRelativePath(projectDirectory, document.FilePath ?? "");
                if (string.IsNullOrWhiteSpace(filepath)) continue;
                var message = $"({project.Name} : Project) -CONTAINS-> ({filepath} : File)";
                await _channelWriter.WriteAsync(new Relationship(message));
            }

            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
                continue;

            var typeWalker = new TypeWalker(_models, projectDirectory);
            foreach (var tree in compilation.SyntaxTrees)
            {
                typeWalker.Visit(await tree.GetRootAsync());
            }

            foreach (var item in typeWalker.Items)
                await _channelWriter.WriteAsync(item);

        }

    }

    public async Task RunAsync()
    {
        await OpenAsync();
        await PrepareAsync();
        await Analyze();
        _channelWriter.Complete();
    }
    

    void IDisposable.Dispose()
    {
        _workspace.Dispose();
    }
}