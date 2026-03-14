using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pga.Core.Tools;

/// <summary>
/// Windows shell execution provider.
/// Uses cmd.exe and detects both forward-slash and backslash path traversal.
/// </summary>
public sealed partial class WindowsShellExecuteProvider : IShellExecuteProvider
{
    public ProcessStartInfo CreateStartInfo(string command, string workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    public bool ContainsPathTraversal(string command)
        => TraversalPattern().IsMatch(command);

    public string? ResolveAndValidateDirectory(string? requestedDir, string allowedRoot)
    {
        if (string.IsNullOrWhiteSpace(requestedDir))
            return allowedRoot;

        // Resolve to canonical full path
        var resolved = Path.GetFullPath(requestedDir);
        var root = Path.GetFullPath(allowedRoot);

        // Ensure both end with separator for prefix comparison
        if (!root.EndsWith(Path.DirectorySeparatorChar))
            root += Path.DirectorySeparatorChar;

        // Case-insensitive comparison for Windows file system
        if (resolved.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
            resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return resolved;
        }

        return null;
    }

    /// <summary>
    /// Matches ".." as a path component using both / and \ separators,
    /// which are both valid on Windows.
    /// </summary>
    [GeneratedRegex(@"(^|[\s/\\;|&(""'`])\.\.([/\\\s;|&)""'`]|$)", RegexOptions.Compiled)]
    private static partial Regex TraversalPattern();
}
