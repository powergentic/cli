using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="ConfigMerger"/>.
/// </summary>
public class ConfigMergerTests
{
    // --- Null argument checks ---

    [Fact]
    public void Merge_NullBaseConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigMerger.Merge(null!, new PgaConfiguration()));
    }

    [Fact]
    public void Merge_NullOverrideConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigMerger.Merge(new PgaConfiguration(), null!));
    }

    // --- Profile merging ---

    [Fact]
    public void Merge_OverrideAddsNewProfile()
    {
        var baseConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k" }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["local"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal(2, merged.Profiles.Count);
        Assert.True(merged.Profiles.ContainsKey("default"));
        Assert.True(merged.Profiles.ContainsKey("local"));
    }

    [Fact]
    public void Merge_OverrideReplacesExistingProfile_ApiKey()
    {
        var baseConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://e.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "old-key",
                    AuthMode = "key"
                }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    ApiKey = "new-secret-key"
                }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Single(merged.Profiles);
        Assert.Equal("new-secret-key", merged.Profiles["default"].ApiKey);
        // Base values should be preserved where override doesn't specify
        Assert.Equal("https://e.com", merged.Profiles["default"].Endpoint);
        Assert.Equal("gpt-4o", merged.Profiles["default"].DeploymentName);
    }

    [Fact]
    public void Merge_OverrideChangesOllamaModel()
    {
        var baseConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["local"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["local"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("codestral", merged.Profiles["local"].OllamaModel);
    }

    [Fact]
    public void Merge_EmptyOverrideProfiles_PreservesBaseProfiles()
    {
        var baseConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["default"] = new LlmProfile { Provider = "azure-openai", Endpoint = "https://e.com", DeploymentName = "d", ApiKey = "k" },
                ["local"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>()
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal(2, merged.Profiles.Count);
    }

    // --- DefaultProfile merging ---

    [Fact]
    public void Merge_OverrideDefaultProfile_WhenChanged()
    {
        var baseConfig = new PgaConfiguration { DefaultProfile = "default" };
        var overrideConfig = new PgaConfiguration { DefaultProfile = "local" };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("local", merged.DefaultProfile);
    }

    [Fact]
    public void Merge_PreservesBaseDefaultProfile_WhenOverrideIsDefault()
    {
        var baseConfig = new PgaConfiguration { DefaultProfile = "my-custom-profile" };
        var overrideConfig = new PgaConfiguration { DefaultProfile = "default" }; // default value

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("my-custom-profile", merged.DefaultProfile);
    }

    // --- Version merging ---

    [Fact]
    public void Merge_OverrideVersion_WhenChanged()
    {
        var baseConfig = new PgaConfiguration { Version = "1.0" };
        var overrideConfig = new PgaConfiguration { Version = "2.0" };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("2.0", merged.Version);
    }

    [Fact]
    public void Merge_PreservesBaseVersion_WhenOverrideIsDefault()
    {
        var baseConfig = new PgaConfiguration { Version = "2.0" };
        var overrideConfig = new PgaConfiguration { Version = "1.0" }; // default value

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("2.0", merged.Version);
    }

    // --- AutoSelect merging ---

    [Fact]
    public void Merge_OverrideAutoSelectRules_ReplacesBaseRules()
    {
        var baseConfig = new PgaConfiguration
        {
            AutoSelect = new AutoSelectConfig
            {
                Enabled = false,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.cs", Profile = "azure", Description = "C#" }
                }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "codestral", Description = "Python" }
                }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.True(merged.AutoSelect.Enabled);
        Assert.Single(merged.AutoSelect.Rules);
        Assert.Equal("*.py", merged.AutoSelect.Rules[0].Pattern);
    }

    [Fact]
    public void Merge_EmptyOverrideAutoSelectRules_PreservesBaseRules()
    {
        var baseConfig = new PgaConfiguration
        {
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.cs", Profile = "azure" }
                }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            AutoSelect = new AutoSelectConfig
            {
                Enabled = false,
                Rules = new List<AutoSelectRule>()
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.True(merged.AutoSelect.Enabled); // base was true
        Assert.Single(merged.AutoSelect.Rules);
    }

    // --- ToolSafety merging ---

    [Fact]
    public void Merge_OverrideToolSafetyMode()
    {
        var baseConfig = new PgaConfiguration
        {
            ToolSafety = new ToolSafetyConfig { Mode = "prompt-writes" }
        };

        var overrideConfig = new PgaConfiguration
        {
            ToolSafety = new ToolSafetyConfig { Mode = "auto-approve" }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("auto-approve", merged.ToolSafety.Mode);
    }

    [Fact]
    public void Merge_OverrideTrustedPaths_MergesUniquePaths()
    {
        var baseConfig = new PgaConfiguration
        {
            ToolSafety = new ToolSafetyConfig
            {
                TrustedPaths = new List<string> { "/path/a", "/path/b" }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            ToolSafety = new ToolSafetyConfig
            {
                TrustedPaths = new List<string> { "/path/b", "/path/c" }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal(3, merged.ToolSafety.TrustedPaths.Count);
        Assert.Contains("/path/a", merged.ToolSafety.TrustedPaths);
        Assert.Contains("/path/b", merged.ToolSafety.TrustedPaths);
        Assert.Contains("/path/c", merged.ToolSafety.TrustedPaths);
    }

    // --- UI merging ---

    [Fact]
    public void Merge_OverrideUiTheme()
    {
        var baseConfig = new PgaConfiguration
        {
            Ui = new UiConfig { Theme = "default" }
        };

        var overrideConfig = new PgaConfiguration
        {
            Ui = new UiConfig { Theme = "dark" }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("dark", merged.Ui.Theme);
    }

    [Fact]
    public void Merge_PreservesBaseUiTheme_WhenOverrideIsDefault()
    {
        var baseConfig = new PgaConfiguration
        {
            Ui = new UiConfig { Theme = "monokai" }
        };

        var overrideConfig = new PgaConfiguration
        {
            Ui = new UiConfig { Theme = "default" }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        Assert.Equal("monokai", merged.Ui.Theme);
    }

    // --- Full integration-style merge ---

    [Fact]
    public void Merge_FullScenario_LocalOverridesSecretsOnly()
    {
        // Simulate: config.yaml in source control with structure, config.local.yaml with secrets
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
                    // No API key — that comes from local override
                },
                ["local"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3"
                }
            },
            ToolSafety = new ToolSafetyConfig { Mode = "prompt-writes" }
        };

        var localOverride = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["azure"] = new LlmProfile
                {
                    ApiKey = "sk-my-secret-api-key-12345"
                }
            },
            ToolSafety = new ToolSafetyConfig
            {
                TrustedPaths = new List<string> { "/Users/chris/projects" }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, localOverride);

        // Secrets merged in
        Assert.Equal("sk-my-secret-api-key-12345", merged.Profiles["azure"].ApiKey);
        // Structure preserved
        Assert.Equal("https://my-org.openai.azure.com", merged.Profiles["azure"].Endpoint);
        Assert.Equal("gpt-4o", merged.Profiles["azure"].DeploymentName);
        Assert.Equal("azure", merged.DefaultProfile);
        // Local profile untouched
        Assert.Equal("llama3", merged.Profiles["local"].OllamaModel);
        // Trusted paths added
        Assert.Contains("/Users/chris/projects", merged.ToolSafety.TrustedPaths);
    }

    [Fact]
    public void Merge_DoesNotMutateOriginalConfigs()
    {
        var baseConfig = new PgaConfiguration
        {
            DefaultProfile = "base",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["base"] = new LlmProfile { Provider = "ollama", OllamaModel = "llama3" }
            }
        };

        var overrideConfig = new PgaConfiguration
        {
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["new"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            }
        };

        var merged = ConfigMerger.Merge(baseConfig, overrideConfig);

        // Original base config unchanged
        Assert.Single(baseConfig.Profiles);
        Assert.False(baseConfig.Profiles.ContainsKey("new"));

        // Original override config unchanged
        Assert.Single(overrideConfig.Profiles);
        Assert.False(overrideConfig.Profiles.ContainsKey("base"));

        // Merged has both
        Assert.Equal(2, merged.Profiles.Count);
    }
}
