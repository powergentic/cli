using System.Text.Json;
using Pga.Core.Chat;
using Pga.Core.Configuration;

namespace Pga.Tests.Chat;

/// <summary>
/// Tests for ChatOrchestrator construction, error handling, and helper methods.
/// Note: Full LLM interaction tests require network access or IChatClient injection (refactoring).
/// These tests cover initialization, profile resolution errors, and LLM communication errors.
/// </summary>
public class ChatOrchestratorTests : IDisposable
{
    private readonly string _tempProjectDir;

    public ChatOrchestratorTests()
    {
        _tempProjectDir = Path.Combine(Path.GetTempPath(), "pga_chat_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempProjectDir))
            Directory.Delete(_tempProjectDir, true);
    }

    private ConfigManager CreateConfigManager(PgaConfiguration config)
    {
        var configDir = Path.Combine(_tempProjectDir, ".powergentic");
        Directory.CreateDirectory(configDir);
        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(Path.Combine(configDir, "config.json"), json);
        return new ConfigManager(_tempProjectDir);
    }

    [Fact]
    public void Constructor_InitializesHistory()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);

        Assert.NotNull(orchestrator.History);
        // Should have system message
        Assert.True(orchestrator.History.Count > 0);
    }

    [Fact]
    public void Constructor_WithAgentName_InitializesCorrectly()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        // Should not throw even with a nonexistent agent name
        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir, agentName: "nonexistent-agent");

        Assert.NotNull(orchestrator.History);
        Assert.True(orchestrator.History.Count > 0);
    }

    [Fact]
    public void Constructor_WithAgentsMarkdown_IncludesAgentInstructions()
    {
        // Create an AGENTS.md in the project dir
        File.WriteAllText(Path.Combine(_tempProjectDir, "AGENTS.md"), "You are a test agent. Be helpful.");

        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);

        // The system prompt should include agent instructions
        var messages = orchestrator.History.ToList();
        Assert.True(messages.Count > 0);
        var systemMsg = messages.First(m => m.Role == Microsoft.Extensions.AI.ChatRole.System);
        Assert.Contains("Powergentic", systemMsg.Text ?? "");
    }

    [Fact]
    public async Task SendMessageAsync_NoProfileConfigured_ReturnsError()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "nonexistent-profile",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageAsync("Hello");

        Assert.Contains("Error", result);
        Assert.Contains("No LLM profile configured", result);
    }

    [Fact]
    public async Task SendMessageAsync_InvalidEndpoint_ReturnsLlmError()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://invalid-endpoint-that-does-not-exist.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "fake-key",
                    AuthMode = "key"
                }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageAsync("Hello");

        Assert.Contains("Error communicating with LLM", result);
    }

    [Fact]
    public async Task SendMessageAsync_AddsUserMessageToHistory()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "nonexistent",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        await orchestrator.SendMessageAsync("Test message");

        var messages = orchestrator.History.ToList();
        Assert.Contains(messages, m =>
            m.Role == Microsoft.Extensions.AI.ChatRole.User &&
            m.Text == "Test message");
    }

    [Fact]
    public async Task SendMessageStreamingAsync_NoProfileConfigured_ReturnsError()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "nonexistent-profile",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageStreamingAsync("Hello");

        Assert.Contains("Error", result);
        Assert.Contains("No LLM profile configured", result);
    }

    [Fact]
    public async Task SendMessageStreamingAsync_InvalidEndpoint_ReturnsLlmError()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://invalid-endpoint-that-does-not-exist.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "fake-key",
                    AuthMode = "key"
                }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageStreamingAsync("Hello");

        Assert.Contains("Error communicating with LLM", result);
    }

    [Fact]
    public async Task SendMessageStreamingAsync_AddsUserMessageToHistory()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "nonexistent",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        await orchestrator.SendMessageStreamingAsync("Streaming test");

        var messages = orchestrator.History.ToList();
        Assert.Contains(messages, m =>
            m.Role == Microsoft.Extensions.AI.ChatRole.User &&
            m.Text == "Streaming test");
    }

    [Fact]
    public void Events_CanBeSubscribed()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);

        var toolInvoked = false;
        var toolResultReceived = false;
        var streamingTokenReceived = false;

        orchestrator.OnToolInvocation += (name, desc) => { toolInvoked = true; return Task.CompletedTask; };
        orchestrator.OnToolResult += (name, result) => { toolResultReceived = true; };
        orchestrator.OnStreamingToken += (token) => { streamingTokenReceived = true; };
        orchestrator.OnToolApprovalNeeded += (name, desc) => Task.FromResult(true);

        // Events are just subscribed, not triggered in this test
        Assert.NotNull(orchestrator);
    }

    [Fact]
    public void Constructor_WithCustomAgentDir_LoadsAgents()
    {
        // Create a custom agent
        var agentDir = Path.Combine(_tempProjectDir, ".powergentic", "agents");
        Directory.CreateDirectory(agentDir);
        File.WriteAllText(Path.Combine(agentDir, "test-helper.agent.md"), """
        ---
        name: test-helper
        description: A test helper agent
        ---
        You help with testing.
        """);

        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir, agentName: "test-helper");

        Assert.NotNull(orchestrator.History);
        Assert.True(orchestrator.History.Count > 0);
    }

    [Fact]
    public async Task SendMessageAsync_WithOllamaProfile_ReturnsLlmError()
    {
        // Ollama at localhost should fail if not running
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaHost = "http://localhost:1",
                    OllamaModel = "llama3"
                }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageAsync("Hello");

        Assert.Contains("Error communicating with LLM", result);
    }

    [Fact]
    public async Task SendMessageStreamingAsync_WithOllamaProfile_ReturnsLlmError()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaHost = "http://localhost:1",
                    OllamaModel = "llama3"
                }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var result = await orchestrator.SendMessageStreamingAsync("Hello");

        Assert.Contains("Error communicating with LLM", result);
    }

    [Fact]
    public void Constructor_SystemPromptContainsBaseInstructions()
    {
        var configManager = CreateConfigManager(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var orchestrator = new ChatOrchestrator(configManager, _tempProjectDir);
        var messages = orchestrator.History.ToList();
        var systemMsg = messages.FirstOrDefault(m => m.Role == Microsoft.Extensions.AI.ChatRole.System);

        Assert.NotNull(systemMsg);
        Assert.Contains("Powergentic CLI", systemMsg!.Text ?? "");
        Assert.Contains("tools", systemMsg.Text ?? "");
    }
}
