using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pga.Core.Tools;

/// <summary>
/// Unix/macOS shell execution provider.
/// Uses /bin/bash and detects Unix-style path traversal.
/// </summary>
public sealed partial class LinuxShellExecuteProvider : IShellExecuteProvider
{
    public ProcessStartInfo CreateStartInfo(string command, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Use ArgumentList so the full command is passed as a single argv entry to bash.
        // ProcessStartInfo.Arguments would split on spaces and break multi-word commands.
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        return psi;
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

        if (resolved.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal) ||
            resolved.StartsWith(root, StringComparison.Ordinal))
        {
            return resolved;
        }

        return null;
    }

    /// <summary>
    /// Matches ".." as a path component:
    /// - preceded by start-of-string, whitespace, /, or common shell operators
    /// - followed by /, whitespace, end-of-string, or shell operators
    /// </summary>
    [GeneratedRegex(@"(^|[\s/;|&(""'`])\.\.([/\s;|&)""'`]|$)", RegexOptions.Compiled)]
    private static partial Regex TraversalPattern();
}
