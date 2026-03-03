using System.Text.Json.Serialization;

namespace Pga.Core.Configuration;

/// <summary>
/// An LLM provider profile configured in ~/.powergentic/config.json
/// </summary>
public sealed class LlmProfile
{
    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "azure-openai";

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    // --- Azure OpenAI / AI Foundry ---

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("deploymentName")]
    public string? DeploymentName { get; set; }

    [JsonPropertyName("modelId")]
    public string? ModelId { get; set; }

    [JsonPropertyName("apiVersion")]
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Authentication mode: "key" or "entra" (Azure AD/Entra ID).
    /// </summary>
    [JsonPropertyName("authMode")]
    public string AuthMode { get; set; } = "key";

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    // --- Ollama ---

    [JsonPropertyName("ollamaHost")]
    public string OllamaHost { get; set; } = "http://localhost:11434";

    [JsonPropertyName("ollamaModel")]
    public string? OllamaModel { get; set; }

    // --- Shared ---

    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("topP")]
    public float? TopP { get; set; }

    /// <summary>
    /// Validates that required fields are set for the chosen provider.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        switch (Provider.ToLowerInvariant())
        {
            case "azure-openai":
            case "azure-ai-foundry":
                if (string.IsNullOrWhiteSpace(Endpoint))
                    errors.Add("Endpoint is required for Azure OpenAI.");
                if (string.IsNullOrWhiteSpace(DeploymentName))
                    errors.Add("DeploymentName is required for Azure OpenAI.");
                if (AuthMode == "key" && string.IsNullOrWhiteSpace(ApiKey))
                    errors.Add("ApiKey is required when authMode is 'key'.");
                break;

            case "ollama":
                if (string.IsNullOrWhiteSpace(OllamaModel))
                    errors.Add("OllamaModel is required for Ollama provider.");
                break;

            default:
                errors.Add($"Unknown provider: {Provider}");
                break;
        }

        return errors;
    }
}
