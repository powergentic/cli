using System.Text.Json;

namespace Pga.Core.Configuration;

/// <summary>
/// Manages reading/writing the PGA configuration.
/// Supports JSON (.json) and YAML (.yaml, .yml) configuration formats.
/// Looks for .powergentic/config.{json,yaml,yml} in the project directory first,
/// then falls back to ~/.powergentic/config.{json,yaml,yml}.
/// After loading the primary config file, a local override file (config.local.{json,yaml,yml})
/// is merged on top if it exists, allowing secrets and local settings to remain out of source control.
/// </summary>
public sealed class ConfigManager
{
    private readonly string? _projectPath;
    private readonly IReadOnlyList<IConfigProvider> _providers;

    /// <summary>
    /// The base config file names to search for, in priority order.
    /// </summary>
    internal static readonly string[] ConfigFileNames = { "config.json", "config.yaml", "config.yml" };

    /// <summary>
    /// The local override file names to search for, in priority order.
    /// </summary>
    internal static readonly string[] LocalOverrideFileNames = { "config.local.json", "config.local.yaml", "config.local.yml" };

    public ConfigManager(string? projectPath = null)
        : this(projectPath, new IConfigProvider[] { new JsonConfigProvider(), new YamlConfigProvider() })
    {
    }

    /// <summary>
    /// Creates a ConfigManager with explicit providers (useful for testing).
    /// </summary>
    public ConfigManager(string? projectPath, IReadOnlyList<IConfigProvider> providers)
    {
        _projectPath = projectPath;
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public static string GlobalConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".powergentic");

    public static string GlobalConfigFilePath =>
        Path.Combine(GlobalConfigDirectory, "config.json");

    /// <summary>
    /// Returns the local (project-level) config directory path, if a project path was provided.
    /// </summary>
    public string? LocalConfigDirectory =>
        _projectPath != null
            ? Path.Combine(Path.GetFullPath(_projectPath), ".powergentic")
            : null;

    /// <summary>
    /// Returns the local (project-level) config file path, if a project path was provided.
    /// Searches for config.json, config.yaml, and config.yml in order.
    /// </summary>
    public string? LocalConfigFilePath
    {
        get
        {
            if (LocalConfigDirectory == null)
                return null;

            foreach (var fileName in ConfigFileNames)
            {
                var path = Path.Combine(LocalConfigDirectory, fileName);
                if (File.Exists(path))
                    return path;
            }

            // Default to config.json if none exist (for new project initialization)
            return Path.Combine(LocalConfigDirectory, "config.json");
        }
    }

    /// <summary>
    /// Resolves which config file path to use: local first, then global.
    /// Searches for config.json, config.yaml, and config.yml in each location.
    /// </summary>
    public string ConfigFilePath
    {
        get
        {
            // Check local project directory first
            if (LocalConfigDirectory != null)
            {
                foreach (var fileName in ConfigFileNames)
                {
                    var path = Path.Combine(LocalConfigDirectory, fileName);
                    if (File.Exists(path))
                        return path;
                }
            }

            // Check global directory
            foreach (var fileName in ConfigFileNames)
            {
                var path = Path.Combine(GlobalConfigDirectory, fileName);
                if (File.Exists(path))
                    return path;
            }

            // Default to global config.json if nothing exists
            return GlobalConfigFilePath;
        }
    }

    /// <summary>
    /// Returns the directory containing the resolved config file.
    /// </summary>
    public string ConfigDirectory => Path.GetDirectoryName(ConfigFilePath)!;

    /// <summary>
    /// Finds the local override file path (config.local.{json,yaml,yml}) in the same
    /// directory as the resolved config file, if one exists.
    /// </summary>
    public string? LocalOverrideFilePath
    {
        get
        {
            var dir = ConfigDirectory;
            foreach (var fileName in LocalOverrideFileNames)
            {
                var path = Path.Combine(dir, fileName);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }
    }

    /// <summary>
    /// Gets the appropriate <see cref="IConfigProvider"/> for the given file path based on its extension.
    /// </summary>
    public IConfigProvider GetProviderForFile(string filePath)
    {
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(filePath))
                return provider;
        }

        throw new NotSupportedException(
            $"No configuration provider found for file: {filePath}. " +
            $"Supported formats: {string.Join(", ", _providers.SelectMany(p => p.SupportedExtensions))}");
    }

    /// <summary>
    /// Loads the configuration from disk, or returns a default config if none exists.
    /// Checks the project-level .powergentic/ first, then ~/.powergentic/.
    /// Supports config.json, config.yaml, and config.yml formats.
    /// After loading the primary config, merges any config.local.{json,yaml,yml} override file found
    /// in the same directory.
    /// </summary>
    public PgaConfiguration Load()
    {
        var configPath = ConfigFilePath;

        PgaConfiguration config;
        if (!File.Exists(configPath))
        {
            config = CreateDefaultConfig();
        }
        else
        {
            var provider = GetProviderForFile(configPath);
            config = provider.Load(configPath);
        }

        // Apply local override if present
        var overridePath = LocalOverrideFilePath;
        if (overridePath != null && File.Exists(overridePath))
        {
            var overrideProvider = GetProviderForFile(overridePath);
            var overrideConfig = overrideProvider.Load(overridePath);
            config = ConfigMerger.Merge(config, overrideConfig);
        }

        return config;
    }

    /// <summary>
    /// Saves the configuration to disk using the appropriate provider for the current config file format.
    /// Writes to the project-level config if it exists, otherwise to the global config.
    /// </summary>
    public void Save(PgaConfiguration config)
    {
        var configPath = ConfigFilePath;
        Directory.CreateDirectory(ConfigDirectory);
        var provider = GetProviderForFile(configPath);
        provider.Save(configPath, config);
    }

    /// <summary>
    /// Initializes the configuration file with defaults if it doesn't exist.
    /// Creates at the global (~/.powergentic/) location as config.json.
    /// Returns true if a new config was created.
    /// </summary>
    public bool Initialize()
    {
        // Check if any config file already exists in the global directory
        foreach (var fileName in ConfigFileNames)
        {
            if (File.Exists(Path.Combine(GlobalConfigDirectory, fileName)))
                return false;
        }

        // Always initialize at the global location as JSON
        Directory.CreateDirectory(GlobalConfigDirectory);
        var config = CreateDefaultConfig();
        var provider = GetProviderForFile(GlobalConfigFilePath);
        provider.Save(GlobalConfigFilePath, config);
        return true;
    }

    /// <summary>
    /// Adds or updates an LLM profile.
    /// </summary>
    public void UpsertProfile(string name, LlmProfile profile)
    {
        var config = Load();
        config.Profiles[name] = profile;
        Save(config);
    }

    /// <summary>
    /// Removes an LLM profile.
    /// </summary>
    public bool RemoveProfile(string name)
    {
        var config = Load();
        if (!config.Profiles.Remove(name))
            return false;

        if (config.DefaultProfile == name)
            config.DefaultProfile = config.Profiles.Keys.FirstOrDefault() ?? "default";

        Save(config);
        return true;
    }

    /// <summary>
    /// Gets a specific LLM profile by name, falling back to the default.
    /// </summary>
    public LlmProfile? GetProfile(string? name = null)
    {
        var config = Load();
        var profileName = name ?? config.DefaultProfile;
        return config.Profiles.TryGetValue(profileName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Resolves which profile to use given an optional agent-specified profile
    /// and optional command-line override.
    /// </summary>
    public (string Name, LlmProfile Profile)? ResolveProfile(
        string? commandLineProfile = null,
        string? agentProfile = null)
    {
        var config = Load();
        var profileName = commandLineProfile ?? agentProfile ?? config.DefaultProfile;

        // Try auto-select rules if enabled and no explicit profile specified
        if (commandLineProfile == null && agentProfile == null && config.AutoSelect.Enabled)
        {
            foreach (var rule in config.AutoSelect.Rules)
            {
                if (config.Profiles.ContainsKey(rule.Profile))
                {
                    profileName = rule.Profile;
                    break;
                }
            }
        }

        if (config.Profiles.TryGetValue(profileName, out var profile))
            return (profileName, profile);

        return null;
    }

    /// <summary>
    /// Validates the entire configuration and returns any errors.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        var config = Load();

        if (config.Profiles.Count == 0)
        {
            errors.Add("No LLM profiles configured. Run 'pga config add-profile' to add one.");
            return errors;
        }

        if (!config.Profiles.ContainsKey(config.DefaultProfile))
            errors.Add($"Default profile '{config.DefaultProfile}' does not exist.");

        foreach (var (name, profile) in config.Profiles)
        {
            var profileErrors = profile.Validate();
            foreach (var error in profileErrors)
                errors.Add($"Profile '{name}': {error}");
        }

        return errors;
    }

    private static PgaConfiguration CreateDefaultConfig() => new()
    {
        Version = "1.0",
        DefaultProfile = "default",
        Profiles = new Dictionary<string, LlmProfile>
        {
            ["default"] = new()
            {
                Provider = "azure-openai",
                DisplayName = "Default Azure OpenAI",
                Endpoint = "",
                DeploymentName = "",
                ApiKey = "",
                AuthMode = "key"
            }
        },
        ToolSafety = new ToolSafetyConfig
        {
            Mode = "prompt-writes"
        },
        Ui = new UiConfig
        {
            ShowToolCalls = true,
            StreamResponses = true
        }
    };
}
