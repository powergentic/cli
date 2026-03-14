using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentMarkdownParserTests
{
    private readonly AgentMarkdownParser _parser = new();

    [Fact]
    public void ExtractFrontmatter_WithValidFrontmatter_ReturnsParts()
    {
        var content = """
            ---
            name: test-agent
            description: A test agent
            ---
            # Test Agent

            This is the body.
            """;

        var (frontmatter, body) = AgentMarkdownParser.ExtractFrontmatter(content);

        Assert.NotNull(frontmatter);
        Assert.Contains("name: test-agent", frontmatter);
        Assert.Contains("Test Agent", body);
    }

    [Fact]
    public void ExtractFrontmatter_WithoutFrontmatter_ReturnsNullAndBody()
    {
        var content = "# Just Markdown\n\nNo frontmatter here.";

        var (frontmatter, body) = AgentMarkdownParser.ExtractFrontmatter(content);

        Assert.Null(frontmatter);
        Assert.Equal(content, body);
    }

    [Fact]
    public void ParseGlobalAgentFile_ReturnsGlobalAgent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "# Global Instructions\n\nBe helpful and thorough.");

            var agent = _parser.ParseGlobalAgentFile(tempFile);

            Assert.Equal("global", agent.Name);
            Assert.True(agent.IsGlobalAgent);
            Assert.Contains("Be helpful and thorough", agent.Instructions);
            Assert.True(agent.Scope.IsGlobal);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseAgentFile_WithFrontmatter_ParsesAllFields()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "my-helper.agent.md");
            File.WriteAllText(agentFile, """
                ---
                name: my-helper
                description: A helpful agent
                profile: gpt4
                tools:
                  - file_read
                  - shell_execute
                ---
                # My Helper

                You are a helpful coding assistant.
                """);

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("my-helper", agent.Name);
            Assert.Equal("A helpful agent", agent.Description);
            Assert.Equal("gpt4", agent.Profile);
            Assert.Contains("file_read", agent.Tools);
            Assert.Contains("shell_execute", agent.Tools);
            Assert.Contains("helpful coding assistant", agent.Instructions);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ParseAgentFile_DerivesNameFromFilename()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var agentFile = Path.Combine(tempDir, "code-reviewer.agent.md");
            File.WriteAllText(agentFile, "# Code Reviewer\n\nReview code carefully.");

            var agent = _parser.ParseAgentFile(agentFile, tempDir);

            Assert.Equal("code-reviewer", agent.Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
