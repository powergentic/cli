namespace Pga.Core.Agents;

/// <summary>
/// Discovers and loads agent definitions from a project directory.
/// Follows GitHub Copilot conventions:
///   - AGENTS.md at root for global instructions
///   - agents/*.agent.md for custom agents
///   - .github/agents/*.agent.md (alternative location)
///   - Scoped agents in subdirectories (e.g., src/frontend/agents/*.agent.md)
/// </summary>
public sealed class AgentLoader
{
    private readonly AgentMarkdownParser _parser = new();

    /// <summary>
    /// Loads all agent definitions from the given project root directory.
    /// </summary>
    public AgentCollection LoadAgents(string projectRoot)
    {
        var collection = new AgentCollection();
        projectRoot = Path.GetFullPath(projectRoot);

        // 1. Load global AGENTS.md
        LoadGlobalAgent(projectRoot, collection);

        // 2. Load agents from agents/ directory
        LoadAgentsFromDirectory(Path.Combine(projectRoot, "agents"), projectRoot, collection);

        // 3. Load agents from .github/agents/ directory
        LoadAgentsFromDirectory(Path.Combine(projectRoot, ".github", "agents"), projectRoot, collection);

        // 4. Discover scoped agents in subdirectories
        LoadScopedAgents(projectRoot, collection);

        return collection;
    }

    /// <summary>
    /// Loads agents applicable to a specific working path within the project.
    /// Returns agents sorted by specificity (most specific scope first).
    /// </summary>
    public List<AgentDefinition> ResolveAgentsForPath(string projectRoot, string workingPath)
    {
        var collection = LoadAgents(projectRoot);
        return collection.ResolveForPath(workingPath);
    }

    private void LoadGlobalAgent(string projectRoot, AgentCollection collection)
    {
        var agentsFile = Path.Combine(projectRoot, "AGENTS.md");
        if (File.Exists(agentsFile))
        {
            var agent = _parser.ParseGlobalAgentFile(agentsFile);
            collection.GlobalAgent = agent;
        }
    }

    private void LoadAgentsFromDirectory(string agentsDir, string scopeBasePath, AgentCollection collection)
    {
        if (!Directory.Exists(agentsDir))
            return;

        var agentFiles = Directory.GetFiles(agentsDir, "*.agent.md", SearchOption.TopDirectoryOnly);
        foreach (var file in agentFiles)
        {
            var agent = _parser.ParseAgentFile(file, scopeBasePath);
            collection.AddAgent(agent);
        }
    }

    private void LoadScopedAgents(string projectRoot, AgentCollection collection)
    {
        // Search for agents/ folders in subdirectories (for scoping)
        try
        {
            var allAgentDirs = Directory.GetDirectories(projectRoot, "agents", SearchOption.AllDirectories);
            foreach (var agentDir in allAgentDirs)
            {
                // Skip root-level agents/ and .github/agents/ (already loaded)
                var parentDir = Path.GetDirectoryName(agentDir) ?? string.Empty;
                if (parentDir.Equals(projectRoot, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (parentDir.Equals(Path.Combine(projectRoot, ".github"), StringComparison.OrdinalIgnoreCase))
                    continue;

                // Skip hidden directories and common non-project directories
                if (ShouldSkipDirectory(agentDir, projectRoot))
                    continue;

                var scopeBase = parentDir;
                var agentFiles = Directory.GetFiles(agentDir, "*.agent.md", SearchOption.TopDirectoryOnly);
                foreach (var file in agentFiles)
                {
                    var agent = _parser.ParseAgentFile(file, scopeBase);
                    agent.Scope.IsGlobal = false;
                    collection.AddAgent(agent);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip directories we can't access
        }
    }

    private static bool ShouldSkipDirectory(string dir, string projectRoot)
    {
        var relative = Path.GetRelativePath(projectRoot, dir);
        var parts = relative.Split(Path.DirectorySeparatorChar);

        foreach (var part in parts)
        {
            if (part.StartsWith('.') && part != ".github")
                return true;
            if (part is "node_modules" or "bin" or "obj" or "dist" or "build"
                or ".git" or "__pycache__" or "vendor" or "packages")
                return true;
        }

        return false;
    }
}

/// <summary>
/// A collection of loaded agent definitions with resolution logic.
/// </summary>
public sealed class AgentCollection
{
    public AgentDefinition? GlobalAgent { get; set; }
    public List<AgentDefinition> Agents { get; } = new();

    public void AddAgent(AgentDefinition agent) => Agents.Add(agent);

    /// <summary>
    /// Gets a specific named agent.
    /// </summary>
    public AgentDefinition? GetByName(string name) =>
        Agents.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns all agents applicable to a given working path, sorted by scope specificity.
    /// Global agent instructions are always included first.
    /// </summary>
    public List<AgentDefinition> ResolveForPath(string workingPath)
    {
        var result = new List<AgentDefinition>();

        if (GlobalAgent != null)
            result.Add(GlobalAgent);

        var applicable = Agents
            .Where(a => a.Scope.Applies(workingPath))
            .OrderByDescending(a => a.Scope.BasePath.Length) // Most specific first
            .ToList();

        result.AddRange(applicable);
        return result;
    }

    /// <summary>
    /// Returns all available agent names.
    /// </summary>
    public List<string> GetAgentNames() =>
        Agents.Select(a => a.Name).Distinct().ToList();

    /// <summary>
    /// Builds a combined system prompt from all applicable agents for a given path.
    /// </summary>
    public string BuildSystemPrompt(string workingPath, string? specificAgentName = null)
    {
        var parts = new List<string>();

        // Always include global instructions
        if (GlobalAgent != null && !string.IsNullOrWhiteSpace(GlobalAgent.Instructions))
            parts.Add(GlobalAgent.Instructions);

        if (specificAgentName != null)
        {
            var specific = GetByName(specificAgentName);
            if (specific != null && !string.IsNullOrWhiteSpace(specific.Instructions))
                parts.Add(specific.Instructions);
        }
        else
        {
            // Include all scoped agents that apply
            var scoped = Agents
                .Where(a => a.Scope.Applies(workingPath) && !string.IsNullOrWhiteSpace(a.Instructions))
                .OrderByDescending(a => a.Scope.BasePath.Length);

            foreach (var agent in scoped)
                parts.Add(agent.Instructions);
        }

        return string.Join("\n\n---\n\n", parts);
    }
}
