using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentLoaderExtendedTests
{
    [Fact]
    public void LoadAgents_WithGitHubAgentsFolder_LoadsAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var githubAgentsDir = Path.Combine(tempDir, ".github", "agents");
        Directory.CreateDirectory(githubAgentsDir);

        try
        {
            File.WriteAllText(Path.Combine(githubAgentsDir, "github-bot.agent.md"), """
                ---
                name: github-bot
                description: A GitHub agent
                ---
                # GitHub Bot
                Help with GitHub.
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Single(collection.Agents);
            Assert.Equal("github-bot", collection.Agents[0].Name);
            Assert.True(collection.Agents[0].Scope.IsGlobal);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsNodeModules()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var nodeModulesAgents = Path.Combine(tempDir, "node_modules", "some-pkg", "agents");
        Directory.CreateDirectory(nodeModulesAgents);

        try
        {
            File.WriteAllText(Path.Combine(nodeModulesAgents, "pkg-agent.agent.md"), """
                ---
                name: pkg-agent
                ---
                # Package Agent
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsBinDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var binAgents = Path.Combine(tempDir, "bin", "Debug", "agents");
        Directory.CreateDirectory(binAgents);

        try
        {
            File.WriteAllText(Path.Combine(binAgents, "bin-agent.agent.md"), """
                ---
                name: bin-agent
                ---
                # Bin Agent
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsObjDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var objAgents = Path.Combine(tempDir, "obj", "agents");
        Directory.CreateDirectory(objAgents);

        try
        {
            File.WriteAllText(Path.Combine(objAgents, "obj-agent.agent.md"), """
                ---
                name: obj-agent
                ---
                # Obj Agent
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsHiddenDirectories()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var hiddenAgents = Path.Combine(tempDir, ".hidden", "agents");
        Directory.CreateDirectory(hiddenAgents);

        try
        {
            File.WriteAllText(Path.Combine(hiddenAgents, "hidden.agent.md"), """
                ---
                name: hidden
                ---
                # Hidden Agent
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_DoesNotSkipGitHubDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var githubAgents = Path.Combine(tempDir, ".github", "agents");
        Directory.CreateDirectory(githubAgents);

        try
        {
            File.WriteAllText(Path.Combine(githubAgents, "gh.agent.md"), "# GH Agent");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Single(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_CombinesGlobalAndRootAndScoped()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rootAgentsDir = Path.Combine(tempDir, "agents");
        var scopedDir = Path.Combine(tempDir, "src", "frontend", "agents");
        Directory.CreateDirectory(rootAgentsDir);
        Directory.CreateDirectory(scopedDir);

        try
        {
            // Global AGENTS.md
            File.WriteAllText(Path.Combine(tempDir, "AGENTS.md"), "# Global\nFollow standards.");

            // Root agent
            File.WriteAllText(Path.Combine(rootAgentsDir, "reviewer.agent.md"), """
                ---
                name: reviewer
                ---
                # Reviewer
                """);

            // Scoped agent
            File.WriteAllText(Path.Combine(scopedDir, "frontend.agent.md"), """
                ---
                name: frontend
                ---
                # Frontend Expert
                """);

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.NotNull(collection.GlobalAgent);
            Assert.Equal(2, collection.Agents.Count);

            var reviewer = collection.GetByName("reviewer");
            Assert.NotNull(reviewer);
            Assert.True(reviewer!.Scope.IsGlobal);

            var frontend = collection.GetByName("frontend");
            Assert.NotNull(frontend);
            Assert.False(frontend!.Scope.IsGlobal);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_MultipleAgentsInSameDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var agentsDir = Path.Combine(tempDir, "agents");
        Directory.CreateDirectory(agentsDir);

        try
        {
            File.WriteAllText(Path.Combine(agentsDir, "agent-a.agent.md"), "# Agent A");
            File.WriteAllText(Path.Combine(agentsDir, "agent-b.agent.md"), "# Agent B");
            File.WriteAllText(Path.Combine(agentsDir, "agent-c.agent.md"), "# Agent C");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Equal(3, collection.Agents.Count);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_IgnoresNonAgentMdFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var agentsDir = Path.Combine(tempDir, "agents");
        Directory.CreateDirectory(agentsDir);

        try
        {
            File.WriteAllText(Path.Combine(agentsDir, "valid.agent.md"), "# Valid Agent");
            File.WriteAllText(Path.Combine(agentsDir, "README.md"), "# README");
            File.WriteAllText(Path.Combine(agentsDir, "notes.txt"), "Some notes");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Single(collection.Agents);
            Assert.Equal("valid", collection.Agents[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolveAgentsForPath_ReturnsApplicableAgentsOnly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var frontendDir = Path.Combine(tempDir, "src", "frontend");
        var frontendAgentsDir = Path.Combine(frontendDir, "agents");
        var backendDir = Path.Combine(tempDir, "src", "backend");
        var backendAgentsDir = Path.Combine(backendDir, "agents");
        Directory.CreateDirectory(frontendAgentsDir);
        Directory.CreateDirectory(backendAgentsDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "AGENTS.md"), "# Global");

            File.WriteAllText(Path.Combine(frontendAgentsDir, "react.agent.md"), """
                ---
                name: react
                ---
                # React Expert
                """);

            File.WriteAllText(Path.Combine(backendAgentsDir, "dotnet.agent.md"), """
                ---
                name: dotnet
                ---
                # .NET Expert
                """);

            var loader = new AgentLoader();

            // Working in frontend directory
            var frontendAgents = loader.ResolveAgentsForPath(
                tempDir, Path.Combine(frontendDir, "src", "App.tsx"));

            // Should include global + react, but NOT dotnet
            Assert.Contains(frontendAgents, a => a.Name == "global");
            Assert.Contains(frontendAgents, a => a.Name == "react");
            Assert.DoesNotContain(frontendAgents, a => a.Name == "dotnet");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsDistDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var distAgents = Path.Combine(tempDir, "dist", "agents");
        Directory.CreateDirectory(distAgents);

        try
        {
            File.WriteAllText(Path.Combine(distAgents, "dist.agent.md"), "# Dist Agent");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsBuildDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var buildAgents = Path.Combine(tempDir, "build", "agents");
        Directory.CreateDirectory(buildAgents);

        try
        {
            File.WriteAllText(Path.Combine(buildAgents, "build.agent.md"), "# Build Agent");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsPycacheDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var pycacheAgents = Path.Combine(tempDir, "__pycache__", "agents");
        Directory.CreateDirectory(pycacheAgents);

        try
        {
            File.WriteAllText(Path.Combine(pycacheAgents, "cache.agent.md"), "# Cache Agent");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadAgents_SkipsVendorDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var vendorAgents = Path.Combine(tempDir, "vendor", "agents");
        Directory.CreateDirectory(vendorAgents);

        try
        {
            File.WriteAllText(Path.Combine(vendorAgents, "vendor.agent.md"), "# Vendor Agent");

            var loader = new AgentLoader();
            var collection = loader.LoadAgents(tempDir);

            Assert.Empty(collection.Agents);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
