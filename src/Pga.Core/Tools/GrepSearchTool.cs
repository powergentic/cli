using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Searches file contents using text or regex patterns.
/// </summary>
public sealed class GrepSearchTool : IAgentTool
{
    public string Name => "grep_search";
    public string Description => "Search file contents with text or regex patterns. Returns matching lines with file paths and line numbers.";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction() => AIFunctionFactory.Create(SearchAsync, new AIFunctionFactoryOptions
    {
        Name = Name,
        Description = Description
    });

    [Description("Search file contents for a pattern. Returns matching lines with file paths and line numbers.")]
    private Task<string> SearchAsync(
        [Description("The directory to search in.")] string directory,
        [Description("The search pattern (text or regex).")] string pattern,
        [Description("Whether the pattern is a regular expression. Default is false.")] bool isRegex = false,
        [Description("Optional glob pattern to filter which files to search. E.g., '*.cs' or '*.py'")] string? filePattern = null,
        [Description("Maximum number of matches to return. Default is 50.")] int maxResults = 50)
    {
        try
        {
            if (!Directory.Exists(directory))
                return Task.FromResult($"Error: Directory not found: {directory}");

            Regex? regex = null;
            if (isRegex)
            {
                try
                {
                    regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
                }
                catch (RegexParseException ex)
                {
                    return Task.FromResult($"Error: Invalid regex pattern: {ex.Message}");
                }
            }

            var results = new List<string>();
            var searchPattern = filePattern ?? "*.*";
            var files = GetSearchableFiles(directory, searchPattern);

            foreach (var file in files)
            {
                if (results.Count >= maxResults) break;

                try
                {
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length && results.Count < maxResults; i++)
                    {
                        bool matches = regex != null
                            ? regex.IsMatch(lines[i])
                            : lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase);

                        if (matches)
                        {
                            var relativePath = Path.GetRelativePath(directory, file);
                            results.Add($"{relativePath}:{i + 1}: {lines[i].TrimStart()}");
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be read (binary, locked, etc.)
                }
            }

            if (results.Count == 0)
                return Task.FromResult($"No matches found for '{pattern}' in {directory}");

            return Task.FromResult(string.Join('\n', results));
        }
        catch (Exception ex)
        {
            return Task.FromResult($"Error during search: {ex.Message}");
        }
    }

    private static IEnumerable<string> GetSearchableFiles(string directory, string searchPattern)
    {
        var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "node_modules", "bin", "obj", ".git", "dist", "__pycache__", "vendor", "packages", ".vs"
        };

        return Directory.EnumerateFiles(directory, searchPattern, new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.System
        }).Where(f =>
        {
            var parts = Path.GetRelativePath(directory, f).Split(Path.DirectorySeparatorChar);
            return !parts.Any(p => skipDirs.Contains(p) || p.StartsWith('.'));
        });
    }
}
