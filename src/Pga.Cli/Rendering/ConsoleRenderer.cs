using Spectre.Console;

namespace Pga.Cli.Rendering;

/// <summary>
/// Renders console output with rich formatting using Spectre.Console.
/// </summary>
public static class ConsoleRenderer
{
    /// <summary>
    /// Renders a welcome banner.
    /// </summary>
    public static void RenderBanner()
    {
        AnsiConsole.Write(new FigletText("PGA")
            .Color(Color.Blue));
        AnsiConsole.MarkupLine("[dim]Powergentic CLI — AI-powered CLI assistant[/]");
        AnsiConsole.MarkupLine("[dim]Type your message, or /help for commands. /exit to quit.[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a user input prompt and returns the input.
    /// </summary>
    public static string Prompt()
    {
        AnsiConsole.Markup("[bold blue]❯[/] ");
        return Console.ReadLine() ?? string.Empty;
    }

    /// <summary>
    /// Renders a multiline user input prompt (for multi-line input mode).
    /// </summary>
    public static string PromptMultiLine()
    {
        AnsiConsole.MarkupLine("[dim](Enter your message. Press Enter twice to send, or Ctrl+C to cancel)[/]");
        var lines = new List<string>();
        int emptyLineCount = 0;

        while (true)
        {
            AnsiConsole.Markup("[dim]…[/] ");
            var line = Console.ReadLine();
            if (line == null) break;

            if (string.IsNullOrEmpty(line))
            {
                emptyLineCount++;
                if (emptyLineCount >= 2)
                    break;
                lines.Add(line);
            }
            else
            {
                emptyLineCount = 0;
                lines.Add(line);
            }
        }

        return string.Join('\n', lines).TrimEnd();
    }

    /// <summary>
    /// Renders an assistant response with markdown-like formatting.
    /// </summary>
    public static void RenderAssistantMessage(string content)
    {
        AnsiConsole.WriteLine();
        var panel = new Panel(Markup.Escape(content))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Padding = new Padding(1, 0, 1, 0),
            Header = new PanelHeader("[bold green] Assistant [/]")
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Renders a streaming token (appended without newline).
    /// </summary>
    public static void RenderStreamingToken(string token)
    {
        AnsiConsole.Markup(Markup.Escape(token));
    }

    /// <summary>
    /// Renders a tool invocation notice.
    /// </summary>
    public static void RenderToolCall(string toolName, string description)
    {
        AnsiConsole.MarkupLine($"  [dim]🔧 Calling[/] [bold yellow]{Markup.Escape(toolName)}[/][dim]...[/]");
    }

    /// <summary>
    /// Renders a tool result summary.
    /// </summary>
    public static void RenderToolResult(string toolName, string result)
    {
        var preview = result.Length > 200 ? result[..200] + "..." : result;
        AnsiConsole.MarkupLine($"  [dim]✓ {Markup.Escape(toolName)} completed ({result.Length} chars)[/]");
    }

    /// <summary>
    /// Renders an error message.
    /// </summary>
    public static void RenderError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Renders a warning message.
    /// </summary>
    public static void RenderWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]Warning:[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Renders a success message.
    /// </summary>
    public static void RenderSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]✓[/] {Markup.Escape(message)}");
    }

    /// <summary>
    /// Renders an info message.
    /// </summary>
    public static void RenderInfo(string message)
    {
        AnsiConsole.MarkupLine($"[dim]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Prompts the user to approve a tool execution.
    /// </summary>
    public static async Task<bool> PromptToolApproval(string toolName, string description)
    {
        AnsiConsole.MarkupLine($"  [bold yellow]⚠ Tool requires approval:[/] [bold]{Markup.Escape(toolName)}[/]");
        AnsiConsole.MarkupLine($"  [dim]{Markup.Escape(description)}[/]");

        var result = AnsiConsole.Confirm("  Allow execution?", defaultValue: true);
        return await Task.FromResult(result);
    }

    /// <summary>
    /// Renders help information for interactive mode.
    /// </summary>
    public static void RenderHelp()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("Command")
            .AddColumn("Description");

        table.AddRow("[bold]/help[/]", "Show this help message");
        table.AddRow("[bold]/exit[/], [bold]/quit[/]", "Exit interactive mode");
        table.AddRow("[bold]/clear[/]", "Clear conversation history");
        table.AddRow("[bold]/agents[/]", "List available agents");
        table.AddRow("[bold]/agent <name>[/]", "Switch to a specific agent");
        table.AddRow("[bold]/profile <name>[/]", "Switch LLM profile");
        table.AddRow("[bold]/status[/]", "Show current session status");
        table.AddRow("[bold]/multiline[/]", "Enter multi-line input mode");

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Renders session status information.
    /// </summary>
    public static void RenderStatus(string projectPath, string? activeAgent, string activeProfile, int messageCount)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("Setting")
            .AddColumn("Value");

        table.AddRow("Project Path", Markup.Escape(projectPath));
        table.AddRow("Active Agent", Markup.Escape(activeAgent ?? "(default)"));
        table.AddRow("LLM Profile", Markup.Escape(activeProfile));
        table.AddRow("Messages", messageCount.ToString());

        AnsiConsole.Write(table);
    }
}
