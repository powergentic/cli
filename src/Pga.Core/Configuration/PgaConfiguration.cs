using System.Text.Json.Serialization;

namespace Pga.Core.Configuration;

/// <summary>
/// Root configuration stored at ~/.powergentic/config.json
/// </summary>
public sealed class PgaConfiguration
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("defaultProfile")]
    public string DefaultProfile { get; set; } = "default";

    [JsonPropertyName("profiles")]
    public Dictionary<string, LlmProfile> Profiles { get; set; } = new();

    [JsonPropertyName("autoSelect")]
    public AutoSelectConfig AutoSelect { get; set; } = new();

    [JsonPropertyName("toolSafety")]
    public ToolSafetyConfig ToolSafety { get; set; } = new();

    [JsonPropertyName("ui")]
    public UiConfig Ui { get; set; } = new();
}

/// <summary>
/// Configuration for automatic LLM profile selection.
/// </summary>
public sealed class AutoSelectConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("rules")]
    public List<AutoSelectRule> Rules { get; set; } = new();
}

/// <summary>
/// A rule for auto-selecting an LLM profile based on context.
/// </summary>
public sealed class AutoSelectRule
{
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "*";

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "default";

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Tool safety/approval configuration.
/// </summary>
public sealed class ToolSafetyConfig
{
    /// <summary>
    /// auto-approve | prompt-always | prompt-writes
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "prompt-writes";

    /// <summary>
    /// Trusted directories where write operations are auto-approved.
    /// </summary>
    [JsonPropertyName("trustedPaths")]
    public List<string> TrustedPaths { get; set; } = new();
}

/// <summary>
/// UI/rendering preferences.
/// </summary>
public sealed class UiConfig
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "default";

    [JsonPropertyName("showToolCalls")]
    public bool ShowToolCalls { get; set; } = true;

    [JsonPropertyName("streamResponses")]
    public bool StreamResponses { get; set; } = true;
}
