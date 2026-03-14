# Configuration Reference

PGA stores configuration in your `.powergentic/` directory, supporting both **JSON** and **YAML** formats:

```
~/.powergentic/config.json      # JSON format (default)
~/.powergentic/config.yaml      # YAML format
~/.powergentic/config.yml       # YAML format (short extension)
```

PGA searches for config files in this priority order: `config.json` → `config.yaml` → `config.yml`. The first file found is used.

This page documents every field in the configuration file.

## Creating the Config File

```bash
pga config init
```

This creates a default `config.json` at `~/.powergentic/`. You can also create the file manually in either JSON or YAML format.

## Local Override Files

After loading the primary config file, PGA looks for a **local override** file in the same directory:

```
config.local.json
config.local.yaml
config.local.yml
```

If found, values from the local override are **merged** on top of the base configuration. This enables a powerful workflow:

- **`config.yaml`** — Shared configuration checked into source control (endpoints, deployment names, structure)
- **`config.local.yaml`** — Private overrides with secrets (API keys, tokens) excluded from source control via `.gitignore`

### How Merging Works

| Field | Merge Behavior |
|---|---|
| `profiles` | Override profiles are merged by name. New profiles are added; existing profiles have their non-null fields overridden. |
| `defaultProfile` | Overridden if the local file specifies a non-default value |
| `toolSafety.trustedPaths` | Lists are merged (union of both) |
| `autoSelect.rules` | If the override has rules, they replace the base rules entirely |
| `ui` | Individual fields are overridden |

### Example: Shared Config + Local Secrets

**`config.yaml`** (checked into source control):
```yaml
version: "1.0"
defaultProfile: azure
profiles:
  azure:
    provider: azure-openai
    endpoint: https://my-org.openai.azure.com
    deploymentName: gpt-4o
    authMode: key
  local:
    provider: ollama
    ollamaModel: llama3
toolSafety:
  mode: prompt-writes
```

**`config.local.yaml`** (in `.gitignore`, not committed):
```yaml
profiles:
  azure:
    apiKey: sk-my-secret-api-key-12345
toolSafety:
  trustedPaths:
    - /Users/chris/my-project
```

The merged result will have the Azure profile with both the endpoint/deployment from the base and the API key from the local override.

## Full Schema

### JSON Format

```jsonc
{
  "version": "1.0",
  "defaultProfile": "default",
  "profiles": {
    "default": {
      "provider": "azure-openai",
      "displayName": "My Azure GPT-4o",
      "endpoint": "https://my-resource.openai.azure.com",
      "apiKey": "sk-...",
      "deploymentName": "gpt-4o",
      "modelId": null,
      "apiVersion": null,
      "authMode": "key",
      "tenantId": null,
      "ollamaHost": "http://localhost:11434",
      "ollamaModel": null,
      "maxTokens": null,
      "temperature": null,
      "topP": null
    }
  },
  "autoSelect": {
    "enabled": false,
    "rules": []
  },
  "toolSafety": {
    "mode": "prompt-writes",
    "trustedPaths": []
  },
  "ui": {
    "theme": "default",
    "showToolCalls": true,
    "streamResponses": true
  }
}
```

### YAML Format

```yaml
version: "1.0"
defaultProfile: default
profiles:
  default:
    provider: azure-openai
    displayName: My Azure GPT-4o
    endpoint: https://my-resource.openai.azure.com
    apiKey: sk-...
    deploymentName: gpt-4o
    authMode: key
    ollamaHost: http://localhost:11434
autoSelect:
  enabled: false
  rules: []
toolSafety:
  mode: prompt-writes
  trustedPaths: []
ui:
  theme: default
  showToolCalls: true
  streamResponses: true
```

---

## Top-Level Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `version` | `string` | `"1.0"` | Config file schema version |
| `defaultProfile` | `string` | `"default"` | Name of the LLM profile to use when none is specified |
| `profiles` | `object` | `{}` | Dictionary of named LLM profile configurations |
| `autoSelect` | `object` | *(see below)* | Automatic profile selection rules |
| `toolSafety` | `object` | *(see below)* | Tool execution safety settings |
| `ui` | `object` | *(see below)* | UI/rendering preferences |

---

## Profiles

Each key in the `profiles` object is a profile name, and its value is a profile configuration object.

### Profile Fields

| Field | Type | Default | Providers | Description |
|---|---|---|---|---|
| `provider` | `string` | `"azure-openai"` | All | Provider type: `"azure-openai"`, `"azure-ai-foundry"`, or `"ollama"` |
| `displayName` | `string?` | `null` | All | Human-readable name for display |
| `endpoint` | `string?` | `null` | Azure | Azure OpenAI endpoint URL |
| `apiKey` | `string?` | `null` | Azure | API key (when `authMode` is `"key"`) |
| `deploymentName` | `string?` | `null` | Azure | Azure OpenAI deployment/model name |
| `modelId` | `string?` | `null` | Azure | Optional model identifier |
| `apiVersion` | `string?` | `null` | Azure | Optional API version override |
| `authMode` | `string` | `"key"` | Azure | Authentication mode: `"key"` or `"entra"` |
| `tenantId` | `string?` | `null` | Azure | Azure tenant ID (for Entra ID auth) |
| `ollamaHost` | `string` | `"http://localhost:11434"` | Ollama | Ollama server URL |
| `ollamaModel` | `string?` | `null` | Ollama | Ollama model name (e.g., `"llama3"`, `"codestral"`) |
| `maxTokens` | `int?` | `null` | All | Maximum tokens in the response |
| `temperature` | `float?` | `null` | All | Sampling temperature (0.0–2.0) |
| `topP` | `float?` | `null` | All | Nucleus sampling parameter |

### Example: Azure OpenAI with API Key

```json
{
  "profiles": {
    "azure-gpt4": {
      "provider": "azure-openai",
      "displayName": "GPT-4o on Azure",
      "endpoint": "https://my-resource.openai.azure.com",
      "deploymentName": "gpt-4o",
      "authMode": "key",
      "apiKey": "your-api-key-here",
      "temperature": 0.7
    }
  }
}
```

### Example: Azure OpenAI with Entra ID

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

### Example: Ollama (Local)

```json
{
  "profiles": {
    "local": {
      "provider": "ollama",
      "displayName": "Local Llama 3",
      "ollamaHost": "http://localhost:11434",
      "ollamaModel": "llama3"
    }
  }
}
```

### Profile Validation

Each profile is validated based on its provider:

| Provider | Required Fields |
|---|---|
| `azure-openai` | `endpoint`, `deploymentName`, and `apiKey` (if `authMode` is `"key"`) |
| `azure-ai-foundry` | `endpoint`, `deploymentName`, and `apiKey` (if `authMode` is `"key"`) |
| `ollama` | `ollamaModel` |

Run `pga config validate` to check all profiles.

---

## Auto-Select

Automatic profile selection lets PGA choose the right LLM profile based on contextual rules.

```json
{
  "autoSelect": {
    "enabled": true,
    "rules": [
      {
        "pattern": "*.py",
        "profile": "codestral",
        "description": "Use Codestral for Python projects"
      },
      {
        "pattern": "*",
        "profile": "gpt4",
        "description": "Default to GPT-4 for everything else"
      }
    ]
  }
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `enabled` | `bool` | `false` | Whether auto-select is active |
| `rules` | `array` | `[]` | Ordered list of selection rules |

### Rule Fields

| Field | Type | Description |
|---|---|---|
| `pattern` | `string` | Glob pattern to match against |
| `profile` | `string` | Name of the profile to use when matched |
| `description` | `string?` | Optional human-readable description |

Rules are evaluated in order — the first matching rule wins. Auto-select only applies when no explicit profile is specified via `--profile` or agent configuration.

---

## Tool Safety

Controls how PGA handles tool execution approval.

```json
{
  "toolSafety": {
    "mode": "prompt-writes",
    "trustedPaths": [
      "/Users/chris/projects/my-app"
    ]
  }
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `mode` | `string` | `"prompt-writes"` | Approval mode (see below) |
| `trustedPaths` | `string[]` | `[]` | Directories where write operations are auto-approved |

### Safety Modes

| Mode | Behavior |
|---|---|
| `auto-approve` | All tool executions proceed without prompting |
| `prompt-writes` | Read-only tools auto-execute; write and execute tools require approval |
| `prompt-always` | Every tool invocation requires explicit approval |

### Tool Safety Levels

Each built-in tool has an assigned safety level:

| Safety Level | Behavior | Tools |
|---|---|---|
| **ReadOnly** | Never prompts (except in `prompt-always` mode) | `file_read`, `file_search`, `grep_search`, `directory_list`, `git_operations`, `web_fetch` |
| **Write** | Prompts in `prompt-writes` and `prompt-always` modes | `file_write`, `file_edit` |
| **Execute** | Prompts in `prompt-writes` and `prompt-always` modes | `shell_execute` |

### Approval Prompt

When approval is required, PGA displays:

```
⚠ Tool: shell_execute
  Action: Execute command: npm test
  Approve? (y/N):
```

---

## UI Settings

```json
{
  "ui": {
    "theme": "default",
    "showToolCalls": true,
    "streamResponses": true
  }
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `theme` | `string` | `"default"` | UI color theme |
| `showToolCalls` | `bool` | `true` | Display tool invocations and results in the chat output |
| `streamResponses` | `bool` | `true` | Stream LLM responses token-by-token instead of waiting for completion |

When `showToolCalls` is `true`, you'll see:

```
🔧 file_read: Reading /path/to/file.cs
✓ Tool result: 42 lines read
```

When `false`, tools execute silently and only the final response is shown.

---

## Config File Location

The configuration directory is always:

```
~/.powergentic/
```

Which expands to:

| OS | Path |
|---|---|
| macOS | `/Users/<username>/.powergentic/` |
| Linux | `/home/<username>/.powergentic/` |
| Windows | `C:\Users\<username>\.powergentic\` |

### File Search Order

PGA searches for config files in this priority order:

1. **Project-level** (`.powergentic/` in the current project directory):
   - `config.json` → `config.yaml` → `config.yml`
2. **Global** (`~/.powergentic/`):
   - `config.json` → `config.yaml` → `config.yml`

The first file found is used as the base configuration.

After loading the base config, PGA searches the **same directory** for a local override:
- `config.local.json` → `config.local.yaml` → `config.local.yml`

If found, the override values are merged on top of the base configuration.

### Recommended `.gitignore` Entries

To keep secrets out of source control, add these to your `.gitignore`:

```gitignore
# PGA local config (contains secrets)
.powergentic/config.local.json
.powergentic/config.local.yaml
.powergentic/config.local.yml
```

## Profile Resolution Order

When PGA needs to determine which LLM profile to use, it follows this precedence:

1. **`--profile` CLI option** — Highest priority, always wins
2. **Agent-specified profile** — The `profile` field in an `*.agent.md` frontmatter
3. **Auto-select rules** — If enabled and no explicit profile is set
4. **Default profile** — The `defaultProfile` field in config

## Managing Profiles via CLI

```bash
pga config add-profile <name>       # Interactive wizard
pga config remove-profile <name>    # Remove a profile
pga config list-profiles             # Show all profiles
pga config set-default <name>        # Change default profile
pga config validate                  # Validate entire config
pga config show                      # Display config tree
```

See [Commands](commands.md) for full details.
