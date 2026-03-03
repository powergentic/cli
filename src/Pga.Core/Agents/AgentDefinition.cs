namespace Pga.Core.Agents;

/// <summary>
/// Represents a parsed agent definition from AGENTS.md or *.agent.md files.
/// </summary>
public sealed class AgentDefinition
{
    /// <summary>
    /// The agent name (derived from filename or frontmatter).
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Optional description of the agent.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The full system prompt / instructions from the markdown body.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Optional LLM profile name to use for this agent.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>
    /// List of tool names this agent is allowed to use.
    /// If empty, all tools are available.
    /// </summary>
    public List<string> Tools { get; set; } = new();

    /// <summary>
    /// List of tool names this agent is explicitly denied.
    /// </summary>
    public List<string> DisabledTools { get; set; } = new();

    /// <summary>
    /// The file path this agent was loaded from.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// The scope/directory this agent applies to.
    /// </summary>
    public AgentScope Scope { get; set; } = new();

    /// <summary>
    /// Whether this is the root AGENTS.md (global instructions).
    /// </summary>
    public bool IsGlobalAgent { get; set; }

    /// <summary>
    /// Custom metadata from frontmatter.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
