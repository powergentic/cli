using System.CommandLine;
using Pga.Cli.Rendering;
using Pga.Core.Agents;
using Pga.Core.Chat;
using Pga.Core.Configuration;

namespace Pga.Cli.Commands;

/// <summary>
/// Interactive chat REPL command - the primary mode of PGA.
/// </summary>
public static class ChatCommand
{
    public static Command Create()
    {
        var pathOption = new Option<string>("--path")
        {
            Description = "The project root directory to work with.",
            DefaultValueFactory = _ => Environment.CurrentDirectory
        };
        pathOption.Aliases.Add("-p");

        var agentOption = new Option<string?>("--agent")
        {
            Description = "The name of a specific agent to use."
        };
        agentOption.Aliases.Add("-a");

        var profileOption = new Option<string?>("--profile")
        {
            Description = "The LLM profile to use (overrides agent/auto-select)."
        };

        var command = new Command("chat", "Start an interactive chat session with the AI agent.")
        {
            pathOption,
            agentOption,
            profileOption
        };

        command.SetAction(async (parseResult, ct) =>
        {
            var path = parseResult.GetValue(pathOption) ?? Environment.CurrentDirectory;
            var agent = parseResult.GetValue(agentOption);
            var profile = parseResult.GetValue(profileOption);
            await RunInteractiveChat(path, agent, profile);
        });

        return command;
    }

    /// <summary>
    /// Default entry point when no subcommand is specified.
    /// </summary>
    public static Task RunDefaultChatAsync() =>
        RunInteractiveChat(Environment.CurrentDirectory, null, null);

    private static async Task RunInteractiveChat(string projectPath, string? agentName, string? profileName)
    {
        projectPath = Path.GetFullPath(projectPath);
        var configManager = new ConfigManager();

        // Validate configuration
        var errors = configManager.Validate();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ConsoleRenderer.RenderError(error);
            ConsoleRenderer.RenderInfo("Run 'pga config init' to set up your configuration.");
            return;
        }

        // Show banner and session info
        ConsoleRenderer.RenderBanner();

        var agentLoader = new AgentLoader();
        var agents = agentLoader.LoadAgents(projectPath);
        var agentNames = agents.GetAgentNames();

        if (agentNames.Count > 0)
            ConsoleRenderer.RenderInfo($"Loaded agents: {string.Join(", ", agentNames)}");

        if (agents.GlobalAgent != null)
            ConsoleRenderer.RenderInfo("Found AGENTS.md — global instructions loaded.");

        var resolved = configManager.ResolveProfile(profileName, agentName != null ? agents.GetByName(agentName)?.Profile : null);
        var activeProfile = resolved?.Name ?? "default";
        ConsoleRenderer.RenderInfo($"Using LLM profile: {activeProfile}");
        Console.WriteLine();

        // Create orchestrator
        var orchestrator = new ChatOrchestrator(configManager, projectPath, profileName, agentName);

        var config = configManager.Load();
        if (config.Ui.ShowToolCalls)
        {
            orchestrator.OnToolInvocation += (name, desc) =>
            {
                ConsoleRenderer.RenderToolCall(name, desc);
                return Task.CompletedTask;
            };
            orchestrator.OnToolResult += ConsoleRenderer.RenderToolResult;
        }

        orchestrator.OnToolApprovalNeeded += ConsoleRenderer.PromptToolApproval;

        // Main REPL loop
        while (true)
        {
            var input = ConsoleRenderer.Prompt();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Handle slash commands
            if (input.StartsWith('/'))
            {
                var handled = HandleSlashCommand(input, projectPath, ref agentName, ref activeProfile, orchestrator, agents);
                if (handled == SlashCommandResult.Exit)
                    break;
                if (handled == SlashCommandResult.Handled)
                    continue;
            }

            // Send to LLM
            try
            {
                string response;
                if (config.Ui.StreamResponses)
                {
                    response = await orchestrator.SendMessageStreamingAsync(input, profileName, agentName);
                }
                else
                {
                    response = await orchestrator.SendMessageAsync(input, profileName, agentName);
                }

                ConsoleRenderer.RenderAssistantMessage(response);
            }
            catch (Exception ex)
            {
                ConsoleRenderer.RenderError($"Failed to get response: {ex.Message}");
            }
        }

        ConsoleRenderer.RenderInfo("Goodbye! 👋");
    }

    private enum SlashCommandResult { Exit, Handled, NotHandled }

    private static SlashCommandResult HandleSlashCommand(
        string input,
        string projectPath,
        ref string? agentName,
        ref string activeProfile,
        ChatOrchestrator orchestrator,
        AgentCollection agents)
    {
        var parts = input.TrimStart('/').Split(' ', 2);
        var cmd = parts[0].ToLowerInvariant();
        var arg = parts.Length > 1 ? parts[1] : null;

        switch (cmd)
        {
            case "exit" or "quit":
                return SlashCommandResult.Exit;

            case "help":
                ConsoleRenderer.RenderHelp();
                return SlashCommandResult.Handled;

            case "clear":
                orchestrator.History.Clear();
                ConsoleRenderer.RenderSuccess("Conversation history cleared.");
                return SlashCommandResult.Handled;

            case "agents":
                var names = agents.GetAgentNames();
                if (names.Count == 0)
                    ConsoleRenderer.RenderInfo("No custom agents found. Add *.agent.md files to the agents/ directory.");
                else
                    ConsoleRenderer.RenderInfo($"Available agents: {string.Join(", ", names)}");
                return SlashCommandResult.Handled;

            case "agent":
                if (string.IsNullOrWhiteSpace(arg))
                {
                    ConsoleRenderer.RenderInfo($"Current agent: {agentName ?? "(default)"}");
                }
                else
                {
                    agentName = arg;
                    ConsoleRenderer.RenderSuccess($"Switched to agent: {agentName}");
                }
                return SlashCommandResult.Handled;

            case "profile":
                if (string.IsNullOrWhiteSpace(arg))
                {
                    ConsoleRenderer.RenderInfo($"Current profile: {activeProfile}");
                }
                else
                {
                    activeProfile = arg;
                    ConsoleRenderer.RenderSuccess($"Switched to profile: {activeProfile}");
                }
                return SlashCommandResult.Handled;

            case "status":
                ConsoleRenderer.RenderStatus(projectPath, agentName, activeProfile, orchestrator.History.Count);
                return SlashCommandResult.Handled;

            case "multiline":
                var multiInput = ConsoleRenderer.PromptMultiLine();
                if (!string.IsNullOrWhiteSpace(multiInput))
                    ConsoleRenderer.RenderInfo($"[Input captured: {multiInput.Length} chars. Send with regular prompt.]");
                return SlashCommandResult.Handled;

            default:
                ConsoleRenderer.RenderWarning($"Unknown command: /{cmd}. Type /help for available commands.");
                return SlashCommandResult.Handled;
        }
    }
}
