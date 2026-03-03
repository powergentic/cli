using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentMarkdownParserExtendedTests
{
    private readonly AgentMarkdownParser _parser = new();

    [Fact]
    public void ExtractFrontmatter_WithContentNotStartingWithDashes_ReturnsNullFrontmatter()
    {
        var content = "Some text before\n---\nname: test\n---\n# Body";

        var (frontmatter, body) = AgentMarkdownParser.ExtractFrontmatter(content);

        Assert.Null(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void ExtractFrontmatter_WithOnlyOpeningDashes_ReturnsNullFrontmatter()
    {
        var content = "---\nname: test\nNo closing dashes";

        var (frontmatter, body) = AgentMarkdownParser.ExtractFrontmatter(content);

        Assert.Null(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void ExtractFrontmatter_EmptyFrontmatter_ReturnsEmptyString()
    {
        var content = "---\n---\n# Body content";

        var (frontmatter, body) = AgentMarkdownParser.ExtractFrontmatter(content);

        Assert.NotNull(frontmatter);
        Assert.Equal(string.Empty, frontmatter);
        Assert.Contains("Body content", body);
    }

    [Fact]
    public void ParseAgentFile_WithDisabledTools_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "safe-agent.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: safe-agent
                disabledTools:
                  - shell_execute
                  - file_write
                  - file_edit
                ---
                # Safe Agent
                Only reads, never writes.
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("safe-agent", agent.Name);
            Assert.Equal(3, agent.DisabledTools.Count);
            Assert.Contains("shell_execute", agent.DisabledTools);
            Assert.Contains("file_write", agent.DisabledTools);
            Assert.Contains("file_edit", agent.DisabledTools);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_WithMetadata_ParsesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "meta-agent.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: meta-agent
                metadata:
                  version: "2.0"
                  category: testing
                ---
                # Meta Agent
                An agent with metadata.
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("meta-agent", agent.Name);
            Assert.Equal(2, agent.Metadata.Count);
            Assert.Equal("2.0", agent.Metadata["version"]);
            Assert.Equal("testing", agent.Metadata["category"]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_WithMalformedYaml_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "bad-yaml.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: [invalid yaml
                description: {broken
                ---
                # Bad Yaml Agent
                Body content is fine.
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            // Should not throw — falls back to filename-derived name
            Assert.Equal("bad-yaml", agent.Name);
            Assert.Contains("Body content is fine", agent.Instructions);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_NoFrontmatter_UsesFilename()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "simple-helper.agent.md");
            File.WriteAllText(agentFile, "# Simple Helper\n\nJust a simple agent with no frontmatter.");

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("simple-helper", agent.Name);
            Assert.Null(agent.Description);
            Assert.Null(agent.Profile);
            Assert.Empty(agent.Tools);
            Assert.Contains("Just a simple agent", agent.Instructions);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_FrontmatterNameOverridesFilename()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "file-name.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: custom-name
                ---
                # Custom Named Agent
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("custom-name", agent.Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_SetsSourcePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "traced.agent.md");
            File.WriteAllText(agentFile, "# Traced Agent\nTraced instructions.");

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal(agentFile, agent.SourcePath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_WithToolsAndDisabledTools_ParsesBoth()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "mixed.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: mixed
                tools:
                  - file_read
                  - grep_search
                disabledTools:
                  - shell_execute
                ---
                # Mixed Agent
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal(2, agent.Tools.Count);
            Assert.Contains("file_read", agent.Tools);
            Assert.Contains("grep_search", agent.Tools);
            Assert.Single(agent.DisabledTools);
            Assert.Contains("shell_execute", agent.DisabledTools);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseGlobalAgentFile_WithFrontmatter_AppliesFrontmatter()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                ---
                description: Global project instructions
                profile: gpt4
                ---
                # Global Instructions
                Follow these rules.
                """);

            var agent = _parser.ParseGlobalAgentFile(tempFile);

            Assert.Equal("global", agent.Name); // Always "global" for AGENTS.md
            Assert.True(agent.IsGlobalAgent);
            Assert.Equal("Global project instructions", agent.Description);
            Assert.Equal("gpt4", agent.Profile);
            Assert.Contains("Follow these rules", agent.Instructions);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseAgentFile_ScopeIsSetCorrectly_ForRootAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var agentsDir = Path.Combine(tempDir, "agents");
        Directory.CreateDirectory(agentsDir);

        try
        {
            var agentFile = Path.Combine(agentsDir, "root.agent.md");
            File.WriteAllText(agentFile, "# Root Agent");

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.True(agent.Scope.IsGlobal);
            Assert.Equal(tempDir, agent.Scope.BasePath);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_ScopeIsSetCorrectly_ForGitHubAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var githubAgentsDir = Path.Combine(tempDir, ".github", "agents");
        Directory.CreateDirectory(githubAgentsDir);

        try
        {
            var agentFile = Path.Combine(githubAgentsDir, "github.agent.md");
            File.WriteAllText(agentFile, "# GitHub Agent");

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.True(agent.Scope.IsGlobal);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
