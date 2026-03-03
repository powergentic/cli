namespace Pga.Core.Agents;

/// <summary>
/// Defines the scope where an agent's instructions apply.
/// </summary>
public sealed class AgentScope
{
    /// <summary>
    /// The directory path this agent is scoped to.
    /// Instructions only apply when working within this directory or its children.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this scope applies globally (root-level AGENTS.md or agents/).
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Checks whether a given working path falls within this agent's scope.
    /// </summary>
    public bool Applies(string workingPath)
    {
        if (IsGlobal || string.IsNullOrEmpty(BasePath))
            return true;

        var normalizedBase = Path.GetFullPath(BasePath).TrimEnd(Path.DirectorySeparatorChar);
        var normalizedWork = Path.GetFullPath(workingPath).TrimEnd(Path.DirectorySeparatorChar);

        return normalizedWork.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }
}
