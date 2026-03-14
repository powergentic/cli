using System.CommandLine;
using Pga.Cli.Rendering;
using Pga.Core;

namespace Pga.Cli.Commands;

/// <summary>
/// Initializes a project directory with AGENTS.md and agents/ folder.
/// </summary>
public static class InitCommand
{
    public static Command Create()
    {
        var pathOption = new Option<string>("--path")
        {
            Description = "The project directory to initialize.",
            DefaultValueFactory = _ => Environment.CurrentDirectory
        };
        pathOption.Aliases.Add("-p");

        var command = new Command("init", $"Initialize a project with {StaticValues.GlobalAgentFileName} and {StaticValues.AgentSubfolderName}/ folder.")
        {
            pathOption
        };

        command.SetAction(parseResult =>
        {
            var path = Path.GetFullPath(parseResult.GetValue(pathOption) ?? Environment.CurrentDirectory);
            InitializeProject(path);
        });

        return command;
    }

    private static void InitializeProject(string projectPath)
    {
        if (!Directory.Exists(projectPath))
        {
            ConsoleRenderer.RenderError($"Directory not found: {projectPath}");
            return;
        }

        // Create AGENTS.md
        var agentsFile = Path.Combine(projectPath, StaticValues.GlobalAgentFileName);
        if (!File.Exists(agentsFile))
        {
            File.WriteAllText(agentsFile, DefaultAgentsMd());
            ConsoleRenderer.RenderSuccess($"Created {agentsFile}");
        }
        else
        {
            ConsoleRenderer.RenderInfo($"{StaticValues.GlobalAgentFileName} already exists, skipping.");
        }

        // Create agents/ directory
        var agentsDir = Path.Combine(projectPath, StaticValues.AgentSubfolderName);
        if (!Directory.Exists(agentsDir))
        {
            Directory.CreateDirectory(agentsDir);
            ConsoleRenderer.RenderSuccess($"Created {agentsDir}/");

            // Create an example agent
            var exampleAgent = Path.Combine(agentsDir, "code-reviewer.agent.md");
            File.WriteAllText(exampleAgent, DefaultExampleAgentMd());
            ConsoleRenderer.RenderSuccess($"Created example agent: {exampleAgent}");
        }
        else
        {
            ConsoleRenderer.RenderInfo("agents/ directory already exists, skipping.");
        }

        ConsoleRenderer.RenderInfo("");
        ConsoleRenderer.RenderSuccess("Project initialized for PGA!");
        ConsoleRenderer.RenderInfo("• Edit AGENTS.md to customize global AI instructions for your project.");
        ConsoleRenderer.RenderInfo("• Add custom agents in the agents/ directory as *.agent.md files.");
        ConsoleRenderer.RenderInfo("• Run 'pga chat' to start an interactive session.");
    }

    private static string DefaultAgentsMd() => """
        # Project Instructions for AI Agent

        You are an AI assistant working on this project. Follow these guidelines:

        ## General Rules
        - Follow the existing code style and conventions in the project
        - Write clean, well-documented code
        - Include appropriate error handling
        - Write unit tests for new functionality

        ## Project Structure
        <!-- Describe your project structure here so the AI understands it -->

        ## Technology Stack
        <!-- List the key technologies, frameworks, and libraries used -->

        ## Coding Standards
        <!-- Add any specific coding standards or patterns to follow -->
        """;

    private static string DefaultExampleAgentMd() => """
        ---
        name: code-reviewer
        description: Reviews code for quality, best practices, and potential issues.
        tools:
          - file_read
          - grep_search
          - directory_list
          - git_operations
        ---

        # Code Reviewer Agent

        You are an expert code reviewer. When reviewing code:

        1. **Check for bugs** — Look for potential null references, off-by-one errors, race conditions.
        2. **Assess code quality** — Evaluate naming, structure, and adherence to SOLID principles.
        3. **Review security** — Identify potential security vulnerabilities.
        4. **Suggest improvements** — Offer specific, actionable suggestions with code examples.
        5. **Check tests** — Verify that tests adequately cover the changes.

        Be constructive and explain the *why* behind each suggestion.
        """;
}
