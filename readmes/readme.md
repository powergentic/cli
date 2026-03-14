# Powergentic CLI Documentation

**Powergentic CLI** (`pga`) is an open-source, AI-powered coding assistant for the command line. It provides GitHub Copilot CLI-like functionality with configurable LLM backends, a customizable agent system, and built-in developer tools.

![Powergentic CLI screenshot](images/pga-cli-screenshot.png)

## Table of Contents

| Document | Description |
|---|---|
| [Getting Started](getting-started.md) | Quick start guide — first install, first chat |
| [Installation](installation.md) | Building from source, publishing a self-contained binary |
| [Commands](commands.md) | Full reference for every CLI command and option |
| [Configuration](configuration.md) | Config file reference — JSON and YAML formats, local overrides |
| [Agents](agents.md) | Agent system — `AGENTS.md`, `*.agent.md`, scoping |
| [Tools](tools.md) | All 9 built-in tools the AI agent can invoke |
| [LLM Providers](llm-providers.md) | Azure OpenAI, Azure AI Foundry, and Ollama setup |
| [Architecture](architecture.md) | Project structure, components, and extension points |

## At a Glance

```
pga                        # Start interactive chat (default)
pga chat                   # Explicit interactive chat session
pga explain "git rebase"   # Single-shot explanation
pga suggest "find large files"   # Get a shell command suggestion
pga config init            # Create initial configuration
pga init                   # Scaffold AGENTS.md and agents/ in a project
```

## Key Features

- **Interactive chat REPL** — Multi-turn conversations with full tool access
- **Single-shot commands** — `explain` and `suggest` for quick answers
- **Agent system** — Define project-specific AI behaviors with `AGENTS.md` and `*.agent.md` files, following the GitHub Copilot agent convention
- **Scoped agents** — Agents that only apply to specific directories in a monorepo
- **9 built-in tools** — File I/O, shell execution, git operations, grep search, web fetch, and more
- **Tool safety** — Configurable approval modes (auto-approve, prompt-writes, prompt-always)
- **Multiple LLM backends** — Azure OpenAI (API key + Entra ID), Azure AI Foundry, Ollama (local models)
- **Multiple profiles** — Switch between LLM configurations per agent or per session
- **Auto-select rules** — Automatically pick the right model based on context

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- An LLM provider: Azure OpenAI, Azure AI Foundry, or [Ollama](https://ollama.com) (for local models)

## License

PGA is licensed under the [MIT License](../LICENSE).
