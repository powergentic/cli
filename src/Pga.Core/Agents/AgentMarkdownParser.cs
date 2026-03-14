using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Pga.Core.Agents;

/// <summary>
/// Parses agent markdown files (AGENTS.md and *.agent.md) into AgentDefinition objects.
/// Supports YAML frontmatter delimited by --- lines.
/// </summary>
public sealed class AgentMarkdownParser
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Parses an AGENTS.md file (global instructions without frontmatter).
    /// </summary>
    public AgentDefinition ParseGlobalAgentFile(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var (frontmatter, body) = ExtractFrontmatter(content);

        var agent = new AgentDefinition
        {
            Name = "global",
            Instructions = body.Trim(),
            IsGlobalAgent = true,
            SourcePath = filePath,
            Scope = new AgentScope
            {
                BasePath = Path.GetDirectoryName(filePath) ?? string.Empty,
                IsGlobal = true
            }
        };

        // AGENTS.md can optionally have frontmatter too
        if (frontmatter != null)
            ApplyFrontmatter(agent, frontmatter);

        return agent;
    }

    /// <summary>
    /// Parses a *.agent.md file with YAML frontmatter and markdown body.
    /// </summary>
    public AgentDefinition ParseAgentFile(string filePath, string scopeBasePath)
    {
        var content = File.ReadAllText(filePath);
        var (frontmatter, body) = ExtractFrontmatter(content);

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        // Remove .agent suffix: "my-helper.agent.md" → "my-helper"
        if (fileName.EndsWith(".agent", StringComparison.OrdinalIgnoreCase))
            fileName = fileName[..^6];

        var agent = new AgentDefinition
        {
            Name = fileName,
            Instructions = body.Trim(),
            IsGlobalAgent = false,
            SourcePath = filePath,
            Scope = new AgentScope
            {
                BasePath = scopeBasePath,
                IsGlobal = IsRootScope(filePath, scopeBasePath)
            }
        };

        if (frontmatter != null)
            ApplyFrontmatter(agent, frontmatter);

        return agent;
    }

    /// <summary>
    /// Extracts YAML frontmatter (between --- delimiters) and the markdown body.
    /// </summary>
    public static (string? Frontmatter, string Body) ExtractFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            return (null, content);

        var endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (null, content);

        var frontmatter = content[3..endIndex].Trim();
        var body = content[(endIndex + 4)..]; // Skip past \n---

        return (frontmatter, body);
    }

    private void ApplyFrontmatter(AgentDefinition agent, string yaml)
    {
        try
        {
            var data = YamlDeserializer.Deserialize<AgentFrontmatter>(yaml);
            if (data == null) return;

            if (!string.IsNullOrWhiteSpace(data.Name))
                agent.Name = data.Name;
            if (!string.IsNullOrWhiteSpace(data.Description))
                agent.Description = data.Description;
            if (!string.IsNullOrWhiteSpace(data.Profile))
                agent.Profile = data.Profile;

            if (data.Tools?.Count > 0)
                agent.Tools = data.Tools;
            if (data.DisabledTools?.Count > 0)
                agent.DisabledTools = data.DisabledTools;

            if (data.Metadata != null)
            {
                foreach (var (key, value) in data.Metadata)
                    agent.Metadata[key] = value;
            }
        }
        catch (Exception)
        {
            // If YAML parsing fails, treat entire content as instructions
        }
    }

    private static bool IsRootScope(string filePath, string scopeBasePath)
    {
        var fileDir = Path.GetDirectoryName(filePath) ?? string.Empty;
        // Consider it root scope if it's directly under the base path or in a default agent folder
        var agentsDir = Path.Combine(scopeBasePath, StaticValues.AgentSubfolderName);

        // Check if file is in one of the default agent folders (e.g., .powergentic/agents/)
        foreach (var defaultFolder in StaticValues.DefaultAgentFolders)
        {
            var defaultAgentsDir = Path.Combine(scopeBasePath, defaultFolder, StaticValues.AgentSubfolderName);
            if (fileDir.Equals(defaultAgentsDir, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Internal model for deserializing YAML frontmatter.
    /// </summary>
    private sealed class AgentFrontmatter
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Profile { get; set; }
        public List<string>? Tools { get; set; }
        public List<string>? DisabledTools { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
