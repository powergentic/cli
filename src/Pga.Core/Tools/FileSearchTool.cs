using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Pga.Core.Tools;

/// <summary>
/// Searches for files matching a glob pattern in the workspace.
/// </summary>
public sealed class FileSearchTool : IAgentTool
{
    public string Name => "file_search";
    public string Description => "Search for files matching a glob pattern in the workspace. Returns matching file paths.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(SearchAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Search for files by glob pattern. Examples: **/*.cs, src/**/*.ts, **/README.md")]
    private Task<string> SearchAsync(
        [Description("The root directory to search from.")] string rootDirectory,
        [Description("The glob pattern to match files. Examples: '**/*.cs', 'src/**/*.json'")] string pattern,
        [Description("Maximum number of results to return. Default is 50.")] int maxResults = 50)
    {
        try
        {
            if (!Directory.Exists(rootDirectory))
                return Task.FromResult($"Error: Directory not found: {rootDirectory}");

            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            // Exclude common non-project directories
            matcher.AddExclude("**/node_modules/**");
            matcher.AddExclude("**/bin/**");
            matcher.AddExclude("**/obj/**");
            matcher.AddExclude("**/.git/**");
            matcher.AddExclude("**/dist/**");
            matcher.AddExclude("**/__pycache__/**");

            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(rootDirectory)));
            var files = result.Files
                .Take(maxResults)
                .Select(f => f.Path)
                .ToList();

            if (files.Count == 0)
                return Task.FromResult($"No files found matching pattern '{pattern}' in {rootDirectory}");

            return Task.FromResult(string.Join('\n', files));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error searching files: {ex.Message}");
        }
    }
}
