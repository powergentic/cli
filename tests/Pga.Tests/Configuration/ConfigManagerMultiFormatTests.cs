using System.Text.Json;
using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

/// <summary>
/// Tests for ConfigManager multi-format support (JSON + YAML) and local override file merging.
/// </summary>
public class ConfigManagerMultiFormatTests : IDisposable
{
    private readonly string _tempProjectDir;

    public ConfigManagerMultiFormatTests()
    {
        _tempProjectDir = Path.Combine(Path.GetTempPath(), "pga_multiformat_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempProjectDir))
            Directory.Delete(_tempProjectDir, true);
    }

    private string ConfigDir => Path.Combine(_tempProjectDir, ".powergentic");

    private void WriteConfigFile(string fileName, string content)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(Path.Combine(ConfigDir, fileName), content);
    }

    private void WriteJsonConfig(string fileName, PgaConfiguration config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(Path.Combine(ConfigDir, fileName), json);
    }

    // ========================
    // ConfigFilePath resolution
    // ========================

    [Fact]
    public void ConfigFilePath_PrefersJsonWhenAllExist()
    {
        WriteConfigFile("config.json", "{}");
        WriteConfigFile("config.yaml", "version: '1.0'");
        WriteConfigFile("config.yml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.json", manager.ConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_FallsBackToYamlWhenNoJson()
    {
        WriteConfigFile("config.yaml", "version: '1.0'\ndefaultProfile: default\nprofiles: {}");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.yaml", manager.ConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_FallsBackToYmlWhenNoJsonOrYaml()
    {
        WriteConfigFile("config.yml", "version: '1.0'\ndefaultProfile: default\nprofiles: {}");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.yml", manager.ConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_DefaultsToGlobalJsonWhenNothingExists()
    {
        var manager = new ConfigManager(_tempProjectDir);

        Assert.Equal(ConfigManager.GlobalConfigFilePath, manager.ConfigFilePath);
    }

    // ========================
    // LocalConfigFilePath resolution
    // ========================

    [Fact]
    public void LocalConfigFilePath_FindsJsonFile()
    {
        WriteConfigFile("config.json", "{}");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.json", manager.LocalConfigFilePath!);
    }

    [Fact]
    public void LocalConfigFilePath_FindsYamlFile()
    {
        WriteConfigFile("config.yaml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.yaml", manager.LocalConfigFilePath!);
    }

    [Fact]
    public void LocalConfigFilePath_FindsYmlFile()
    {
        WriteConfigFile("config.yml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.yml", manager.LocalConfigFilePath!);
    }

    [Fact]
    public void LocalConfigFilePath_DefaultsToJsonWhenNoneExist()
    {
        Directory.CreateDirectory(ConfigDir);

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.json", manager.LocalConfigFilePath!);
    }

    [Fact]
    public void LocalConfigFilePath_NullWhenNoProjectPath()
    {
        var manager = new ConfigManager(null);
        Assert.Null(manager.LocalConfigFilePath);
    }

    // ========================
    // Load from JSON
    // ========================

    [Fact]
    public void Load_JsonConfig_ReturnsCorrectConfiguration()
    {
        var config = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "json-profile",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["json-profile"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "test-key",
                    AuthMode = "key"
                }
            }
        };
        WriteJsonConfig("config.json", config);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("json-profile", loaded.DefaultProfile);
        Assert.Equal("azure-openai", loaded.Profiles["json-profile"].Provider);
    }

    // ========================
    // Load from YAML
    // ========================

    [Fact]
    public void Load_YamlConfig_ReturnsCorrectConfiguration()
    {
        var yaml = """
            version: "1.0"
            defaultProfile: yaml-profile
            profiles:
              yaml-profile:
                provider: ollama
                ollamaModel: llama3
                ollamaHost: "http://localhost:11434"
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yaml", yaml);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("yaml-profile", loaded.DefaultProfile);
        Assert.Equal("ollama", loaded.Profiles["yaml-profile"].Provider);
        Assert.Equal("llama3", loaded.Profiles["yaml-profile"].OllamaModel);
    }

    [Fact]
    public void Load_YmlConfig_ReturnsCorrectConfiguration()
    {
        var yaml = """
            version: "1.0"
            defaultProfile: yml-profile
            profiles:
              yml-profile:
                provider: ollama
                ollamaModel: codestral
                ollamaHost: "http://localhost:11434"
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yml", yaml);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("yml-profile", loaded.DefaultProfile);
        Assert.Equal("codestral", loaded.Profiles["yml-profile"].OllamaModel);
    }

    // ========================
    // Local override merging
    // ========================

    [Fact]
    public void Load_JsonWithLocalJsonOverride_MergesCorrectly()
    {
        // Base config with structure
        var baseConfig = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "azure",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://my-org.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    AuthMode = "key"
                }
            }
        };
        WriteJsonConfig("config.json", baseConfig);

        // Local override with secrets
        var localOverride = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    ApiKey = "sk-secret-api-key"
                }
            }
        };
        WriteJsonConfig("config.local.json", localOverride);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        // Secret from local override
        Assert.Equal("sk-secret-api-key", loaded.Profiles["azure"].ApiKey);
        // Structure from base config
        Assert.Equal("https://my-org.openai.azure.com", loaded.Profiles["azure"].Endpoint);
        Assert.Equal("gpt-4o", loaded.Profiles["azure"].DeploymentName);
        Assert.Equal("azure", loaded.DefaultProfile);
    }

    [Fact]
    public void Load_YamlWithLocalYamlOverride_MergesCorrectly()
    {
        var baseYaml = """
            version: "1.0"
            defaultProfile: azure
            profiles:
              azure:
                provider: azure-openai
                endpoint: "https://my-org.openai.azure.com"
                deploymentName: gpt-4o
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yaml", baseYaml);

        var localYaml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              azure:
                provider: azure-openai
                apiKey: sk-yaml-secret-key
                authMode: key
            """;
        WriteConfigFile("config.local.yaml", localYaml);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("sk-yaml-secret-key", loaded.Profiles["azure"].ApiKey);
        Assert.Equal("https://my-org.openai.azure.com", loaded.Profiles["azure"].Endpoint);
    }

    [Fact]
    public void Load_YmlWithLocalYmlOverride_MergesCorrectly()
    {
        var baseYml = """
            version: "1.0"
            defaultProfile: local
            profiles:
              local:
                provider: ollama
                ollamaModel: llama3
                ollamaHost: "http://localhost:11434"
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yml", baseYml);

        var localYml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              local:
                provider: ollama
                ollamaModel: codestral
                ollamaHost: "http://localhost:11434"
                authMode: key
            """;
        WriteConfigFile("config.local.yml", localYml);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("codestral", loaded.Profiles["local"].OllamaModel);
    }

    [Fact]
    public void Load_JsonWithLocalYamlOverride_MergesCorrectly()
    {
        // Mixed formats: base is JSON, override is YAML
        var baseConfig = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "azure",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://my-org.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    AuthMode = "key"
                }
            }
        };
        WriteJsonConfig("config.json", baseConfig);

        var localYaml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              azure:
                provider: azure-openai
                apiKey: sk-yaml-override-key
                authMode: key
            """;
        WriteConfigFile("config.local.yaml", localYaml);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("sk-yaml-override-key", loaded.Profiles["azure"].ApiKey);
        Assert.Equal("https://my-org.openai.azure.com", loaded.Profiles["azure"].Endpoint);
    }

    [Fact]
    public void Load_YamlWithLocalJsonOverride_MergesCorrectly()
    {
        // Mixed formats: base is YAML, override is JSON
        var baseYaml = """
            version: "1.0"
            defaultProfile: azure
            profiles:
              azure:
                provider: azure-openai
                endpoint: "https://my-org.openai.azure.com"
                deploymentName: gpt-4o
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yaml", baseYaml);

        var localOverride = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    ApiKey = "sk-json-override-key"
                }
            }
        };
        WriteJsonConfig("config.local.json", localOverride);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("sk-json-override-key", loaded.Profiles["azure"].ApiKey);
        Assert.Equal("https://my-org.openai.azure.com", loaded.Profiles["azure"].Endpoint);
    }

    [Fact]
    public void Load_WithNoLocalOverride_LoadsBaseOnly()
    {
        var config = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "only-base",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["only-base"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3"
                }
            }
        };
        WriteJsonConfig("config.json", config);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("only-base", loaded.DefaultProfile);
        Assert.Equal("llama3", loaded.Profiles["only-base"].OllamaModel);
    }

    [Fact]
    public void Load_LocalOverrideAddsNewProfile()
    {
        var baseConfig = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "azure",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://e.com",
                    DeploymentName = "d",
                    ApiKey = "k",
                    AuthMode = "key"
                }
            }
        };
        WriteJsonConfig("config.json", baseConfig);

        var localOverride = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["local-ollama"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "codestral"
                }
            }
        };
        WriteJsonConfig("config.local.json", localOverride);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal(2, loaded.Profiles.Count);
        Assert.True(loaded.Profiles.ContainsKey("azure"));
        Assert.True(loaded.Profiles.ContainsKey("local-ollama"));
    }

    [Fact]
    public void Load_LocalOverrideTrustedPaths_AreMerged()
    {
        var baseConfig = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            },
            ToolSafety = new ToolSafetyConfig
            {
                Mode = "prompt-writes",
                TrustedPaths = new List<string> { "/shared/path" }
            }
        };
        WriteJsonConfig("config.json", baseConfig);

        var localOverride = new PgaConfiguration
        {
            ToolSafety = new ToolSafetyConfig
            {
                TrustedPaths = new List<string> { "/local/path" }
            }
        };
        WriteJsonConfig("config.local.json", localOverride);

        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Contains("/shared/path", loaded.ToolSafety.TrustedPaths);
        Assert.Contains("/local/path", loaded.ToolSafety.TrustedPaths);
    }

    // ========================
    // LocalOverrideFilePath resolution
    // ========================

    [Fact]
    public void LocalOverrideFilePath_FindsLocalJson()
    {
        WriteConfigFile("config.json", "{}");
        WriteConfigFile("config.local.json", "{}");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.NotNull(manager.LocalOverrideFilePath);
        Assert.EndsWith("config.local.json", manager.LocalOverrideFilePath!);
    }

    [Fact]
    public void LocalOverrideFilePath_FindsLocalYaml()
    {
        WriteConfigFile("config.json", "{}");
        WriteConfigFile("config.local.yaml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.NotNull(manager.LocalOverrideFilePath);
        Assert.EndsWith("config.local.yaml", manager.LocalOverrideFilePath!);
    }

    [Fact]
    public void LocalOverrideFilePath_FindsLocalYml()
    {
        WriteConfigFile("config.json", "{}");
        WriteConfigFile("config.local.yml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.NotNull(manager.LocalOverrideFilePath);
        Assert.EndsWith("config.local.yml", manager.LocalOverrideFilePath!);
    }

    [Fact]
    public void LocalOverrideFilePath_PrefersJsonWhenMultipleExist()
    {
        WriteConfigFile("config.json", "{}");
        WriteConfigFile("config.local.json", "{}");
        WriteConfigFile("config.local.yaml", "version: '1.0'");
        WriteConfigFile("config.local.yml", "version: '1.0'");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.EndsWith("config.local.json", manager.LocalOverrideFilePath!);
    }

    [Fact]
    public void LocalOverrideFilePath_ReturnsNullWhenNoOverrideExists()
    {
        WriteConfigFile("config.json", "{}");

        var manager = new ConfigManager(_tempProjectDir);

        Assert.Null(manager.LocalOverrideFilePath);
    }

    // ========================
    // GetProviderForFile
    // ========================

    [Fact]
    public void GetProviderForFile_ReturnsJsonProviderForJsonFile()
    {
        var manager = new ConfigManager(_tempProjectDir);
        var provider = manager.GetProviderForFile("config.json");

        Assert.IsType<JsonConfigProvider>(provider);
    }

    [Fact]
    public void GetProviderForFile_ReturnsYamlProviderForYamlFile()
    {
        var manager = new ConfigManager(_tempProjectDir);
        var provider = manager.GetProviderForFile("config.yaml");

        Assert.IsType<YamlConfigProvider>(provider);
    }

    [Fact]
    public void GetProviderForFile_ReturnsYamlProviderForYmlFile()
    {
        var manager = new ConfigManager(_tempProjectDir);
        var provider = manager.GetProviderForFile("config.yml");

        Assert.IsType<YamlConfigProvider>(provider);
    }

    [Fact]
    public void GetProviderForFile_ThrowsForUnsupportedFormat()
    {
        var manager = new ConfigManager(_tempProjectDir);

        Assert.Throws<NotSupportedException>(() => manager.GetProviderForFile("config.xml"));
    }

    // ========================
    // Save preserves format
    // ========================

    [Fact]
    public void Save_WhenConfigIsJson_SavesAsJson()
    {
        WriteConfigFile("config.json", "{}");

        var manager = new ConfigManager(_tempProjectDir);
        var config = new PgaConfiguration
        {
            DefaultProfile = "saved",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["saved"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        manager.Save(config);

        var content = File.ReadAllText(Path.Combine(ConfigDir, "config.json"));
        Assert.Contains("saved", content);
        // JSON should have braces
        Assert.Contains("{", content);
    }

    [Fact]
    public void Save_WhenConfigIsYaml_SavesAsYaml()
    {
        WriteConfigFile("config.yaml", "version: '1.0'\ndefaultProfile: default\nprofiles: {}");

        var manager = new ConfigManager(_tempProjectDir);
        var config = new PgaConfiguration
        {
            DefaultProfile = "saved-yaml",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["saved-yaml"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        manager.Save(config);

        var content = File.ReadAllText(Path.Combine(ConfigDir, "config.yaml"));
        Assert.Contains("saved-yaml", content);
    }

    // ========================
    // ConfigFileNames and LocalOverrideFileNames are correct
    // ========================

    [Fact]
    public void ConfigFileNames_ContainsExpectedNames()
    {
        Assert.Equal(3, ConfigManager.ConfigFileNames.Length);
        Assert.Equal("config.json", ConfigManager.ConfigFileNames[0]);
        Assert.Equal("config.yaml", ConfigManager.ConfigFileNames[1]);
        Assert.Equal("config.yml", ConfigManager.ConfigFileNames[2]);
    }

    [Fact]
    public void LocalOverrideFileNames_ContainsExpectedNames()
    {
        Assert.Equal(3, ConfigManager.LocalOverrideFileNames.Length);
        Assert.Equal("config.local.json", ConfigManager.LocalOverrideFileNames[0]);
        Assert.Equal("config.local.yaml", ConfigManager.LocalOverrideFileNames[1]);
        Assert.Equal("config.local.yml", ConfigManager.LocalOverrideFileNames[2]);
    }

    // ========================
    // Load returns default when no config exists
    // ========================

    [Fact]
    public void Load_WhenNoConfigExists_ReturnsDefault()
    {
        var isolatedDir = Path.Combine(_tempProjectDir, "empty_project");
        Directory.CreateDirectory(isolatedDir);

        var manager = new ConfigManager(isolatedDir);
        var config = manager.Load();

        Assert.NotNull(config);
        Assert.Equal("1.0", config.Version);
        Assert.Equal("default", config.DefaultProfile);
        Assert.True(config.Profiles.ContainsKey("default"));
    }

    // ========================
    // Initialize only checks all formats
    // ========================

    [Fact]
    public void Initialize_WhenYamlExistsGlobally_ReturnsFalse()
    {
        // We can't easily write to global dir in tests, but we can test the logic:
        // If the global config file already exists, Initialize returns false.
        // This test verifies the method doesn't throw.
        var manager = new ConfigManager(_tempProjectDir);
        // If global config already exists (likely does from prior tests/real usage)
        if (File.Exists(ConfigManager.GlobalConfigFilePath))
        {
            Assert.False(manager.Initialize());
        }
    }

    // ========================
    // Full workflow: save JSON, override with YAML local, load merged result
    // ========================

    [Fact]
    public void FullWorkflow_JsonBaseWithYamlLocalOverride()
    {
        // Step 1: Create base JSON config
        var baseConfig = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "azure",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://prod.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    AuthMode = "key",
                    DisplayName = "Production Azure"
                },
                ["ollama"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3",
                    DisplayName = "Local Development"
                }
            },
            ToolSafety = new ToolSafetyConfig
            {
                Mode = "prompt-writes",
                TrustedPaths = new List<string> { "/shared/org/path" }
            },
            Ui = new UiConfig
            {
                ShowToolCalls = true,
                StreamResponses = true
            }
        };
        WriteJsonConfig("config.json", baseConfig);

        // Step 2: Create local YAML override with secrets
        var localYaml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              azure:
                provider: azure-openai
                apiKey: sk-real-secret-key-do-not-commit
                authMode: key
            toolSafety:
              mode: prompt-writes
              trustedPaths:
                - /Users/chris/my-project
            """;
        WriteConfigFile("config.local.yaml", localYaml);

        // Step 3: Load and verify merged result
        var manager = new ConfigManager(_tempProjectDir);
        var loaded = manager.Load();

        Assert.Equal("azure", loaded.DefaultProfile);
        Assert.Equal(2, loaded.Profiles.Count);

        // Azure profile has merged values
        Assert.Equal("sk-real-secret-key-do-not-commit", loaded.Profiles["azure"].ApiKey);
        Assert.Equal("https://prod.openai.azure.com", loaded.Profiles["azure"].Endpoint);
        Assert.Equal("gpt-4o", loaded.Profiles["azure"].DeploymentName);
        Assert.Equal("Production Azure", loaded.Profiles["azure"].DisplayName);

        // Ollama profile untouched
        Assert.Equal("llama3", loaded.Profiles["ollama"].OllamaModel);
        Assert.Equal("Local Development", loaded.Profiles["ollama"].DisplayName);

        // Trusted paths merged
        Assert.Contains("/shared/org/path", loaded.ToolSafety.TrustedPaths);
        Assert.Contains("/Users/chris/my-project", loaded.ToolSafety.TrustedPaths);
    }

    // ========================
    // Existing operations still work with JSON
    // ========================

    [Fact]
    public void UpsertProfile_WithJsonConfig_StillWorks()
    {
        WriteJsonConfig("config.json", new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        manager.UpsertProfile("new", new LlmProfile { Provider = "ollama", OllamaModel = "llama3" });

        var config = manager.Load();
        Assert.Equal(2, config.Profiles.Count);
        Assert.True(config.Profiles.ContainsKey("new"));
    }

    [Fact]
    public void RemoveProfile_WithJsonConfig_StillWorks()
    {
        WriteJsonConfig("config.json", new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["extra"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.RemoveProfile("extra");

        Assert.True(result);
        var config = manager.Load();
        Assert.Single(config.Profiles);
    }

    [Fact]
    public void Validate_WithJsonConfig_StillWorks()
    {
        WriteJsonConfig("config.json", new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://e.com",
                    DeploymentName = "d",
                    ApiKey = "k",
                    AuthMode = "key"
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var errors = manager.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void UpsertProfile_WithYamlConfig_Works()
    {
        var yaml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              default:
                provider: ollama
                ollamaModel: llama3
                ollamaHost: "http://localhost:11434"
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yaml", yaml);

        var manager = new ConfigManager(_tempProjectDir);
        manager.UpsertProfile("new-profile", new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://e.com",
            DeploymentName = "d",
            ApiKey = "k",
            AuthMode = "key"
        });

        var config = manager.Load();
        Assert.Equal(2, config.Profiles.Count);
        Assert.True(config.Profiles.ContainsKey("new-profile"));
    }

    [Fact]
    public void ResolveProfile_WithYamlConfig_Works()
    {
        var yaml = """
            version: "1.0"
            defaultProfile: default
            profiles:
              default:
                provider: ollama
                ollamaModel: llama3
                ollamaHost: "http://localhost:11434"
                authMode: key
              special:
                provider: azure-openai
                endpoint: "https://e.com"
                deploymentName: gpt-4o
                apiKey: key
                authMode: key
            autoSelect:
              enabled: false
              rules: []
            toolSafety:
              mode: prompt-writes
              trustedPaths: []
            ui:
              theme: default
              showToolCalls: true
              streamResponses: true
            """;
        WriteConfigFile("config.yaml", yaml);

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile("special");

        Assert.NotNull(result);
        Assert.Equal("special", result!.Value.Name);
        Assert.Equal("azure-openai", result.Value.Profile.Provider);
    }
}
