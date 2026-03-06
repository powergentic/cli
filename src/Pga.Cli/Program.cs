using System.CommandLine;
using Pga.Cli.Commands;

namespace Pga.Cli;

/// <summary>
/// PGA (Powergentic CLI) — An open-source AI agent CLI tool.
/// Provides GitHub Copilot-like functionality with configurable LLM backends.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PGA (Powergentic CLI) — AI-powered coding assistant CLI");

        var pathOption = new Option<string>("--path")
        {
            Description = "The project root directory to work with.",
            DefaultValueFactory = _ => Environment.CurrentDirectory
        };
        pathOption.Aliases.Add("-p");
        rootCommand.Options.Add(pathOption);

        rootCommand.Subcommands.Add(ChatCommand.Create());
        rootCommand.Subcommands.Add(ExplainCommand.Create());
        rootCommand.Subcommands.Add(SuggestCommand.Create());
        rootCommand.Subcommands.Add(ConfigCommand.Create());
        rootCommand.Subcommands.Add(InitCommand.Create());

        // If no subcommand is specified, default to interactive chat
        rootCommand.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathOption) ?? Environment.CurrentDirectory;
            await ChatCommand.RunDefaultChatAsync(path);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
