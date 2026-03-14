namespace Pga.Core.Configuration;

/// <summary>
/// Merges override configuration values into a base configuration.
/// Non-default values in the override replace the corresponding values in the base.
/// Profiles are merged by key — override profiles replace base profiles with the same name,
/// and new profiles are added.
/// </summary>
public static class ConfigMerger
{
    /// <summary>
    /// Merges <paramref name="overrideConfig"/> into <paramref name="baseConfig"/>,
    /// returning a new <see cref="PgaConfiguration"/> with the merged result.
    /// </summary>
    public static PgaConfiguration Merge(PgaConfiguration baseConfig, PgaConfiguration overrideConfig)
    {
        ArgumentNullException.ThrowIfNull(baseConfig);
        ArgumentNullException.ThrowIfNull(overrideConfig);

        var merged = new PgaConfiguration
        {
            Version = !string.IsNullOrEmpty(overrideConfig.Version) && overrideConfig.Version != "1.0"
                ? overrideConfig.Version
                : baseConfig.Version,

            DefaultProfile = !string.IsNullOrEmpty(overrideConfig.DefaultProfile) && overrideConfig.DefaultProfile != "default"
                ? overrideConfig.DefaultProfile
                : baseConfig.DefaultProfile,

            // Start with base profiles, then overlay overrides
            Profiles = new Dictionary<string, LlmProfile>(baseConfig.Profiles),

            AutoSelect = MergeAutoSelect(baseConfig.AutoSelect, overrideConfig.AutoSelect),
            ToolSafety = MergeToolSafety(baseConfig.ToolSafety, overrideConfig.ToolSafety),
            Ui = MergeUi(baseConfig.Ui, overrideConfig.Ui)
        };

        // Overlay override profiles onto base profiles
        foreach (var (name, profile) in overrideConfig.Profiles)
        {
            if (merged.Profiles.TryGetValue(name, out var existingProfile))
            {
                merged.Profiles[name] = MergeProfile(existingProfile, profile);
            }
            else
            {
                merged.Profiles[name] = profile;
            }
        }

        return merged;
    }

    private static LlmProfile MergeProfile(LlmProfile baseProfile, LlmProfile overrideProfile)
    {
        return new LlmProfile
        {
            Provider = !string.IsNullOrEmpty(overrideProfile.Provider) && overrideProfile.Provider != "azure-openai"
                ? overrideProfile.Provider
                : baseProfile.Provider,
            DisplayName = overrideProfile.DisplayName ?? baseProfile.DisplayName,
            Endpoint = overrideProfile.Endpoint ?? baseProfile.Endpoint,
            ApiKey = overrideProfile.ApiKey ?? baseProfile.ApiKey,
            DeploymentName = overrideProfile.DeploymentName ?? baseProfile.DeploymentName,
            ModelId = overrideProfile.ModelId ?? baseProfile.ModelId,
            ApiVersion = overrideProfile.ApiVersion ?? baseProfile.ApiVersion,
            AuthMode = !string.IsNullOrEmpty(overrideProfile.AuthMode) && overrideProfile.AuthMode != "key"
                ? overrideProfile.AuthMode
                : baseProfile.AuthMode,
            TenantId = overrideProfile.TenantId ?? baseProfile.TenantId,
            OllamaHost = overrideProfile.OllamaHost != "http://localhost:11434"
                ? overrideProfile.OllamaHost
                : baseProfile.OllamaHost,
            OllamaModel = overrideProfile.OllamaModel ?? baseProfile.OllamaModel,
            MaxTokens = overrideProfile.MaxTokens ?? baseProfile.MaxTokens,
            Temperature = overrideProfile.Temperature ?? baseProfile.Temperature,
            TopP = overrideProfile.TopP ?? baseProfile.TopP
        };
    }

    private static AutoSelectConfig MergeAutoSelect(AutoSelectConfig baseAutoSelect, AutoSelectConfig overrideAutoSelect)
    {
        // If override has rules defined, use them entirely (replace, don't merge individual rules)
        if (overrideAutoSelect.Rules.Count > 0)
        {
            return new AutoSelectConfig
            {
                Enabled = overrideAutoSelect.Enabled || baseAutoSelect.Enabled,
                Rules = new List<AutoSelectRule>(overrideAutoSelect.Rules)
            };
        }

        return new AutoSelectConfig
        {
            Enabled = overrideAutoSelect.Enabled || baseAutoSelect.Enabled,
            Rules = new List<AutoSelectRule>(baseAutoSelect.Rules)
        };
    }

    private static ToolSafetyConfig MergeToolSafety(ToolSafetyConfig baseSafety, ToolSafetyConfig overrideSafety)
    {
        var merged = new ToolSafetyConfig
        {
            Mode = overrideSafety.Mode != "prompt-writes"
                ? overrideSafety.Mode
                : baseSafety.Mode,
            TrustedPaths = new List<string>(baseSafety.TrustedPaths)
        };

        // Add any new trusted paths from override
        foreach (var path in overrideSafety.TrustedPaths)
        {
            if (!merged.TrustedPaths.Contains(path))
                merged.TrustedPaths.Add(path);
        }

        return merged;
    }

    private static UiConfig MergeUi(UiConfig baseUi, UiConfig overrideUi)
    {
        return new UiConfig
        {
            Theme = overrideUi.Theme != "default"
                ? overrideUi.Theme
                : baseUi.Theme,
            ShowToolCalls = overrideUi.ShowToolCalls && baseUi.ShowToolCalls,
            StreamResponses = overrideUi.StreamResponses && baseUi.StreamResponses
        };
    }
}
