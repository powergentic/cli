using System.Text.Json;

namespace Pga.Core.Configuration;

/// <summary>
/// Manages reading/writing the PGA configuration.
/// Looks for .powergentic/config.json in the project directory first,
/// then falls back to ~/.powergentic/config.json.
/// </summary>
public sealed class ConfigManager
{
    private readonly string? _projectPath;

    public ConfigManager(string? projectPath = null)
    {
        _projectPath = projectPath;
    }

    public static string GlobalConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".powergentic");

    public static string GlobalConfigFilePath =>
        Path.Combine(GlobalConfigDirectory, "config.json");

    /// <summary>
    /// Returns the local (project-level) config file path, if a project path was provided.
    /// </summary>
    public string? LocalConfigFilePath =>
        _projectPath != null
            ? Path.Combine(Path.GetFullPath(_projectPath), ".powergentic", "config.json")
            : null;

    /// <summary>
    /// Resolves which config file path to use: local first, then global.
    /// </summary>
    public string ConfigFilePath =>
        (LocalConfigFilePath != null && File.Exists(LocalConfigFilePath))
            ? LocalConfigFilePath
            : GlobalConfigFilePath;

    /// <summary>
    /// Returns the directory containing the resolved config file.
    /// </summary>
    public string ConfigDirectory => Path.GetDirectoryName(ConfigFilePath)!;

    /// <summary>
    /// Loads the configuration from disk, or returns a default config if none exists.
    /// Checks the project-level .powergentic/config.json first, then ~/.powergentic/config.json.
    /// </summary>
    public PgaConfiguration Load()
    {
        if (!File.Exists(ConfigFilePath))
            return CreateDefaultConfig();

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize(json, PgaJsonContext.Default.PgaConfiguration)
               ?? CreateDefaultConfig();
    }

    /// <summary>
    /// Saves the configuration to disk.
    /// Writes to the project-level config if it exists, otherwise to the global config.
    /// </summary>
    public void Save(PgaConfiguration config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// Initializes the configuration file with defaults if it doesn't exist.
    /// Creates at the global (~/.powergentic/) location.
    /// Returns true if a new config was created.
    /// </summary>
    public bool Initialize()
    {
        if (File.Exists(GlobalConfigFilePath))
            return false;

        // Always initialize at the global location
        Directory.CreateDirectory(GlobalConfigDirectory);
        var config = CreateDefaultConfig();
        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(GlobalConfigFilePath, json);
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
