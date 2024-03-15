﻿using System.Threading.Channels;
using CodeGrapher.Analysis;
using CodeGrapher.Entities;
using CodeGrapher.Outputs;

namespace CodeGrapher;

public class GraphManager
{
    private readonly Channel<Relationship> _channel = Channel.CreateUnbounded<Relationship>();
    private readonly bool _enableConsole;
    private readonly string _filepath;
    private readonly string _password;
    private readonly string _uri;
    private readonly string _user;

    public GraphManager(string? filepath, string? uri, string? user, string? password, bool enableConsole)
    {
        _filepath = filepath ?? throw new ArgumentNullException(nameof(filepath));
        _uri = uri ?? throw new ArgumentNullException(nameof(uri));
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _enableConsole = enableConsole;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Starting up...");
        using var analyser = new Analyzer(_channel, _filepath);
        var analysis = analyser.RunAsync();

        using var neo4JProcessor = new Neo4jProcessor(_uri, _user, _password);
        await neo4JProcessor.InitializeAsync();

        IProcessor[] processors;
        if (_enableConsole)
            processors = [new ConsoleProcessor(), neo4JProcessor];
        else
            processors = [neo4JProcessor];

        try
        {
            var mgr = new ProcessingManager(_channel) { Processors = processors, TotalItems = analyser.TotalItems};
            var process = mgr.ProcessAsync();

            await analysis;
            mgr.TotalItems = analyser.TotalItems;
            
            await Task.WhenAll(process, _channel.Reader.Completion);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        Console.WriteLine("Done");
    }
}