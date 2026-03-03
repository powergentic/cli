using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentLoaderTests
{
    [Fact]
    public void LoadAgents_EmptyDirectory_ReturnsEmptyCollection()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Null(collection.GlobalAgent);
            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_WithAgentsMd_LoadsGlobalAgent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "AGENTS.md"), "# Global\nBe nice.");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.NotNull(collection.GlobalAgent);
            Assert.Equal("global", collection.GlobalAgent!.Name);
            Assert.Contains("Be nice", collection.GlobalAgent.Instructions);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_WithAgentsFolder_LoadsCustomAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var agentsDir = Path.Combine(tempDir, "agents");
        Directory.CreateDirectory(agentsDir);

        try
        {
            File.WriteAllText(Path.Combine(agentsDir, "helper.agent.md"), """
                ---
                name: helper
                description: A helper
                ---
                # Helper
                Help with stuff.
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Single(collection.Agents);
            Assert.Equal("helper", collection.Agents[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_WithScopedAgents_LoadsWithCorrectScope()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var subDir = Path.Combine(tempDir, "src", "frontend");
        var scopedAgentsDir = Path.Combine(subDir, "agents");
        Directory.CreateDirectory(scopedAgentsDir);

        try
        {
            File.WriteAllText(Path.Combine(scopedAgentsDir, "frontend.agent.md"), """
                ---
                name: frontend
                ---
                # Frontend Agent
                Handle frontend tasks.
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Single(collection.Agents);
            var agent = collection.Agents[0];
            Assert.Equal("frontend", agent.Name);
            Assert.False(agent.Scope.IsGlobal);

            // Should apply to files within src/frontend/
            Assert.True(agent.Scope.Applies(Path.Combine(subDir, "index.ts")));

            // Should NOT apply to files outside src/frontend/
            Assert.False(agent.Scope.Applies(Path.Combine(tempDir, "src", "backend", "app.cs")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void BuildSystemPrompt_CombinesGlobalAndSpecificAgent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var agentsDir = Path.Combine(tempDir, "agents");
        Directory.CreateDirectory(agentsDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "AGENTS.md"), "# Global\nFollow standards.");
            File.WriteAllText(Path.Combine(agentsDir, "reviewer.agent.md"), """
                ---
                name: reviewer
                ---
                # Reviewer
                Review code carefully.
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            var prompt = collection.BuildSystemPrompt(tempDir, "reviewer");

            Assert.Contains("Follow standards", prompt);
            Assert.Contains("Review code carefully", prompt);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
