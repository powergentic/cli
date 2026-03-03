using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentCollectionTests
{
    [Fact]
    public void GetByName_ExistingAgent_ReturnsAgent()
    {
        var collection = new AgentCollection();
        collection.AddAgent(new AgentDefinition { Name = "reviewer" });
        collection.AddAgent(new AgentDefinition { Name = "writer" });

        var result = collection.GetByName("reviewer");

        Assert.NotNull(result);
        Assert.Equal("reviewer", result!.Name);
    }

    [Fact]
    public void GetByName_CaseInsensitive_ReturnsAgent()
    {
        var collection = new AgentCollection();
        collection.AddAgent(new AgentDefinition { Name = "Code-Reviewer" });

        var result = collection.GetByName("code-reviewer");

        Assert.NotNull(result);
        Assert.Equal("Code-Reviewer", result!.Name);
    }

    [Fact]
    public void GetByName_NonExistent_ReturnsNull()
    {
        var collection = new AgentCollection();
        collection.AddAgent(new AgentDefinition { Name = "reviewer" });

        var result = collection.GetByName("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetAgentNames_ReturnsAllNames()
    {
        var collection = new AgentCollection();
        collection.AddAgent(new AgentDefinition { Name = "reviewer" });
        collection.AddAgent(new AgentDefinition { Name = "writer" });
        collection.AddAgent(new AgentDefinition { Name = "designer" });

        var names = collection.GetAgentNames();

        Assert.Equal(3, names.Count);
        Assert.Contains("reviewer", names);
        Assert.Contains("writer", names);
        Assert.Contains("designer", names);
    }

    [Fact]
    public void GetAgentNames_EmptyCollection_ReturnsEmptyList()
    {
        var collection = new AgentCollection();

        var names = collection.GetAgentNames();

        Assert.Empty(names);
    }

    [Fact]
    public void ResolveForPath_IncludesGlobalAgent()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                IsGlobalAgent = true,
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        var result = collection.ResolveForPath("/some/path");

        Assert.Single(result);
        Assert.Equal("global", result[0].Name);
    }

    [Fact]
    public void ResolveForPath_IncludesGlobalAndScopedAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var frontendDir = Path.Combine(tempDir, "frontend");
        Directory.CreateDirectory(frontendDir);

        try
        {
            var collection = new AgentCollection
            {
                GlobalAgent = new AgentDefinition
                {
                    Name = "global",
                    IsGlobalAgent = true,
                    Scope = new AgentScope { IsGlobal = true }
                }
            };

            collection.AddAgent(new AgentDefinition
            {
                Name = "frontend-expert",
                Scope = new AgentScope { BasePath = frontendDir, IsGlobal = false }
            });

            collection.AddAgent(new AgentDefinition
            {
                Name = "backend-expert",
                Scope = new AgentScope { BasePath = Path.Combine(tempDir, "backend"), IsGlobal = false }
            });

            var result = collection.ResolveForPath(Path.Combine(frontendDir, "index.ts"));

            // Should include global + frontend-expert, not backend-expert
            Assert.Equal(2, result.Count);
            Assert.Equal("global", result[0].Name);
            Assert.Equal("frontend-expert", result[1].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ResolveForPath_GlobalAgentsAlwaysApply()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                IsGlobalAgent = true,
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        collection.AddAgent(new AgentDefinition
        {
            Name = "root-agent",
            Scope = new AgentScope { IsGlobal = true }
        });

        var result = collection.ResolveForPath("/any/path/at/all");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void BuildSystemPrompt_GlobalOnly_ReturnsGlobalInstructions()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                Instructions = "Be helpful and follow standards.",
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        var prompt = collection.BuildSystemPrompt("/some/path");

        Assert.Contains("Be helpful and follow standards", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithSpecificAgent_IncludesBoth()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                Instructions = "Global rules.",
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        collection.AddAgent(new AgentDefinition
        {
            Name = "test-writer",
            Instructions = "Write comprehensive tests.",
            Scope = new AgentScope { IsGlobal = true }
        });

        var prompt = collection.BuildSystemPrompt("/some/path", "test-writer");

        Assert.Contains("Global rules", prompt);
        Assert.Contains("Write comprehensive tests", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_SpecificAgentNotFound_ReturnsGlobalOnly()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                Instructions = "Global rules.",
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        var prompt = collection.BuildSystemPrompt("/some/path", "nonexistent-agent");

        Assert.Contains("Global rules", prompt);
        Assert.DoesNotContain("---", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_NoGlobalAgent_ReturnsOnlySpecificAgent()
    {
        var collection = new AgentCollection();

        collection.AddAgent(new AgentDefinition
        {
            Name = "reviewer",
            Instructions = "Review code carefully.",
            Scope = new AgentScope { IsGlobal = true }
        });

        var prompt = collection.BuildSystemPrompt("/some/path", "reviewer");

        Assert.Contains("Review code carefully", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_NoAgentSpecified_IncludesAllScopedAgents()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var collection = new AgentCollection();

            collection.AddAgent(new AgentDefinition
            {
                Name = "agent-a",
                Instructions = "Instructions A",
                Scope = new AgentScope { BasePath = tempDir, IsGlobal = true }
            });

            collection.AddAgent(new AgentDefinition
            {
                Name = "agent-b",
                Instructions = "Instructions B",
                Scope = new AgentScope { BasePath = tempDir, IsGlobal = true }
            });

            var prompt = collection.BuildSystemPrompt(tempDir);

            Assert.Contains("Instructions A", prompt);
            Assert.Contains("Instructions B", prompt);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void BuildSystemPrompt_EmptyInstructions_AreSkipped()
    {
        var collection = new AgentCollection
        {
            GlobalAgent = new AgentDefinition
            {
                Name = "global",
                Instructions = "",
                Scope = new AgentScope { IsGlobal = true }
            }
        };

        collection.AddAgent(new AgentDefinition
        {
            Name = "reviewer",
            Instructions = "Review code.",
            Scope = new AgentScope { IsGlobal = true }
        });

        var prompt = collection.BuildSystemPrompt("/some/path", "reviewer");

        Assert.Contains("Review code", prompt);
        Assert.DoesNotContain("---", prompt); // No separator because global was empty
    }
}
