using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Creates or overwrites a file with the given content.
/// Will create directories as needed.
/// </summary>
public sealed class FileWriteTool : IAgentTool
{
    public string Name => "file_write";
    public string Description => "Create a new file or overwrite an existing file with the specified content. Automatically creates parent directories.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.Write;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(WriteFileAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Create or overwrite a file with the specified content.")]
    private Task<string> WriteFileAsync(
        [Description("The absolute path to the file to create or overwrite.")] string filePath,
        [Description("The content to write to the file.")] string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(filePath, content);
            return Task.FromResult($"Successfully wrote {content.Length} characters to {filePath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error writing file: {ex.Message}");
        }
    }
}
