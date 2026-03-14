namespace Pga.Core;

public static class StaticValues
{
    /// <summary>
    /// Default folders to look for agent definitions in (e.g., .powergentic/agents/, .github/agents/)
    /// </summary>
    public static readonly string[] DefaultAgentFolders = new []
    {
        ".powergentic",
        ".github"
    };

    /// <summary>
    /// Subfolder to look for agent definitions in (e.g., .powergentic/agents/, .github/agents/, src/frontend/agents/)
    /// </summary>
    public static readonly string AgentSubfolderName = "agents";

    /// <summary>
    /// File name for the global agent definition (e.g., AGENTS.md)
    /// </summary>
    public static readonly string GlobalAgentFileName = "AGENTS.md";

    /// <summary>
    /// File pattern for custom agent definitions (e.g., *.agent.md)
    /// </summary>
    public static readonly string CustomAgentFilePattern = "*.agent.md";
}