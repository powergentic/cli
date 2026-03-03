using System.CommandLine;
using Pga.Cli.Commands;

namespace Pga.Cli;

/// <summary>
/// PGA (Powergentic Agent) — An open-source AI agent CLI tool.
/// Provides GitHub Copilot-like functionality with configurable LLM backends.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("PGA (Powergentic Agent) — AI-powered coding assistant CLI");
        rootCommand.Subcommands.Add(ChatCommand.Create());
        rootCommand.Subcommands.Add(ExplainCommand.Create());
        rootCommand.Subcommands.Add(SuggestCommand.Create());
        rootCommand.Subcommands.Add(ConfigCommand.Create());
        rootCommand.Subcommands.Add(InitCommand.Create());

        // If no subcommand is specified, default to interactive chat
        rootCommand.SetAction(async (parseResult, ct) =>
        {
            await ChatCommand.RunDefaultChatAsync();
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
