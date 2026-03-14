---
title: Discover Powergentic CLI
description: An open-source, AI-powered coding assistant for the command line with configurable LLM backends, customizable agents, and built-in developer tools.
---

# :fontawesome-regular-compass: Discover Powergentic CLI

**Powergentic CLI** (`pga`) is an open-source, AI-powered coding assistant for the command line. It provides GitHub Copilot CLI-like functionality with configurable LLM backends, a customizable agent system, and built-in developer tools.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/powergentic/cli/blob/main/LICENSE)
![Framework: .NET 10+](https://img.shields.io/badge/framework-.NET%2010%2B-blue)
[![Build and Test](https://github.com/powergentic/cli/actions/workflows/build.yml/badge.svg)](https://github.com/powergentic/cli/actions/workflows/build.yml)
![Ollama: Ready](https://img.shields.io/badge/Ollama-ready-purple)
![OpenAI: Ready](https://img.shields.io/badge/OpenAI-ready-purple)
![Azure AI Foundry: Ready](https://img.shields.io/badge/Azure%20AI%20Foundry-ready-purple)

[:octicons-rocket-24: Get Started](getting-started/index.md){ .md-button .md-button--primary }
[:octicons-download-24: Installation](installation/index.md){ .md-button }

---

## :rocket: Quick Start

```bash
pga                              # Start interactive chat (default)
pga chat                         # Explicit interactive chat session
pga explain "git rebase"         # Single-shot explanation
pga suggest "find large files"   # Get a shell command suggestion
pga config init                  # Create initial configuration
pga init                         # Scaffold AGENTS.md and agents/ in a project
```

![Powergentic CLI screenshot](images/pga-cli-screenshot.png)

---

## :bulb: Key Features

<div class="grid cards" markdown>

-   :material-chat-processing:{ .lg .middle } __Interactive Chat REPL__

    ---

    Multi-turn conversations with full tool access for exploring and modifying your codebase.

    [:octicons-arrow-right-24: Commands](commands/index.md)

-   :material-robot:{ .lg .middle } __Agent System__

    ---

    Define project-specific AI behaviors with `AGENTS.md` and `*.agent.md` files, following the GitHub Copilot agent convention.

    [:octicons-arrow-right-24: Agents](agents/index.md)

-   :material-wrench:{ .lg .middle } __9 Built-in Tools__

    ---

    File I/O, shell execution, git operations, grep search, web fetch, and more — all with configurable safety levels.

    [:octicons-arrow-right-24: Tools](tools/index.md)

-   :material-swap-horizontal:{ .lg .middle } __Multiple LLM Backends__

    ---

    Azure OpenAI (API key + Entra ID), Azure AI Foundry, and Ollama for local models.

    [:octicons-arrow-right-24: LLM Providers](llm-providers/index.md)

</div>

- :material-shield-check: **Tool Safety** — Configurable approval modes (auto-approve, prompt-writes, prompt-always)
- :material-folder-multiple: **Scoped Agents** — Agents that only apply to specific directories in a monorepo
- :material-tune: **Multiple Profiles** — Switch between LLM configurations per agent or per session
- :material-auto-fix: **Auto-Select Rules** — Automatically pick the right model based on context
- :material-lightning-bolt: **Single-Shot Commands** — `explain` and `suggest` for quick answers

---

## :brain: Use Cases

Powergentic CLI is great for:

- Exploring unfamiliar codebases interactively from the terminal
- Getting command-line explanations and shell suggestions
- Automating code review with custom agents
- Working across multiple projects with scoped agent configurations
- Running local models via Ollama for privacy-sensitive work
- Integrating AI assistance into your terminal workflow

---

## :gear: LLM Providers

Powergentic CLI supports multiple LLM backends. Configure one or many profiles and switch between them per session or per agent.

<div class="grid cards" markdown>

-   :material-microsoft-azure:{ .lg .middle } __Azure OpenAI__

    ---

    Use Azure-hosted OpenAI models like GPT-4o with API key or Entra ID authentication.

    [:octicons-arrow-right-24: Setup Guide](llm-providers/index.md#azure-openai)

-   :material-microsoft-azure:{ .lg .middle } __Azure AI Foundry__

    ---

    Deploy and use models via Azure AI Foundry.

    [:octicons-arrow-right-24: Setup Guide](llm-providers/index.md#azure-ai-foundry)

-   :simple-ollama:{ .lg .middle } __Ollama__

    ---

    Run open-source models locally — Llama 3, Codestral, Qwen, and more.

    [:octicons-arrow-right-24: Setup Guide](llm-providers/index.md#ollama-local-models)

</div>

---

## :raised_hands: Contributing

We welcome contributions, feedback, and new ideas. Whether it's a bug report or a pull request, head over to our [GitHub repository](https://github.com/powergentic/cli) to start collaborating!

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- An LLM provider: Azure OpenAI, Azure AI Foundry, or [Ollama](https://ollama.com) (for local models)
