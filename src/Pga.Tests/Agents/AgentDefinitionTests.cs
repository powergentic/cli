using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentDefinitionTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var agent = new AgentDefinition();

        Assert.Equal("default", agent.Name);
        Assert.Null(agent.Description);
        Assert.Equal(string.Empty, agent.Instructions);
        Assert.Null(agent.Profile);
        Assert.Empty(agent.Tools);
        Assert.Empty(agent.DisabledTools);
        Assert.Null(agent.SourcePath);
        Assert.False(agent.IsGlobalAgent);
        Assert.Empty(agent.Metadata);
        Assert.NotNull(agent.Scope);
    }

    [Fact]
    public void CanSet_AllProperties()
    {
        var agent = new AgentDefinition
        {
            Name = "test-agent",
            Description = "A test agent",
            Instructions = "Do testing",
            Profile = "gpt4",
            Tools = new List<string> { "file_read", "file_write" },
            DisabledTools = new List<string> { "shell_execute" },
            SourcePath = "/path/to/agent.md",
            IsGlobalAgent = false,
            Metadata = new Dictionary<string, string> { ["key"] = "value" },
            Scope = new AgentScope { IsGlobal = true }
        };

        Assert.Equal("test-agent", agent.Name);
        Assert.Equal("A test agent", agent.Description);
        Assert.Equal("Do testing", agent.Instructions);
        Assert.Equal("gpt4", agent.Profile);
        Assert.Equal(2, agent.Tools.Count);
        Assert.Single(agent.DisabledTools);
        Assert.Equal("/path/to/agent.md", agent.SourcePath);
        Assert.False(agent.IsGlobalAgent);
        Assert.Single(agent.Metadata);
        Assert.True(agent.Scope.IsGlobal);
    }
}
