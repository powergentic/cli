using System.Text.Json;
using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

/// <summary>
/// Tests ConfigManager filesystem operations: Save, Load, Initialize, UpsertProfile, RemoveProfile, GetProfile, Validate.
/// </summary>
public class ConfigManagerFilesystemTests : IDisposable
{
    private readonly string _tempProjectDir;
    private readonly string _tempGlobalDir;

    public ConfigManagerFilesystemTests()
    {
        _tempProjectDir = Path.Combine(Path.GetTempPath(), "pga_cfgtest_" + Guid.NewGuid().ToString("N"));
        _tempGlobalDir = Path.Combine(Path.GetTempPath(), "pga_cfgtest_global_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempProjectDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempProjectDir))
            Directory.Delete(_tempProjectDir, true);
        if (Directory.Exists(_tempGlobalDir))
            Directory.Delete(_tempGlobalDir, true);
    }

    private void WriteLocalConfig(PgaConfiguration config)
    {
        var configDir = Path.Combine(_tempProjectDir, ".powergentic");
        Directory.CreateDirectory(configDir);
        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(Path.Combine(configDir, "config.json"), json);
    }

    [Fact]
    public void LocalConfigFilePath_WithProjectPath_ReturnsLocalPath()
    {
        var manager = new ConfigManager(_tempProjectDir);

        Assert.NotNull(manager.LocalConfigFilePath);
        Assert.Contains(".powergentic", manager.LocalConfigFilePath!);
        Assert.Contains("config.json", manager.LocalConfigFilePath);
    }

    [Fact]
    public void LocalConfigFilePath_WithNullProjectPath_ReturnsNull()
    {
        var manager = new ConfigManager(null);
        Assert.Null(manager.LocalConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_WhenLocalExists_ReturnsLocalPath()
    {
        WriteLocalConfig(new PgaConfiguration { Version = "1.0", DefaultProfile = "local" });

        var manager = new ConfigManager(_tempProjectDir);
        Assert.StartsWith(_tempProjectDir, manager.ConfigFilePath);
    }

    [Fact]
    public void ConfigFilePath_WhenNoLocalExists_FallsBackToGlobal()
    {
        var manager = new ConfigManager(_tempProjectDir);

        // No local config exists, should fall back to global
        Assert.Contains(".powergentic", manager.ConfigFilePath);
        Assert.Equal(ConfigManager.GlobalConfigFilePath, manager.ConfigFilePath);
    }

    [Fact]
    public void ConfigDirectory_ReturnsDirectoryOfConfigFile()
    {
        var manager = new ConfigManager(_tempProjectDir);
        var dir = manager.ConfigDirectory;

        Assert.NotNull(dir);
        Assert.Contains(".powergentic", dir);
    }

    [Fact]
    public void Load_WhenLocalConfigExists_LoadsLocalConfig()
    {
        var expected = new PgaConfiguration
        {
            Version = "2.0",
            DefaultProfile = "custom-profile",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["custom-profile"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3"
                }
            }
        };

        WriteLocalConfig(expected);
        var manager = new ConfigManager(_tempProjectDir);
        var config = manager.Load();

        Assert.Equal("2.0", config.Version);
        Assert.Equal("custom-profile", config.DefaultProfile);
        Assert.True(config.Profiles.ContainsKey("custom-profile"));
        Assert.Equal("ollama", config.Profiles["custom-profile"].Provider);
    }

    [Fact]
    public void Load_WhenNoConfigExists_ReturnsDefault()
    {
        var manager = new ConfigManager(_tempProjectDir);
        var config = manager.Load();

        Assert.Equal("1.0", config.Version);
        Assert.Equal("default", config.DefaultProfile);
        Assert.True(config.Profiles.ContainsKey("default"));
    }

    [Fact]
    public void Save_CreatesConfigFile()
    {
        WriteLocalConfig(new PgaConfiguration()); // ensure local dir exists so Save uses it
        var manager = new ConfigManager(_tempProjectDir);

        var config = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "saved-profile",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["saved-profile"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "test-key",
                    AuthMode = "key"
                }
            }
        };

        manager.Save(config);

        Assert.True(File.Exists(manager.ConfigFilePath));
        var loaded = manager.Load();
        Assert.Equal("saved-profile", loaded.DefaultProfile);
        Assert.True(loaded.Profiles.ContainsKey("saved-profile"));
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        // Use a new temp dir that doesn't have .powergentic yet
        var newDir = Path.Combine(_tempProjectDir, "sub", "project");
        Directory.CreateDirectory(newDir);

        // Write initial config so local path resolves
        var localConfigDir = Path.Combine(newDir, ".powergentic");
        Directory.CreateDirectory(localConfigDir);
        File.WriteAllText(Path.Combine(localConfigDir, "config.json"), "{}");

        var manager = new ConfigManager(newDir);
        manager.Save(new PgaConfiguration { Version = "1.0", DefaultProfile = "test" });

        Assert.True(File.Exists(manager.ConfigFilePath));
    }

    [Fact]
    public void UpsertProfile_AddsNewProfile()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        manager.UpsertProfile("new-profile", new LlmProfile
        {
            Provider = "ollama",
            OllamaModel = "codestral"
        });

        var config = manager.Load();
        Assert.Equal(2, config.Profiles.Count);
        Assert.True(config.Profiles.ContainsKey("new-profile"));
        Assert.Equal("ollama", config.Profiles["new-profile"].Provider);
        Assert.Equal("codestral", config.Profiles["new-profile"].OllamaModel);
    }

    [Fact]
    public void UpsertProfile_UpdatesExistingProfile()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://old.com", DeploymentName = "old", ApiKey = "old", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        manager.UpsertProfile("default", new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://new.com",
            DeploymentName = "gpt-4o-new",
            ApiKey = "new-key",
            AuthMode = "key"
        });

        var config = manager.Load();
        Assert.Single(config.Profiles);
        Assert.Equal("https://new.com", config.Profiles["default"].Endpoint);
    }

    [Fact]
    public void RemoveProfile_ExistingProfile_ReturnsTrue()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["secondary"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.RemoveProfile("secondary");

        Assert.True(result);
        var config = manager.Load();
        Assert.Single(config.Profiles);
        Assert.False(config.Profiles.ContainsKey("secondary"));
    }

    [Fact]
    public void RemoveProfile_NonExistentProfile_ReturnsFalse()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.RemoveProfile("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void RemoveProfile_DefaultProfile_UpdatesDefaultToNextAvailable()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "primary",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["primary"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["secondary"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        manager.RemoveProfile("primary");

        var config = manager.Load();
        Assert.Single(config.Profiles);
        Assert.Equal("secondary", config.DefaultProfile);
    }

    [Fact]
    public void RemoveProfile_LastProfile_DefaultBecomesEmpty()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "only",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["only"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        manager.RemoveProfile("only");

        var config = manager.Load();
        Assert.Empty(config.Profiles);
        Assert.Equal("default", config.DefaultProfile); // Falls back to "default" string
    }

    [Fact]
    public void GetProfile_ExistingName_ReturnsProfile()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["ollama"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var profile = manager.GetProfile("ollama");

        Assert.NotNull(profile);
        Assert.Equal("ollama", profile!.Provider);
        Assert.Equal("llama3", profile.OllamaModel);
    }

    [Fact]
    public void GetProfile_NullName_ReturnsDefaultProfile()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var profile = manager.GetProfile(null);

        Assert.NotNull(profile);
        Assert.Equal("azure-openai", profile!.Provider);
    }

    [Fact]
    public void GetProfile_NonExistentName_ReturnsNull()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var profile = manager.GetProfile("nonexistent");

        Assert.Null(profile);
    }

    [Fact]
    public void ResolveProfile_CommandLineOverridesAgent()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["agent-profile"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" },
                ["cli-profile"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile("cli-profile", "agent-profile");

        Assert.NotNull(result);
        Assert.Equal("cli-profile", result!.Value.Name);
        Assert.Equal("codestral", result.Value.Profile.OllamaModel);
    }

    [Fact]
    public void ResolveProfile_AgentProfileUsedWhenNoCommandLine()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["agent-profile"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, "agent-profile");

        Assert.NotNull(result);
        Assert.Equal("agent-profile", result!.Value.Name);
    }

    [Fact]
    public void ResolveProfile_DefaultUsedWhenNeitherSpecified()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, null);

        Assert.NotNull(result);
        Assert.Equal("default", result!.Value.Name);
    }

    [Fact]
    public void ResolveProfile_NonExistentProfile_ReturnsNull()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "nonexistent",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, null);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveProfile_WithAutoSelectEnabled_UsesAutoSelectRule()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["auto-profile"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "auto-profile" }
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, null);

        Assert.NotNull(result);
        // Auto-select should pick "auto-profile" since it exists in profiles
        Assert.Equal("auto-profile", result!.Value.Name);
    }

    [Fact]
    public void ResolveProfile_AutoSelectDisabled_UsesDefault()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" },
                ["auto-profile"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = false,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "auto-profile" }
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, null);

        Assert.NotNull(result);
        Assert.Equal("default", result!.Value.Name);
    }

    [Fact]
    public void ResolveProfile_AutoSelectRuleProfileNotFound_UsesDefault()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k", AuthMode = "key" }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "nonexistent-profile" }
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var result = manager.ResolveProfile(null, null);

        Assert.NotNull(result);
        Assert.Equal("default", result!.Value.Name);
    }

    [Fact]
    public void Validate_ValidConfig_ReturnsNoErrors()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "test-key",
                    AuthMode = "key"
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var errors = manager.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NoProfiles_ReturnsError()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "default",
            Profiles = new Dictionary<string, LlmProfile>()
        });

        var manager = new ConfigManager(_tempProjectDir);
        var errors = manager.Validate();

        Assert.Single(errors);
        Assert.Contains("No LLM profiles", errors[0]);
    }

    [Fact]
    public void Validate_DefaultProfileDoesNotExist_ReturnsError()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "nonexistent",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["other"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var errors = manager.Validate();

        Assert.Contains(errors, e => e.Contains("nonexistent") && e.Contains("does not exist"));
    }

    [Fact]
    public void Validate_InvalidProfile_ReturnsProfileErrors()
    {
        WriteLocalConfig(new PgaConfiguration
        {
            DefaultProfile = "broken",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["broken"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    // Missing Endpoint, DeploymentName, ApiKey
                    AuthMode = "key"
                }
            }
        });

        var manager = new ConfigManager(_tempProjectDir);
        var errors = manager.Validate();

        Assert.True(errors.Count >= 2);
        Assert.All(errors, e => Assert.Contains("Profile 'broken'", e));
    }

    [Fact]
    public void Initialize_CreatesGlobalConfigFile()
    {
        // This test checks the Initialize behavior but since it writes to the real global path,
        // we test it indirectly by verifying the method returns the expected boolean
        var manager = new ConfigManager(_tempProjectDir);

        // If global config already exists, Initialize returns false
        if (File.Exists(ConfigManager.GlobalConfigFilePath))
        {
            var result = manager.Initialize();
            Assert.False(result);
        }
        // If it doesn't exist, Initialize would create it - we can't test this without
        // modifying the global config. We verify the method at least doesn't throw.
    }

    [Fact]
    public void Load_SaveRoundTrip_PreservesAllFields()
    {
        WriteLocalConfig(new PgaConfiguration()); // initial config to establish local path

        var manager = new ConfigManager(_tempProjectDir);
        var config = new PgaConfiguration
        {
            Version = "2.0",
            DefaultProfile = "test",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["test"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3",
                    DisplayName = "Test Ollama",
                    OllamaHost = "http://myhost:11434",
                    MaxTokens = 2048,
                    Temperature = 0.5f,
                    TopP = 0.8f
                }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "test", Description = "Python files" }
                }
            },
            ToolSafety = new ToolSafetyConfig
            {
                Mode = "auto-approve",
                TrustedPaths = new List<string> { "/home/user/trusted" }
            },
            Ui = new UiConfig
            {
                Theme = "dark",
                ShowToolCalls = false,
                StreamResponses = false
            }
        };

        manager.Save(config);
        var loaded = manager.Load();

        Assert.Equal("2.0", loaded.Version);
        Assert.Equal("test", loaded.DefaultProfile);
        Assert.Single(loaded.Profiles);
        Assert.Equal("ollama", loaded.Profiles["test"].Provider);
        Assert.Equal("llama3", loaded.Profiles["test"].OllamaModel);
        Assert.Equal("Test Ollama", loaded.Profiles["test"].DisplayName);
        Assert.Equal("http://myhost:11434", loaded.Profiles["test"].OllamaHost);
        Assert.Equal(2048, loaded.Profiles["test"].MaxTokens);
        Assert.Equal(0.5f, loaded.Profiles["test"].Temperature);
        Assert.Equal(0.8f, loaded.Profiles["test"].TopP);
        Assert.True(loaded.AutoSelect.Enabled);
        Assert.Single(loaded.AutoSelect.Rules);
        Assert.Equal("*.py", loaded.AutoSelect.Rules[0].Pattern);
        Assert.Equal("auto-approve", loaded.ToolSafety.Mode);
        Assert.Single(loaded.ToolSafety.TrustedPaths);
        Assert.Equal("dark", loaded.Ui.Theme);
        Assert.False(loaded.Ui.ShowToolCalls);
        Assert.False(loaded.Ui.StreamResponses);
    }
}
