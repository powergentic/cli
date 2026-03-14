using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Executes shell commands in a terminal session, scoped to the configured working directory.
/// Commands are prevented from escaping the allowed root via path traversal.
/// </summary>
public sealed class ShellExecuteTool : IAgentTool
{
    private readonly string _allowedRoot;
    private readonly IShellExecuteProvider _provider;

    public ShellExecuteTool(string workingDirectory, IShellExecuteProvider provider)
    {
        _allowedRoot = Path.GetFullPath(workingDirectory);
        _provider = provider;
    }

    public string Name => "shell_execute";
    public string Description => "Execute a shell command and return its output. Use for running build commands, scripts, system operations, etc.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.Execute;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(ExecuteAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Execute a shell command and return stdout/stderr output.")]
    private async Task<string> ExecuteAsync(
        [Description("The shell command to execute.")] string command,
        [Description("Optional working directory (must be within the project root). Defaults to the project root.")] string? workingDirectory = null)
    {
        // Validate the command does not contain path traversal sequences
        if (_provider.ContainsPathTraversal(command))
            return "Error: Command rejected — path traversal ('..') is not allowed.";

        // Resolve and validate the working directory stays within the allowed root
        var dir = _provider.ResolveAndValidateDirectory(workingDirectory, _allowedRoot);
        if (dir is null)
            return $"Error: Working directory must be within the project root '{_allowedRoot}'.";

        try
        {
            using var process = new Process();
            process.StartInfo = _provider.CreateStartInfo(command, dir);

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var result = $"Exit code: {process.ExitCode}\n";
            if (!string.IsNullOrWhiteSpace(stdout))
                result += $"STDOUT:\n{TruncateOutput(stdout)}\n";
            if (!string.IsNullOrWhiteSpace(stderr))
                result += $"STDERR:\n{TruncateOutput(stderr)}\n";

            return result.Trim();
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }

    private static string TruncateOutput(string output, int maxLength = 50000)
    {
        if (output.Length <= maxLength) return output;
        return output[..maxLength] + $"\n... (output truncated, {output.Length - maxLength} chars omitted)";
    }

    /// <summary>
    /// Creates the appropriate <see cref="IShellExecuteProvider"/> for the current platform.
    /// </summary>
    public static IShellExecuteProvider CreatePlatformProvider()
        => OperatingSystem.IsWindows()
            ? new WindowsShellExecuteProvider()
            : new LinuxShellExecuteProvider();
}
