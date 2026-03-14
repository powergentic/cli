using System.Text.Json;
using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

/// <summary>
/// Tests for PgaJsonContext source-generated serializer.
/// Exercising serialization/deserialization through PgaJsonContext.Default
/// covers the auto-generated serializer code paths.
/// </summary>
public class PgaJsonContextSerializationTests
{
    [Fact]
    public void Serialize_PgaConfiguration_ProducesValidJson()
    {
        var config = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "test",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["test"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "test-key",
                    AuthMode = "key",
                    DisplayName = "Test Profile"
                }
            }
        };

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);

        Assert.NotNull(json);
        Assert.Contains("version", json);
        Assert.Contains("defaultProfile", json);
        Assert.Contains("test", json);
        Assert.Contains("azure-openai", json);
    }

    [Fact]
    public void Deserialize_PgaConfiguration_RestoresAllFields()
    {
        var json = """
        {
            "version": "2.0",
            "defaultProfile": "myProfile",
            "profiles": {
                "myProfile": {
                    "provider": "ollama",
                    "ollamaModel": "llama3",
                    "ollamaHost": "http://localhost:11434",
                    "displayName": "My Ollama",
                    "maxTokens": 4096,
                    "temperature": 0.7,
                    "topP": 0.95,
                    "authMode": "key"
                }
            },
            "autoSelect": {
                "enabled": true,
                "rules": [
                    { "pattern": "*.py", "profile": "python-profile", "description": "Python" }
                ]
            },
            "toolSafety": {
                "mode": "auto-approve",
                "trustedPaths": ["/home/user/project"]
            },
            "ui": {
                "theme": "dark",
                "showToolCalls": false,
                "streamResponses": true
            }
        }
        """;

        var config = JsonSerializer.Deserialize(json, PgaJsonContext.Default.PgaConfiguration);

        Assert.NotNull(config);
        Assert.Equal("2.0", config!.Version);
        Assert.Equal("myProfile", config.DefaultProfile);
        Assert.Single(config.Profiles);
        Assert.Equal("ollama", config.Profiles["myProfile"].Provider);
        Assert.Equal("llama3", config.Profiles["myProfile"].OllamaModel);
        Assert.Equal("My Ollama", config.Profiles["myProfile"].DisplayName);
        Assert.Equal(4096, config.Profiles["myProfile"].MaxTokens);
        Assert.Equal(0.7f, config.Profiles["myProfile"].Temperature);
        Assert.Equal(0.95f, config.Profiles["myProfile"].TopP);
        Assert.True(config.AutoSelect.Enabled);
        Assert.Single(config.AutoSelect.Rules);
        Assert.Equal("*.py", config.AutoSelect.Rules[0].Pattern);
        Assert.Equal("auto-approve", config.ToolSafety.Mode);
        Assert.Single(config.ToolSafety.TrustedPaths);
        Assert.Equal("dark", config.Ui.Theme);
        Assert.False(config.Ui.ShowToolCalls);
    }

    [Fact]
    public void RoundTrip_PgaConfiguration_PreservesData()
    {
        var original = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "test-key",
                    AuthMode = "key",
                    DisplayName = "Azure GPT-4o"
                },
                ["ollama"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "codestral",
                    OllamaHost = "http://192.168.1.100:11434"
                }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "ollama", Description = "Python" },
                    new() { Pattern = "*", Profile = "default" }
                }
            },
            ToolSafety = new ToolSafetyConfig
            {
                Mode = "prompt-writes",
                TrustedPaths = new List<string> { "/path/a", "/path/b" }
            },
            Ui = new UiConfig
            {
                Theme = "monokai",
                ShowToolCalls = true,
                StreamResponses = false
            }
        };

        var json = JsonSerializer.Serialize(original, PgaJsonContext.Default.PgaConfiguration);
        var deserialized = JsonSerializer.Deserialize(json, PgaJsonContext.Default.PgaConfiguration);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Version, deserialized!.Version);
        Assert.Equal(original.DefaultProfile, deserialized.DefaultProfile);
        Assert.Equal(2, deserialized.Profiles.Count);
        Assert.Equal("azure-openai", deserialized.Profiles["default"].Provider);
        Assert.Equal("Azure GPT-4o", deserialized.Profiles["default"].DisplayName);
        Assert.Equal("codestral", deserialized.Profiles["ollama"].OllamaModel);
        Assert.True(deserialized.AutoSelect.Enabled);
        Assert.Equal(2, deserialized.AutoSelect.Rules.Count);
        Assert.Equal("prompt-writes", deserialized.ToolSafety.Mode);
        Assert.Equal(2, deserialized.ToolSafety.TrustedPaths.Count);
        Assert.Equal("monokai", deserialized.Ui.Theme);
        Assert.False(deserialized.Ui.StreamResponses);
    }

    [Fact]
    public void Serialize_LlmProfile_ProducesExpectedJson()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-ai-foundry",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            ApiKey = "key123",
            AuthMode = "entra",
            TenantId = "tenant-abc",
            ModelId = "model-id",
            ApiVersion = "2024-02-01"
        };

        var json = JsonSerializer.Serialize(profile, PgaJsonContext.Default.LlmProfile);

        Assert.Contains("azure-ai-foundry", json);
        Assert.Contains("entra", json);
        Assert.Contains("tenant-abc", json);
        Assert.Contains("model-id", json);
        Assert.Contains("2024-02-01", json);
    }

    [Fact]
    public void Deserialize_LlmProfile_RestoresAllFields()
    {
        var json = """
        {
            "provider": "azure-openai",
            "displayName": "My Profile",
            "endpoint": "https://test.openai.azure.com",
            "apiKey": "key",
            "deploymentName": "gpt-4o",
            "modelId": "model",
            "apiVersion": "2024-02-01",
            "authMode": "entra",
            "tenantId": "tenant123",
            "ollamaHost": "http://localhost:11434",
            "ollamaModel": null,
            "maxTokens": 2048,
            "temperature": 0.5,
            "topP": 0.9
        }
        """;

        var profile = JsonSerializer.Deserialize(json, PgaJsonContext.Default.LlmProfile);

        Assert.NotNull(profile);
        Assert.Equal("azure-openai", profile!.Provider);
        Assert.Equal("My Profile", profile.DisplayName);
        Assert.Equal("https://test.openai.azure.com", profile.Endpoint);
        Assert.Equal("key", profile.ApiKey);
        Assert.Equal("gpt-4o", profile.DeploymentName);
        Assert.Equal("model", profile.ModelId);
        Assert.Equal("2024-02-01", profile.ApiVersion);
        Assert.Equal("entra", profile.AuthMode);
        Assert.Equal("tenant123", profile.TenantId);
        Assert.Equal(2048, profile.MaxTokens);
        Assert.Equal(0.5f, profile.Temperature);
        Assert.Equal(0.9f, profile.TopP);
    }

    [Fact]
    public void Serialize_ToolSafetyConfig_Works()
    {
        var config = new ToolSafetyConfig
        {
            Mode = "auto-approve",
            TrustedPaths = new List<string> { "/a", "/b" }
        };

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.ToolSafetyConfig);

        Assert.Contains("auto-approve", json);
        Assert.Contains("/a", json);
    }

    [Fact]
    public void Serialize_UiConfig_Works()
    {
        var config = new UiConfig
        {
            Theme = "dark",
            ShowToolCalls = false,
            StreamResponses = true
        };

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.UiConfig);

        Assert.Contains("dark", json);
        Assert.Contains("false", json);
    }

    [Fact]
    public void Serialize_AutoSelectConfig_Works()
    {
        var config = new AutoSelectConfig
        {
            Enabled = true,
            Rules = new List<AutoSelectRule>
            {
                new() { Pattern = "*.cs", Profile = "csharp", Description = "C# files" }
            }
        };

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.AutoSelectConfig);

        Assert.Contains("true", json);
        Assert.Contains("*.cs", json);
    }

    [Fact]
    public void Serialize_AutoSelectRule_Works()
    {
        var rule = new AutoSelectRule
        {
            Pattern = "*.py",
            Profile = "python",
            Description = "Python files"
        };

        var json = JsonSerializer.Serialize(rule, PgaJsonContext.Default.AutoSelectRule);

        Assert.Contains("*.py", json);
        Assert.Contains("python", json);
        Assert.Contains("Python files", json);
    }

    [Fact]
    public void Deserialize_EmptyJson_ReturnsDefaults()
    {
        var config = JsonSerializer.Deserialize("{}", PgaJsonContext.Default.PgaConfiguration);

        Assert.NotNull(config);
        // Default values should be applied
        Assert.Equal("1.0", config!.Version);
        Assert.Equal("default", config.DefaultProfile);
    }

    [Fact]
    public void Serialize_NullOptionalFields_OmittedFromJson()
    {
        var profile = new LlmProfile
        {
            Provider = "ollama",
            OllamaModel = "llama3"
            // All optional fields are null
        };

        var json = JsonSerializer.Serialize(profile, PgaJsonContext.Default.LlmProfile);

        // WhenWritingNull is configured, so null fields should be omitted
        Assert.DoesNotContain("endpoint", json);
        Assert.DoesNotContain("apiKey", json);
        Assert.DoesNotContain("tenantId", json);
    }

    [Fact]
    public void RoundTrip_MinimalConfig_Works()
    {
        var config = new PgaConfiguration
        {
            DefaultProfile = "minimal",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["minimal"] = new LlmProfile { Provider = "ollama", OllamaModel = "tiny" }
            }
        };

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        var restored = JsonSerializer.Deserialize(json, PgaJsonContext.Default.PgaConfiguration);

        Assert.NotNull(restored);
        Assert.Equal("minimal", restored!.DefaultProfile);
        Assert.Equal("tiny", restored.Profiles["minimal"].OllamaModel);
    }
}
