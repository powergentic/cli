using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Reads the contents of a file, optionally with line range.
/// </summary>
public sealed class FileReadTool : IAgentTool
{
    public string Name => "file_read";
    public string Description => "Read the contents of a file, optionally specifying a line range.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(ReadFileAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Read the contents of a file. Returns the file content or an error message.")]
    private Task<string> ReadFileAsync(
        [Description("The absolute path to the file to read.")] string filePath,
        [Description("Optional starting line number (1-based). If omitted, reads from the beginning.")] int? startLine = null,
        [Description("Optional ending line number (1-based, inclusive). If omitted, reads to the end.")] int? endLine = null)
    {
        try
        {
            if (!File.Exists(filePath))
                return Task.FromResult($"Error: File not found: {filePath}");

            var lines = File.ReadAllLines(filePath);
            var start = (startLine ?? 1) - 1;
            var end = (endLine ?? lines.Length) - 1;

            start = Math.Max(0, Math.Min(start, lines.Length - 1));
            end = Math.Max(start, Math.Min(end, lines.Length - 1));

            var selectedLines = lines.Skip(start).Take(end - start + 1);
            var result = string.Join('\n', selectedLines.Select((line, idx) => $"{start + idx + 1}: {line}"));

            if (result.Length > 100000)
                result = result[..100000] + "\n... (content truncated)";

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error reading file: {ex.Message}");
        }
    }
}
