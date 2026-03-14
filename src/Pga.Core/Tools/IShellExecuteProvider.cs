using System.Diagnostics;

namespace Pga.Core.Tools;

/// <summary>
/// Abstracts platform-specific shell execution behavior.
/// Implementations handle shell selection, argument formatting, and
/// platform-specific path traversal detection.
/// </summary>
public interface IShellExecuteProvider
{
    /// <summary>
    /// Creates a <see cref="ProcessStartInfo"/> configured for the platform's shell.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The validated working directory.</param>
    ProcessStartInfo CreateStartInfo(string command, string workingDirectory);

    /// <summary>
    /// Checks whether a command string contains path traversal sequences
    /// that could escape the allowed working directory.
    /// </summary>
    bool ContainsPathTraversal(string command);

    /// <summary>
    /// Resolves a requested working directory to its canonical form
    /// and validates it is within the allowed root.
    /// Returns the resolved path, or <c>null</c> if the path is outside the root.
    /// </summary>
    string? ResolveAndValidateDirectory(string? requestedDir, string allowedRoot);
}
