using System.CommandLine;
using Pga.Cli.Rendering;
using Pga.Core.Chat;
using Pga.Core.Configuration;

namespace Pga.Cli.Commands;

/// <summary>
/// Single-shot suggest command — ask the agent to suggest a shell command.
/// Similar to `gh copilot suggest`.
/// </summary>
public static class SuggestCommand
{
    public static Command Create()
    {
        var textArgument = new Argument<string>("text")
        {
            Description = "What you want to accomplish (the agent will suggest a command)."
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

        var shellOption = new Option<string>("--shell")
        {
            Description = "The target shell (bash, zsh, powershell, cmd).",
            DefaultValueFactory = _ => DetectShell()
        };
        shellOption.Aliases.Add("-s");

        var command = new Command("suggest", "Ask the AI agent to suggest a shell command for a given task.")
        {
            textArgument,
            pathOption,
            profileOption,
            shellOption
        };

        command.SetAction(async (parseResult, ct) =>
        {
            var text = parseResult.GetValue(textArgument)!;
            var path = parseResult.GetValue(pathOption) ?? Environment.CurrentDirectory;
            var profile = parseResult.GetValue(profileOption);
            var shell = parseResult.GetValue(shellOption) ?? DetectShell();
            await RunSuggest(text, path, profile, shell);
        });

        return command;
    }

    private static async Task RunSuggest(string text, string projectPath, string? profileName, string shell)
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

        var prompt = $"""
            Suggest a shell command to accomplish the following task:

            Task: {text}

            Target shell: {shell}
            Operating system: {GetOsName()}
            Working directory: {projectPath}

            Respond with:
            1. The exact command(s) to run
            2. A brief explanation of what each command does
            3. Any warnings or prerequisites

            Format the commands in a code block for easy copying.
            """;

        try
        {
            var response = await orchestrator.SendMessageAsync(prompt, profileName);
            ConsoleRenderer.RenderAssistantMessage(response);

            // Offer to run the command
            Console.Write("Would you like to run this command? (y/N): ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer is "y" or "yes")
            {
                ConsoleRenderer.RenderInfo("Tip: Copy and paste the command from above to run it, for safety.");
            }
        }
        catch (Exception ex)
        {
            ConsoleRenderer.RenderError($"Failed to get suggestion: {ex.Message}");
        }
    }

    private static string DetectShell()
    {
        if (OperatingSystem.IsWindows())
            return Environment.GetEnvironmentVariable("COMSPEC")?.Contains("powershell", StringComparison.OrdinalIgnoreCase) == true
                ? "powershell"
                : "cmd";

        var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/zsh";
        return Path.GetFileName(shell);
    }

    private static string GetOsName()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "Unknown";
    }
}
