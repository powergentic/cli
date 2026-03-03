using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pga.Core.Configuration;

/// <summary>
/// Manages reading/writing the PGA configuration at ~/.powergentic/config.json
/// </summary>
public sealed class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".powergentic");

    public static string ConfigFilePath =>
        Path.Combine(ConfigDirectory, "config.json");

    /// <summary>
    /// Loads the configuration from disk, or returns a default config if none exists.
    /// </summary>
    public PgaConfiguration Load()
    {
        if (!File.Exists(ConfigFilePath))
            return CreateDefaultConfig();

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize<PgaConfiguration>(json, JsonOptions)
               ?? CreateDefaultConfig();
    }

    /// <summary>
    /// Saves the configuration to disk.
    /// </summary>
    public void Save(PgaConfiguration config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// Initializes the configuration file with defaults if it doesn't exist.
    /// Returns true if a new config was created.
    /// </summary>
    public bool Initialize()
    {
        if (File.Exists(ConfigFilePath))
            return false;

        var config = CreateDefaultConfig();
        Save(config);
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
