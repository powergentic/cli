using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Performs a targeted search-and-replace edit within an existing file.
/// </summary>
public sealed class FileEditTool : IAgentTool
{
    public string Name => "file_edit";
    public string Description => "Edit an existing file by replacing a specific string with new content. The old string must match exactly.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.Write;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(EditFileAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Replace a specific string in a file with new content. The oldString must match exactly (including whitespace and indentation).")]
    private Task<string> EditFileAsync(
        [Description("The absolute path to the file to edit.")] string filePath,
        [Description("The exact text to find and replace. Must match exactly including whitespace.")] string oldString,
        [Description("The new text to replace the old text with.")] string newString)
    {
        try
        {
            if (!File.Exists(filePath))
                return Task.FromResult($"Error: File not found: {filePath}");

            var content = File.ReadAllText(filePath);
            var index = content.IndexOf(oldString, StringComparison.Ordinal);

            if (index < 0)
                return Task.FromResult("Error: The specified oldString was not found in the file. Make sure it matches exactly including whitespace and indentation.");

            // Check for multiple matches
            var secondIndex = content.IndexOf(oldString, index + 1, StringComparison.Ordinal);
            if (secondIndex >= 0)
                return Task.FromResult("Error: The specified oldString matches multiple locations in the file. Please provide more context to make it unique.");

            var newContent = content[..index] + newString + content[(index + oldString.Length)..];
            File.WriteAllText(filePath, newContent);

            return Task.FromResult($"Successfully edited {filePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error editing file: {ex.Message}");
        }
    }
}
