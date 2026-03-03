using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Executes shell commands in a terminal session.
/// </summary>
public sealed class ShellExecuteTool : IAgentTool
{
    private readonly string _workingDirectory;

    public ShellExecuteTool(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
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
        [Description("Optional working directory. Defaults to the project root.")] string? workingDirectory = null)
    {
        var dir = workingDirectory ?? _workingDirectory;

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/zsh",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c {command}",
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

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
}
