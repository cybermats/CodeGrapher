﻿using System.Threading.Channels;
using CodeGrapher.Entities;
using CodeGrapher.Utils;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using ShellProgressBar;

namespace CodeGrapher.Analysis;

public sealed class Analyzer : IDisposable
{
    private readonly ChannelWriter<Relationship> _channelWriter;
    private readonly string? _filename;
    private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();
    private readonly Dictionary<ProjectId, string> _projectNameLookup = new();
    private readonly MSBuildWorkspace _workspace = MSBuildWorkspace.Create();

    private int _numSyntaxTrees;
    private IEnumerable<Project> _projects = Array.Empty<Project>();
    private Solution? _solution;

    public int TotalItems { get; private set; }

    public Analyzer(Channel<Relationship> channel, string? filename)
    {
        MSBuildLocator.RegisterDefaults();
        _channelWriter = channel.Writer;
        _filename = filename;
    }


    void IDisposable.Dispose()
    {
        _workspace.Dispose();
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
                _numSyntaxTrees++;
                _models[syntaxTree] = compilation.GetSemanticModel(syntaxTree);
            }

            _projectNameLookup[project.Id] = project.Name;
        }
    }

    private async Task WriteAsync(Relationship relationship)
    {
        await _channelWriter.WriteAsync(relationship);
        TotalItems++;
    }

    private async Task Analyze()
    {
        SolutionNode? solutionNode = null;
        if (_solution is not null && _solution.FilePath != null)
            solutionNode = new SolutionNode(Path.GetFileNameWithoutExtension(_solution.FilePath));

        var solutionDirectory = _solution?.FilePath?.ContainingDirectory() ?? null;

        var options = new ProgressBarOptions();
        using var progressBar = new ProgressBar(_numSyntaxTrees, "Starting...", options);

        foreach (var project in _projects)
        {
            var projectNode = new ProjectNode(project);
            if (solutionNode is not null)
                await WriteAsync(new Relationship(solutionNode, projectNode,
                    RelationshipType.Have));

            var projectDirectory = project.FilePath?.ContainingDirectory() ?? "";

            foreach (var projectReference in project.ProjectReferences)
            {
                var referencedProjectName = _projectNameLookup[projectReference.ProjectId];
                var refProjectNode = new ProjectNode(referencedProjectName);
                await WriteAsync(new Relationship(projectNode, refProjectNode,
                    RelationshipType.DependsOn));
            }

            foreach (var document in project.Documents)
            {
                if (document.Folders.FirstOrDefault() == "obj")
                    continue;

                var filepath = Path.GetRelativePath(solutionDirectory ?? projectDirectory, document.FilePath ?? "");
                var fileNode = new FileNode(filepath);
                if (string.IsNullOrWhiteSpace(filepath)) continue;
                await WriteAsync(new Relationship(projectNode, fileNode, RelationshipType.Contains));
            }

            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
                continue;

            var typeWalker = new TypeWalker(_models, solutionDirectory ?? projectDirectory);
            foreach (var tree in compilation.SyntaxTrees)
            {
                typeWalker.Visit(await tree.GetRootAsync());
                progressBar.Tick(tree.FilePath);
            }

            foreach (var item in typeWalker.Items)
                await WriteAsync(item);
        }
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Opening workspace...");
        await OpenAsync();
        Console.WriteLine("Preparing workspace...");
        await PrepareAsync();
        Console.WriteLine("Analyzing...");
        await Analyze();
        _channelWriter.Complete();
    }
}