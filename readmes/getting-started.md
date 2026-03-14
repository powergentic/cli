# Getting Started

This guide walks you through setting up PGA for the first time and starting your first AI-powered chat session.

## Step 1: Initialize Configuration

PGA stores its configuration at `~/.powergentic/` in JSON or YAML format. Create a default config with:

```bash
pga config init
```

This creates a default configuration file with a placeholder Azure OpenAI profile.

## Step 2: Add an LLM Profile

PGA needs at least one configured LLM provider. You can add a profile interactively:

```bash
pga config add-profile my-gpt4
```

This launches an interactive wizard that asks you to:

1. **Select a provider** — `azure-openai`, `azure-ai-foundry`, or `ollama`
2. **Enter provider-specific settings** — endpoint, deployment, API key, etc.
3. **Optionally set as default** — if this is your first or preferred profile

### Quick Setup: Ollama (Local Models)

If you have [Ollama](https://ollama.com) installed locally:

```bash
# Install and start Ollama (macOS)
brew install ollama
ollama serve

# Pull a model
ollama pull llama3

# Add the profile to PGA
pga config add-profile local
# Select "ollama" as provider
# Host: http://localhost:11434
# Model: llama3
```

### Quick Setup: Azure OpenAI

```bash
pga config add-profile azure
# Select "azure-openai" as provider
# Endpoint: https://your-resource.openai.azure.com
# Deployment: your-deployment-name
# Auth mode: key
# API key: your-api-key-here
```

## Step 3: Validate Configuration

Confirm everything is set up correctly:

```bash
pga config validate
```

You should see:

```
✓ Configuration is valid.
```

To view your full config (with secrets masked):

```bash
pga config show
```

## Step 4: Start Chatting

Navigate to a project directory and launch an interactive session:

```bash
cd ~/my-project
pga chat
```

Or simply run `pga` with no arguments — it defaults to interactive chat:

```bash
cd ~/my-project
pga
```

You'll see:

```
╔═══════════════════════════════════╗
║           P G A                   ║
╚═══════════════════════════════════╝
Powergentic CLI — AI-powered coding assistant

Using LLM profile: my-gpt4

❯
```

Type your question or request at the `❯` prompt:

```
❯ What does this project do? Give me a high-level overview.
```

The agent will use its built-in tools (file reading, directory listing, grep search, etc.) to explore your project and provide an answer.

## Step 5: Try Single-Shot Commands

### Explain Something

```bash
pga explain "docker run -it --rm -v $(pwd):/app node:18 npm test"
```

### Get a Shell Command Suggestion

```bash
pga suggest "find all TODO comments in this codebase"
```

## Step 6: Set Up Project Agents (Optional)

Create project-specific AI instructions:

```bash
cd ~/my-project
pga init
```

This creates:
- `AGENTS.md` — Global instructions for the AI when working on your project
- `agents/code-reviewer.agent.md` — An example custom agent

Edit `AGENTS.md` to describe your project, coding standards, and conventions. The AI will follow these instructions in every chat session within that directory.

## Interactive Chat Commands

Inside an interactive chat session, you can use slash commands:

| Command | Description |
|---|---|
| `/help` | Show all available commands |
| `/exit` or `/quit` | End the session |
| `/clear` | Clear conversation history |
| `/agents` | List available agents |
| `/agent <name>` | Switch to a specific agent |
| `/profile <name>` | Switch LLM profile mid-session |
| `/status` | Show current session info |
| `/multiline` | Enter multi-line input mode |

## Example Session

```
❯ What files are in this project?

🔧 directory_list: Listing contents of /Users/chris/my-project
✓ Tool result: 12 entries

The project contains:
- `src/` — Source code directory
- `tests/` — Unit tests
- `package.json` — Node.js dependencies
- `README.md` — Project documentation
...

❯ Show me the main entry point

🔧 file_read: Reading src/index.ts
✓ Tool result: 45 lines

Here's your main entry point (`src/index.ts`):
...

❯ /exit
Goodbye! 👋
```

## Next Steps

- [Commands](commands.md) — Full reference for all CLI commands
- [Agents](agents.md) — Creating custom agents for your project
- [Configuration](configuration.md) — Advanced configuration options
- [Tools](tools.md) — Understanding the tools available to the agent
