using Microsoft.Extensions.AI;
using Pga.Core.Configuration;

namespace Pga.Core.Providers;

/// <summary>
/// Factory for creating IChatClient instances from LLM profile configurations.
/// </summary>
public static class LlmProviderFactory
{
    /// <summary>
    /// Creates an IChatClient for the given LLM profile.
    /// </summary>
    public static IChatClient CreateChatClient(LlmProfile profile)
    {
        return profile.Provider.ToLowerInvariant() switch
        {
            "azure-openai" or "azure-ai-foundry" => CreateAzureOpenAiClient(profile),
            "ollama" => CreateOllamaClient(profile),
            _ => throw new NotSupportedException($"LLM provider '{profile.Provider}' is not supported.")
        };
    }

    private static IChatClient CreateAzureOpenAiClient(LlmProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Endpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is required.");
        if (string.IsNullOrWhiteSpace(profile.DeploymentName))
            throw new InvalidOperationException("Azure OpenAI deployment name is required.");

        var endpoint = new Uri(profile.Endpoint);

        if (profile.AuthMode.Equals("entra", StringComparison.OrdinalIgnoreCase))
        {
            // Use Azure AD / Entra ID token-based authentication
            var credential = CreateAzureCredential(profile);
            var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(endpoint, credential);
            return azureClient.GetChatClient(profile.DeploymentName).AsIChatClient();
        }
        else
        {
            // Use API key authentication
            if (string.IsNullOrWhiteSpace(profile.ApiKey))
                throw new InvalidOperationException("API key is required for key-based authentication.");

            var apiKeyCredential = new System.ClientModel.ApiKeyCredential(profile.ApiKey);
            var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(endpoint, apiKeyCredential);
            return azureClient.GetChatClient(profile.DeploymentName).AsIChatClient();
        }
    }

    private static Azure.Core.TokenCredential CreateAzureCredential(LlmProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.TenantId))
        {
            return new Azure.Identity.DefaultAzureCredential(
                new Azure.Identity.DefaultAzureCredentialOptions
                {
                    TenantId = profile.TenantId
                });
        }

        return new Azure.Identity.DefaultAzureCredential();
    }

    private static IChatClient CreateOllamaClient(LlmProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.OllamaModel))
            throw new InvalidOperationException("Ollama model name is required.");

        var uri = new Uri(profile.OllamaHost);
        var ollamaClient = new OllamaSharp.OllamaApiClient(uri, profile.OllamaModel);

        // Wrap with schema sanitizer to fix nullable type arrays that Ollama doesn't support
        return new OllamaToolSchemaSanitizer(ollamaClient);
    }
}
