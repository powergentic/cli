using System.Text.Json;
using Pga.Core.Configuration;

namespace Pga.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="JsonConfigProvider"/>.
/// </summary>
public class JsonConfigProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonConfigProvider _provider;

    public JsonConfigProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pga_json_provider_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _provider = new JsonConfigProvider();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string WriteTempFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    // --- SupportedExtensions ---

    [Fact]
    public void SupportedExtensions_ContainsJson()
    {
        Assert.Contains(".json", _provider.SupportedExtensions);
    }

    [Fact]
    public void SupportedExtensions_DoesNotContainYaml()
    {
        Assert.DoesNotContain(".yaml", _provider.SupportedExtensions);
        Assert.DoesNotContain(".yml", _provider.SupportedExtensions);
    }

    // --- CanHandle ---

    [Theory]
    [InlineData("config.json", true)]
    [InlineData("config.JSON", true)]
    [InlineData("config.Json", true)]
    [InlineData("config.yaml", false)]
    [InlineData("config.yml", false)]
    [InlineData("config.xml", false)]
    [InlineData("config.toml", false)]
    public void CanHandle_ReturnsExpectedResult(string fileName, bool expected)
    {
        var path = Path.Combine(_tempDir, fileName);
        Assert.Equal(expected, _provider.CanHandle(path));
    }

    [Fact]
    public void CanHandle_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.CanHandle(null!));
    }

    // --- Load ---

    [Fact]
    public void Load_ValidJsonFile_ReturnsConfiguration()
    {
        var json = """
        {
            "version": "2.0",
            "defaultProfile": "test-profile",
            "profiles": {
                "test-profile": {
                    "provider": "ollama",
                    "ollamaModel": "llama3",
                    "ollamaHost": "http://localhost:11434",
                    "authMode": "key"
                }
            },
            "autoSelect": { "enabled": false, "rules": [] },
            "toolSafety": { "mode": "prompt-writes", "trustedPaths": [] },
            "ui": { "theme": "default", "showToolCalls": true, "streamResponses": true }
        }
        """;
        var path = WriteTempFile("config.json", json);

        var config = _provider.Load(path);

        Assert.NotNull(config);
        Assert.Equal("2.0", config.Version);
        Assert.Equal("test-profile", config.DefaultProfile);
        Assert.Single(config.Profiles);
        Assert.Equal("ollama", config.Profiles["test-profile"].Provider);
        Assert.Equal("llama3", config.Profiles["test-profile"].OllamaModel);
    }

    [Fact]
    public void Load_MinimalJsonFile_ReturnsConfigWithDefaults()
    {
        var json = """{ "version": "1.0", "defaultProfile": "default", "profiles": {} }""";
        var path = WriteTempFile("config.json", json);

        var config = _provider.Load(path);

        Assert.NotNull(config);
        Assert.Equal("1.0", config.Version);
        Assert.Empty(config.Profiles);
    }

    [Fact]
    public void Load_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "nonexistent.json");
        Assert.Throws<FileNotFoundException>(() => _provider.Load(path));
    }

    [Fact]
    public void Load_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Load(null!));
    }

    [Fact]
    public void Load_EmptyJsonObject_ReturnsDefaultConfig()
    {
        var path = WriteTempFile("config.json", "{}");
        var config = _provider.Load(path);

        Assert.NotNull(config);
        // Default values from PgaConfiguration
        Assert.Equal("1.0", config.Version);
        Assert.Equal("default", config.DefaultProfile);
    }

    // --- Save ---

    [Fact]
    public void Save_WritesValidJsonFile()
    {
        var path = Path.Combine(_tempDir, "output.json");
        var config = new PgaConfiguration
        {
            Version = "1.0",
            DefaultProfile = "saved",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["saved"] = new LlmProfile
                {
                    Provider = "ollama",
                    OllamaModel = "llama3"
                }
            }
        };

        _provider.Save(path, config);

        Assert.True(File.Exists(path));
        var content = File.ReadAllText(path);
        Assert.Contains("saved", content);
        Assert.Contains("ollama", content);
        Assert.Contains("llama3", content);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNotExists()
    {
        var subDir = Path.Combine(_tempDir, "sub", "dir");
        var path = Path.Combine(subDir, "config.json");
        var config = new PgaConfiguration { Version = "1.0" };

        _provider.Save(path, config);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_NullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Save(null!, new PgaConfiguration()));
    }

    [Fact]
    public void Save_NullConfig_ThrowsArgumentNullException()
    {
        var path = Path.Combine(_tempDir, "config.json");
        Assert.Throws<ArgumentNullException>(() => _provider.Save(path, null!));
    }

    // --- Save/Load Round Trip ---

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        var path = Path.Combine(_tempDir, "roundtrip.json");
        var original = new PgaConfiguration
        {
            Version = "2.0",
            DefaultProfile = "myprofile",
            Profiles = new Dictionary<string, LlmProfile>
            {
                ["myprofile"] = new LlmProfile
                {
                    Provider = "azure-openai",
                    Endpoint = "https://test.openai.azure.com",
                    DeploymentName = "gpt-4o",
                    ApiKey = "secret-key",
                    AuthMode = "key",
                    DisplayName = "My Azure Profile",
                    MaxTokens = 4096,
                    Temperature = 0.7f,
                    TopP = 0.95f
                }
            },
            AutoSelect = new AutoSelectConfig
            {
                Enabled = true,
                Rules = new List<AutoSelectRule>
                {
                    new() { Pattern = "*.py", Profile = "myprofile", Description = "Python" }
                }
            },
            ToolSafety = new ToolSafetyConfig
            {
                Mode = "auto-approve",
                TrustedPaths = new List<string> { "/path/a" }
            },
            Ui = new UiConfig
            {
                Theme = "dark",
                ShowToolCalls = false,
                StreamResponses = false
            }
        };

        _provider.Save(path, original);
        var loaded = _provider.Load(path);

        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.DefaultProfile, loaded.DefaultProfile);
        Assert.Single(loaded.Profiles);
        Assert.Equal("azure-openai", loaded.Profiles["myprofile"].Provider);
        Assert.Equal("https://test.openai.azure.com", loaded.Profiles["myprofile"].Endpoint);
        Assert.Equal("gpt-4o", loaded.Profiles["myprofile"].DeploymentName);
        Assert.Equal("secret-key", loaded.Profiles["myprofile"].ApiKey);
        Assert.Equal("My Azure Profile", loaded.Profiles["myprofile"].DisplayName);
        Assert.Equal(4096, loaded.Profiles["myprofile"].MaxTokens);
        Assert.Equal(0.7f, loaded.Profiles["myprofile"].Temperature);
        Assert.Equal(0.95f, loaded.Profiles["myprofile"].TopP);
        Assert.True(loaded.AutoSelect.Enabled);
        Assert.Single(loaded.AutoSelect.Rules);
        Assert.Equal("auto-approve", loaded.ToolSafety.Mode);
        Assert.Equal("dark", loaded.Ui.Theme);
        Assert.False(loaded.Ui.ShowToolCalls);
        Assert.False(loaded.Ui.StreamResponses);
    }

    // --- Merge ---

    [Fact]
    public void Merge_DelegatesToConfigMerger()
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
                ["base"] = new LlmProfile { Provider = "ollama", OllamaModel = "codestral" }
            }
        };

        var merged = _provider.Merge(baseConfig, overrideConfig);

        Assert.NotNull(merged);
        Assert.Equal("base", merged.DefaultProfile);
        Assert.Equal("codestral", merged.Profiles["base"].OllamaModel);
    }
}
