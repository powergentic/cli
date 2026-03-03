using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Provides git operations: status, log, diff, branch info, etc.
/// </summary>
public sealed class GitOperationsTool : IAgentTool
{
    private readonly string _workingDirectory;

    public GitOperationsTool(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public string Name => "git_operations";
    public string Description => "Perform git operations: status, log, diff, show, branch listing, blame.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(ExecuteGitAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Execute a read-only git operation and return its output.")]
    private async Task<string> ExecuteGitAsync(
        [Description("The git operation to run. Allowed: status, log, diff, show, branch, blame, remote, stash-list")] string operation,
        [Description("Additional arguments for the git command.")] string? args = null)
    {
        // Only allow read-only git operations
        var allowedOps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "status", "log", "diff", "show", "branch", "blame", "remote", "stash-list",
            "rev-parse", "describe", "shortlog", "tag"
        };

        if (!allowedOps.Contains(operation))
            return $"Error: Git operation '{operation}' is not allowed. Allowed operations: {string.Join(", ", allowedOps)}";

        var gitCommand = operation == "stash-list" ? "stash list" : operation;
        if (!string.IsNullOrWhiteSpace(args))
            gitCommand += " " + args;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"--no-pager {gitCommand}",
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment = { ["GIT_TERMINAL_PROMPT"] = "0" }
            };

            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var result = stdout;
            if (!string.IsNullOrWhiteSpace(stderr) && process.ExitCode != 0)
                result += $"\nSTDERR: {stderr}";

            if (result.Length > 50000)
                result = result[..50000] + "\n... (output truncated)";

            return string.IsNullOrWhiteSpace(result) ? "(no output)" : result;
        }
        catch (Exception ex)
        {
            return $"Error executing git command: {ex.Message}";
        }
    }
}
