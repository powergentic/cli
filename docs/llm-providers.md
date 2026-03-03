# LLM Providers

PGA supports three LLM providers. Each provider is configured as a named profile in `~/.powergentic/config.json`.

## Supported Providers

| Provider | Config Value | Authentication | Description |
|---|---|---|---|
| Azure OpenAI | `azure-openai` | API Key or Entra ID | Azure-hosted OpenAI models |
| Azure AI Foundry | `azure-ai-foundry` | API Key or Entra ID | Azure AI Foundry deployments |
| Ollama | `ollama` | None | Locally-hosted open-source models |

---

## Azure OpenAI

Use Azure-hosted OpenAI models (GPT-4o, GPT-4, GPT-3.5, etc.).

### Prerequisites

1. An [Azure subscription](https://azure.microsoft.com/free/)
2. An [Azure OpenAI resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource)
3. A deployed model (e.g., `gpt-4o`)

### Authentication: API Key

The simplest approach — use an API key from the Azure portal.

```json
{
  "profiles": {
    "azure-gpt4": {
      "provider": "azure-openai",
      "displayName": "GPT-4o on Azure",
      "endpoint": "https://my-resource.openai.azure.com",
      "deploymentName": "gpt-4o",
      "authMode": "key",
      "apiKey": "your-api-key-here"
    }
  }
}
```

**Where to find your API key:**
1. Go to the [Azure Portal](https://portal.azure.com)
2. Navigate to your Azure OpenAI resource
3. Go to **Keys and Endpoint**
4. Copy **Key 1** or **Key 2**

### Authentication: Entra ID (Azure AD)

Use Azure Active Directory / Entra ID for token-based authentication. This is more secure and doesn't require managing API keys.

```json
{
  "profiles": {
    "azure-entra": {
      "provider": "azure-openai",
      "displayName": "GPT-4o (Entra ID)",
      "endpoint": "https://my-resource.openai.azure.com",
      "deploymentName": "gpt-4o",
      "authMode": "entra",
      "tenantId": "your-tenant-id"
    }
  }
}
```

**How Entra ID auth works in PGA:**

PGA uses the [Azure.Identity](https://learn.microsoft.com/dotnet/api/azure.identity.defaultazurecredential) library's `DefaultAzureCredential`, which tries these authentication methods in order:

1. **Environment variables** (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`)
2. **Managed Identity** (when running in Azure)
3. **Azure CLI** (`az login`)
4. **Visual Studio** credentials
5. **Azure PowerShell** credentials
6. **Interactive browser** login

For local development, the easiest approach is:

```bash
# Sign in with Azure CLI
az login

# If you have multiple tenants, specify the tenant
az login --tenant your-tenant-id
```

**Required RBAC role:**

Your Azure AD identity needs the **Cognitive Services OpenAI User** role on the Azure OpenAI resource.

### Optional Settings

| Field | Description | Example |
|---|---|---|
| `modelId` | Model identifier (if different from deployment) | `"gpt-4o-2024-05-13"` |
| `apiVersion` | API version override | `"2024-02-01"` |
| `maxTokens` | Maximum response tokens | `4096` |
| `temperature` | Response randomness (0.0–2.0) | `0.7` |
| `topP` | Nucleus sampling | `0.95` |

---

## Azure AI Foundry

Azure AI Foundry uses the same configuration as Azure OpenAI. Set `provider` to `"azure-ai-foundry"`.

```json
{
  "profiles": {
    "foundry": {
      "provider": "azure-ai-foundry",
      "displayName": "GPT-4o via AI Foundry",
      "endpoint": "https://my-foundry.openai.azure.com",
      "deploymentName": "gpt-4o",
      "authMode": "key",
      "apiKey": "your-api-key"
    }
  }
}
```

Both `azure-openai` and `azure-ai-foundry` use the same underlying `AzureOpenAIClient` from the [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI) SDK. The provider name is purely for organizational clarity.

---

## Ollama (Local Models)

[Ollama](https://ollama.com) lets you run open-source LLMs locally on your machine.

### Prerequisites

1. Install Ollama:
   ```bash
   # macOS
   brew install ollama

   # Linux
   curl -fsSL https://ollama.com/install.sh | sh

   # Windows — download from https://ollama.com/download
   ```

2. Start the Ollama server:
   ```bash
   ollama serve
   ```

3. Pull a model:
   ```bash
   ollama pull llama3
   ollama pull codestral
   ollama pull qwen2.5-coder
   ```

### Configuration

```json
{
  "profiles": {
    "local-llama": {
      "provider": "ollama",
      "displayName": "Llama 3 (Local)",
      "ollamaHost": "http://localhost:11434",
      "ollamaModel": "llama3"
    }
  }
}
```

### Configuration Fields

| Field | Default | Description |
|---|---|---|
| `ollamaHost` | `http://localhost:11434` | Ollama server URL |
| `ollamaModel` | *(required)* | Model name as shown by `ollama list` |

### Recommended Models for Coding

| Model | Size | Best For |
|---|---|---|
| `llama3` / `llama3.1` | 8B | General-purpose, good balance |
| `codestral` | 22B | Code generation and understanding |
| `qwen2.5-coder` | 7B | Code-focused, efficient |
| `deepseek-coder-v2` | 16B | Strong code completion |
| `mistral` | 7B | Fast general-purpose |

### Remote Ollama

If Ollama is running on another machine:

```json
{
  "profiles": {
    "remote-ollama": {
      "provider": "ollama",
      "ollamaHost": "http://192.168.1.100:11434",
      "ollamaModel": "llama3"
    }
  }
}
```

---

## Using Multiple Profiles

PGA supports multiple LLM profiles simultaneously. This is useful for:

- Different models for different tasks (e.g., fast model for chat, powerful model for code review)
- Local models for privacy-sensitive work, cloud models for complex tasks
- Testing different providers

### Configure Multiple Profiles

```json
{
  "defaultProfile": "azure-gpt4",
  "profiles": {
    "azure-gpt4": {
      "provider": "azure-openai",
      "endpoint": "https://my-resource.openai.azure.com",
      "deploymentName": "gpt-4o",
      "authMode": "key",
      "apiKey": "..."
    },
    "local-fast": {
      "provider": "ollama",
      "ollamaHost": "http://localhost:11434",
      "ollamaModel": "llama3"
    },
    "local-code": {
      "provider": "ollama",
      "ollamaHost": "http://localhost:11434",
      "ollamaModel": "codestral"
    }
  }
}
```

### Select a Profile

```bash
# At startup
pga chat --profile local-fast

# Mid-session
/profile local-code
```

### Agent-Specific Profiles

Agents can specify a preferred profile:

```yaml
---
name: code-reviewer
profile: azure-gpt4
---
```

### Profile Precedence

1. `--profile` CLI option (highest)
2. Agent's `profile` frontmatter field
3. Auto-select rules
4. `defaultProfile` in config (lowest)

---

## Troubleshooting

### Azure OpenAI

| Error | Cause | Solution |
|---|---|---|
| `401 Unauthorized` | Invalid API key | Check the key in Azure Portal → Keys and Endpoint |
| `404 Not Found` | Wrong endpoint or deployment | Verify endpoint URL and deployment name |
| `429 Too Many Requests` | Rate limit exceeded | Wait and retry, or upgrade your tier |
| `Endpoint is required` | Missing endpoint in config | Add `endpoint` to the profile |

### Ollama

| Error | Cause | Solution |
|---|---|---|
| `Connection refused` | Ollama server not running | Run `ollama serve` |
| `Model not found` | Model not pulled | Run `ollama pull <model>` |
| Slow responses | Large model on limited hardware | Try a smaller model |

### Entra ID Authentication

| Error | Cause | Solution |
|---|---|---|
| `Interactive authentication required` | No cached credentials | Run `az login` |
| `AADSTS50076` | MFA required | Complete MFA in browser |
| `Insufficient privileges` | Missing RBAC role | Assign **Cognitive Services OpenAI User** role |
