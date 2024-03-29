﻿using System.ComponentModel;
using Spectre.Console.Cli;

namespace CodeGrapher;

public class CommandLine : AsyncCommand<CommandLine.Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        Console.WriteLine("Analyzing...");
        using var manager = new GraphManager(settings.FilePath, settings.Host, settings.Username, settings.Password, settings.Database);
        await manager.RunAsync();
        Console.WriteLine("Done");
        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<filepath>")] public string? FilePath { get; init; }

        [CommandOption("-h|--host")]
        [DefaultValue("bolt://localhost:7687")]
        public string? Host { get; init; }

        [CommandOption("-u|--username")]
        [DefaultValue("neo4j")]
        public string? Username { get; init; }

        [CommandOption("-p|--password")]
        [DefaultValue("12345678")]
        public string? Password { get; init; }
        
        [CommandOption("-d|--database")]
        [DefaultValue("codegrapher")]
        public string? Database { get; init; }

    }
}