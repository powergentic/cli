---
title: Architecture
description: Understand the internal architecture of Powergentic CLI — project structure, components, data flow, and extension points.
---

# :octicons-code-24: Architecture

This document describes the internal architecture of PGA — the project structure, key components, and how they interact.

---

## Solution Structure

```
PowergenticAgent/
├── Directory.Build.props          # Shared build settings (net10.0, nullable, etc.)
├── PowergenticAgent.sln           # Solution file
├── src/
│   ├── Pga.Core/                  # Core library — agents, tools, chat, config
│   │   ├── Agents/                # Agent system (loading, parsing, scoping)
│   │   ├── Chat/                  # Chat orchestrator and message history
│   │   ├── Configuration/         # Config models and manager
│   │   ├── Extensions/            # DI registration helpers
│   │   ├── Providers/             # LLM provider factory
│   │   └── Tools/                 # Built-in tool implementations
│   └── Pga.Cli/                   # CLI application — commands and rendering
│       ├── Commands/              # CLI command definitions
│       └── Rendering/             # Terminal output formatting
├── tests/
│   └── Pga.Tests/                 # Unit tests
│       ├── Agents/
│       ├── Configuration/
│       └── Tools/
└── docs/                          # Documentation
```

---

## Projects

### Pga.Core

The core library containing all business logic. Has no dependency on the CLI or any UI framework. Could be embedded in other applications.

**Dependencies:**

- `Microsoft.Extensions.AI` — AI abstractions (`IChatClient`, `AIFunction`, `ChatMessage`)
- `Microsoft.Extensions.AI.OpenAI` — OpenAI/Azure OpenAI `IChatClient` adapter
- `Azure.AI.OpenAI` — Azure OpenAI client SDK
- `Azure.Identity` — Azure Entra ID authentication
- `OllamaSharp` — Ollama `IChatClient` implementation
- `Markdig` — Markdown processing
- `YamlDotNet` — YAML frontmatter parsing
- `Microsoft.Extensions.Hosting` — Dependency injection support

### Pga.Cli

The console application. References `Pga.Core` and adds CLI-specific concerns: command parsing, interactive REPL, and rich terminal rendering.

**Additional dependencies:**

- `System.CommandLine` — CLI argument parsing and command routing
- `Spectre.Console` — Rich terminal output (panels, tables, figlet text, colors)

**Build output:** The assembly name is `pga` (not `Pga.Cli`), and it's configured for single-file self-contained publishing.

### Pga.Tests

xUnit-based unit tests covering agents, configuration, and tools.

---

## Component Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      Pga.Cli                            │
│  ┌──────────┐  ┌───────────┐  ┌──────────────────────┐ │
│  │ Program  │  │ Commands  │  │  ConsoleRenderer     │ │
│  │ (entry)  │  │ (chat,    │  │  (Spectre.Console)   │ │
│  │          │  │  explain, │  │                      │ │
│  │          │  │  suggest, │  │                      │ │
│  │          │  │  config,  │  │                      │ │
│  │          │  │  init)    │  │                      │ │
│  └──────────┘  └───────────┘  └──────────────────────┘ │
└─────────────────────────┬───────────────────────────────┘
                          │ references
┌─────────────────────────▼───────────────────────────────┐
│                      Pga.Core                           │
│                                                         │
│  ┌───────────────────┐  ┌────────────────────────────┐  │
│  │  ConfigManager    │  │  ChatOrchestrator          │  │
│  │  (load/save       │  │  (conversation loop,       │  │
│  │   config files)   │  │   tool calling,            │  │
│  │                   │  │   message history)         │  │
│  │  PgaConfiguration │  │                            │  │
│  │  LlmProfile       │  │                            │  │
│  └───────────────────┘  └──────────┬─────────────────┘  │
│                                    │                    │
│  ┌───────────────────┐  ┌──────────▼─────────────────┐  │
│  │  AgentLoader      │  │  LlmProviderFactory        │  │
│  │  (discover +      │  │  (creates IChatClient      │  │
│  │   parse agents)   │  │   from profile config)     │  │
│  │                   │  │                            │  │
│  │  AgentParser      │  │  ┌──────────────────────┐  │  │
│  │  AgentDefinition  │  │  │ Azure OpenAI Client  │  │  │
│  │  AgentScope       │  │  │ Ollama Client        │  │  │
│  │  AgentCollection  │  │  └──────────────────────┘  │  │
│  └───────────────────┘  └────────────────────────────┘  │
│                                                         │
│  ┌───────────────────────────────────────────────────┐  │
│  │  ToolRegistry                                     │  │
│  │  ┌──────────────┐ ┌────────────┐ ┌─────────────┐ │  │
│  │  │ shell_execute│ │ file_read  │ │ file_write  │ │  │
│  │  │ file_edit    │ │file_search │ │ grep_search │ │  │
│  │  │directory_list│ │git_ops     │ │ web_fetch   │ │  │
│  │  └──────────────┘ └────────────┘ └─────────────┘ │  │
│  │                                                   │  │
│  │  ToolSafety (approval logic)                      │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

---

## Key Components

### ConfigManager

**Location:** `Pga.Core/Configuration/ConfigManager.cs`

Manages the `~/.powergentic/` configuration files. Supports JSON (`.json`) and YAML (`.yaml`, `.yml`) formats via pluggable `IConfigProvider` implementations (`JsonConfigProvider`, `YamlConfigProvider`). Handles:

- Loading configuration from `config.json`, `config.yaml`, or `config.yml` (with defaults if no file exists)
- Merging local override files (`config.local.json`, `config.local.yaml`, `config.local.yml`) on top of the base config
- Saving configuration in the same format as the source file
- Profile CRUD operations (upsert, remove, get)
- Profile resolution (CLI override → agent preference → auto-select → default)
- Validation

### AgentLoader

**Location:** `Pga.Core/Agents/AgentLoader.cs`

Discovers and loads agent definitions from a project directory:

1. Loads `AGENTS.md` (global instructions)
2. Loads `.powergentic/agents/*.agent.md` (root-level named agents)
3. Loads `.github/agents/*.agent.md` (GitHub convention)
4. Recursively discovers scoped agents in subdirectories

Returns an `AgentCollection` with resolution logic.

### AgentMarkdownParser

**Location:** `Pga.Core/Agents/AgentMarkdownParser.cs`

Parses agent markdown files:

- Extracts YAML frontmatter (between `---` delimiters)
- Deserializes frontmatter into agent metadata (name, description, profile, tools)
- Returns `AgentDefinition` objects

### ChatOrchestrator

**Location:** `Pga.Core/Chat/ChatOrchestrator.cs`

The core engine that manages the LLM conversation loop:

1. Builds a system prompt from global + agent-specific instructions
2. Creates an `IChatClient` via `LlmProviderFactory`
3. Configures function-calling middleware (via `ChatClientBuilder.UseFunctionInvocation`)
4. Processes user messages through the LLM
5. Handles tool call iterations (up to 25 per message)
6. Fires events for UI feedback (`OnToolInvocation`, `OnToolResult`, `OnStreamingToken`, `OnToolApprovalNeeded`)

### LlmProviderFactory

**Location:** `Pga.Core/Providers/LlmProviderFactory.cs`

Static factory that creates `IChatClient` instances from profile configurations:

| Provider | Implementation |
|---|---|
| `azure-openai` | `AzureOpenAIClient` → `.GetChatClient()` → `.AsIChatClient()` |
| `azure-ai-foundry` | Same as `azure-openai` |
| `ollama` | `OllamaApiClient` (implements `IChatClient` natively) |

### ToolRegistry

**Location:** `Pga.Core/Tools/ToolRegistry.cs`

Registry of all available tools. Provides:

- Registration and lookup by name
- Filtered `AITool` lists (whitelist/blacklist support for agents)
- `CreateDefault()` factory that registers all 9 built-in tools

### ToolSafety

**Location:** `Pga.Core/Tools/ToolSafety.cs`

Enforces tool execution safety:

- Checks each tool invocation against the configured safety mode
- Calls the approval callback when prompting is required
- Three modes: `auto-approve`, `prompt-writes`, `prompt-always`

### ConsoleRenderer

**Location:** `Pga.Cli/Rendering/ConsoleRenderer.cs`

Static class handling all terminal output via Spectre.Console:

- Banner, prompts, assistant messages, tool calls
- Color-coded feedback (success/error/warning/info)
- Tool approval prompts
- Help and status displays

---

## Data Flow

### Interactive Chat Session

```
User types message
        │
        ▼
  ChatCommand.RunInteractiveChat()
        │
        ▼
  ChatOrchestrator.SendMessageAsync(input)
        │
        ├── 1. Add user message to MessageHistory
        │
        ├── 2. Build IChatClient with tool-calling middleware
        │       └── LlmProviderFactory.CreateChatClient(profile)
        │
        ├── 3. Call IChatClient.GetResponseAsync(messages, options)
        │       └── options include AITool list from ToolRegistry
        │
        ├── 4. Process response
        │       ├── If tool calls requested:
        │       │   ├── Fire OnToolInvocation event
        │       │   ├── Check ToolSafety (may fire OnToolApprovalNeeded)
        │       │   ├── Execute tool function
        │       │   ├── Fire OnToolResult event
        │       │   ├── Add tool result to messages
        │       │   └── Loop back to step 3 (up to 25 iterations)
        │       │
        │       └── If text response:
        │           └── Return response text
        │
        ▼
  ConsoleRenderer.RenderAssistantMessage(response)
```

### Profile Resolution

```
--profile CLI flag?  ──Yes──▶  Use specified profile
        │
        No
        │
        ▼
Agent has profile field?  ──Yes──▶  Use agent's profile
        │
        No
        │
        ▼
Auto-select enabled?  ──Yes──▶  Evaluate rules, use first match
        │
        No
        │
        ▼
Use defaultProfile from config
```

---

## Microsoft.Extensions.AI Integration

PGA is built on the `Microsoft.Extensions.AI` abstraction layer, which provides:

- **`IChatClient`** — Unified interface for all LLM providers
- **`ChatMessage`** / **`ChatRole`** — Message representation
- **`AIFunction`** / **`AITool`** — Function-calling abstractions
- **`FunctionCallContent`** / **`FunctionResultContent`** — Tool call/result types
- **`ChatClientBuilder.UseFunctionInvocation()`** — Automatic tool-calling middleware

!!! info
    This means PGA can support any LLM provider that implements `IChatClient`, making it straightforward to add new providers in the future.

---

## Adding a New Tool

To add a new built-in tool:

1. Create a class implementing `IAgentTool` in `Pga.Core/Tools/`:

```csharp
public class MyNewTool : IAgentTool
{
    public string Name => "my_new_tool";
    public string Description => "Description of what the tool does";
    public ToolSafetyLevel SafetyLevel => ToolSafetyLevel.ReadOnly;

    public AIFunction ToAIFunction()
    {
        return AIFunctionFactory.Create(
            (string param1, int param2) =>
            {
                // Tool implementation
                return "result";
            },
            name: Name,
            description: Description);
    }
}
```

2. Register it in `ToolRegistry.CreateDefault()`:

```csharp
registry.Register(new MyNewTool());
```

---

## Adding a New LLM Provider

To add a new LLM provider:

1. Add the provider's NuGet package to `Pga.Core`
2. Add a new case in `LlmProviderFactory.CreateChatClient()`:

```csharp
"my-provider" => CreateMyProviderClient(profile),
```

3. Implement the factory method that returns an `IChatClient`
4. Add the necessary fields to `LlmProfile` (with `[JsonPropertyName]` attributes)
5. Add validation in `LlmProfile.Validate()`
6. Update the interactive wizard in `ConfigCommand.CreateAddProfileCommand()`

---

## Build Configuration

### Directory.Build.props

Shared settings applied to all projects:

| Setting | Value |
|---|---|
| `TargetFramework` | `net10.0` |
| `ImplicitUsings` | `enable` |
| `Nullable` | `enable` |
| `LangVersion` | `latest` |

### Pga.Cli Publish Settings

| Setting | Value | Purpose |
|---|---|---|
| `PublishSingleFile` | `true` | Single executable output |
| `SelfContained` | `true` | Includes .NET runtime |
| `PublishTrimmed` | `true` | Removes unused code |
| `AssemblyName` | `pga` | Binary is named `pga` |
