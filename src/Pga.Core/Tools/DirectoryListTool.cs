using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Lists the contents of a directory.
/// </summary>
public sealed class DirectoryListTool : IAgentTool
{
    public string Name => "directory_list";
    public string Description => "List the contents of a directory, showing files and subdirectories.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(ListAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("List the files and directories in a given path. Directories end with '/'.")]
    private Task<string> ListAsync(
        [Description("The absolute path of the directory to list.")] string path,
        [Description("Whether to include hidden files/folders (starting with '.'). Default is false.")] bool includeHidden = false,
        [Description("Maximum depth to recurse. 1 = immediate children only. Default is 1.")] int maxDepth = 1)
    {
        try
        {
            if (!Directory.Exists(path))
                return Task.FromResult($"Error: Directory not found: {path}");

            var entries = new List<string>();
            ListRecursive(path, path, entries, includeHidden, maxDepth, 0);

            if (entries.Count == 0)
                return Task.FromResult("(empty directory)");

            return Task.FromResult(string.Join('\n', entries));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error listing directory: {ex.Message}");
        }
    }

    private static void ListRecursive(string rootPath, string currentPath, List<string> entries,
        bool includeHidden, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth || entries.Count > 500)
            return;

        try
        {
            var dirs = Directory.GetDirectories(currentPath)
                .OrderBy(d => d)
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    if (!includeHidden && name.StartsWith('.')) return false;
                    return name is not ("node_modules" or "bin" or "obj" or ".git");
                });

            foreach (var dir in dirs)
            {
                var relativePath = Path.GetRelativePath(rootPath, dir);
                entries.Add(relativePath + "/");

                if (currentDepth + 1 < maxDepth)
                    ListRecursive(rootPath, dir, entries, includeHidden, maxDepth, currentDepth + 1);
            }

            var files = Directory.GetFiles(currentPath)
                .OrderBy(f => f)
                .Where(f => includeHidden || !Path.GetFileName(f).StartsWith('.'));

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(rootPath, file);
                entries.Add(relativePath);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible directories
        }
    }
}
