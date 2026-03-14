using Pga.Core.Configuration;
using Pga.Core.Providers;

namespace Pga.Tests.Providers;

public class LlmProviderFactoryTests
{
    [Fact]
    public void CreateChatClient_UnsupportedProvider_ThrowsNotSupported()
    {
        var profile = new LlmProfile
        {
            Provider = "unsupported-provider"
        };

        Assert.Throws<NotSupportedException>(() => LlmProviderFactory.CreateChatClient(profile));
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_MissingEndpoint_ThrowsInvalidOperation()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            DeploymentName = "gpt-4o",
            ApiKey = "test-key",
            AuthMode = "key"
        };

        Assert.Throws<InvalidOperationException>(() => LlmProviderFactory.CreateChatClient(profile));
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_MissingDeployment_ThrowsInvalidOperation()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://test.openai.azure.com",
            ApiKey = "test-key",
            AuthMode = "key"
        };

        Assert.Throws<InvalidOperationException>(() => LlmProviderFactory.CreateChatClient(profile));
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_MissingApiKey_ThrowsInvalidOperation()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            AuthMode = "key"
        };

        Assert.Throws<InvalidOperationException>(() => LlmProviderFactory.CreateChatClient(profile));
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_ValidKeyAuth_ReturnsChatClient()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            ApiKey = "test-api-key",
            AuthMode = "key"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_AzureAiFoundry_ValidConfig_ReturnsChatClient()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-ai-foundry",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            ApiKey = "test-api-key",
            AuthMode = "key"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_EntraAuth_ReturnsChatClient()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            AuthMode = "entra",
            TenantId = "test-tenant-id"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_AzureOpenAi_EntraAuth_NoTenant_ReturnsChatClient()
    {
        var profile = new LlmProfile
        {
            Provider = "azure-openai",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            AuthMode = "entra"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_Ollama_MissingModel_ThrowsInvalidOperation()
    {
        var profile = new LlmProfile
        {
            Provider = "ollama"
        };

        Assert.Throws<InvalidOperationException>(() => LlmProviderFactory.CreateChatClient(profile));
    }

    [Fact]
    public void CreateChatClient_Ollama_ValidConfig_ReturnsChatClient()
    {
        var profile = new LlmProfile
        {
            Provider = "ollama",
            OllamaHost = "http://localhost:11434",
            OllamaModel = "llama3"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }

    [Fact]
    public void CreateChatClient_ProviderName_CaseInsensitive()
    {
        var profile = new LlmProfile
        {
            Provider = "Azure-OpenAI",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "gpt-4o",
            ApiKey = "test-key",
            AuthMode = "key"
        };

        var client = LlmProviderFactory.CreateChatClient(profile);

        Assert.NotNull(client);
    }
}
