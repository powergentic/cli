using System.CommandLine;
using Pga.Cli.Rendering;
using Pga.Core.Chat;
using Pga.Core.Configuration;

namespace Pga.Cli.Commands;

/// <summary>
/// Single-shot explain command — ask the agent to explain something.
/// Similar to `gh copilot explain`.
/// </summary>
public static class ExplainCommand
{
    public static Command Create()
    {
        var textArgument = new Argument<string>("text")
        {
            Description = "The command, code, or concept to explain."
        };

        var pathOption = new Option<string>("--path")
        {
            Description = "The project root directory.",
            DefaultValueFactory = _ => Environment.CurrentDirectory
        };
        pathOption.Aliases.Add("-p");

        var profileOption = new Option<string?>("--profile")
        {
            Description = "The LLM profile to use."
        };

        var command = new Command("explain", "Ask the AI agent to explain a command, error, or concept.")
        {
            textArgument,
            pathOption,
            profileOption
        };

        command.SetAction(async (parseResult, ct) =>
        {
            var text = parseResult.GetValue(textArgument)!;
            var path = parseResult.GetValue(pathOption) ?? Environment.CurrentDirectory;
            var profile = parseResult.GetValue(profileOption);
            await RunExplain(text, path, profile);
        });

        return command;
    }

    private static async Task RunExplain(string text, string projectPath, string? profileName)
    {
        projectPath = Path.GetFullPath(projectPath);
        var configManager = new ConfigManager(projectPath);

        var errors = configManager.Validate();
        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ConsoleRenderer.RenderError(error);
            return;
        }

        var orchestrator = new ChatOrchestrator(configManager, projectPath, profileName);

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

        var prompt = $"""
            Please explain the following clearly and concisely:

            ```
            {text}
            ```

            Provide a clear explanation suitable for a developer. Include:
            - What it does
            - How it works
            - Any important caveats or notes
            """;

        try
        {
            var response = await orchestrator.SendMessageAsync(prompt, profileName);
            ConsoleRenderer.RenderAssistantMessage(response);
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError($"Failed to get explanation: {ex.Message}");
        }
    }
}
