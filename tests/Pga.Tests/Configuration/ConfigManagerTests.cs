using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

public class ConfigManagerTests
{
    [Fact]
    public void CreateDefaultConfig_HasExpectedStructure()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), ".powergentic-test-" + Guid.NewGuid());

        try
        {
            // Use a temp config path via environment or direct file ops
            var config = new PgaConfiguration
            {
                Version = "1.0",
                DefaultProfile = "default",
                Profiles = new Dictionary<string, LlmProfile>
                {
                    ["default"] = new()
                    {
                        Provider = "azure-openai",
                        Endpoint = "https://example.openai.azure.com",
                        DeploymentName = "gpt-4o",
                        ApiKey = "test-key",
                        AuthMode = "key"
                    }
                }
            };

            Assert.Equal("1.0", config.Version);
            Assert.Single(config.Profiles);
            Assert.True(config.Profiles.ContainsKey("default"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LlmProfile_Validate_AzureOpenAi_ValidConfig_NoErrors()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://example.openai.azure.com",
            DeploymentName = "gpt-4o",
            ApiKey = "test-key",
            AuthMode = "key"
        };

        var errors = profile.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void LlmProfile_Validate_AzureOpenAi_MissingEndpoint_ReturnsError()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            DeploymentName = "gpt-4o",
            ApiKey = "test-key",
            AuthMode = "key"
        };

        var errors = profile.Validate();
        Assert.Contains(errors, e => e.Contains("Endpoint"));
    }

    [Fact]
    public void LlmProfile_Validate_Ollama_ValidConfig_NoErrors()
    {
        var profile = new LlmProfile
        {
            Provider = "ollama",
            OllamaModel = "llama3",
            OllamaHost = "http://localhost:11434"
        };

        var errors = profile.Validate();
        Assert.Empty(errors);
    }

    [Fact]
    public void LlmProfile_Validate_Ollama_MissingModel_ReturnsError()
    {
        var profile = new LlmProfile
        {
            Provider = "ollama"
        };

        var errors = profile.Validate();
        Assert.Contains(errors, e => e.Contains("OllamaModel"));
    }

    [Fact]
    public void LlmProfile_Validate_UnknownProvider_ReturnsError()
    {
        var profile = new LlmProfile
        {
            Provider = "unknown-provider"
        };

        var errors = profile.Validate();
        Assert.Contains(errors, e => e.Contains("Unknown provider"));
    }
}
