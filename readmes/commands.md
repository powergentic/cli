# Commands Reference

PGA provides five top-level commands. When invoked with no subcommand, it defaults to interactive chat.

```
pga [command] [options]
```

## `pga` (default)

When run with no subcommand, PGA starts an interactive chat session in the current directory. This is equivalent to `pga chat`.

```bash
pga
```

---

## `pga chat`

Start an interactive chat with the AI agent.

```bash
pga chat [options]
```

### Options

| Option | Alias | Type | Default | Description |
|---|---|---|---|---|
| `--path` | `-p` | `string` | Current directory | Project root directory to work with |
| `--agent` | `-a` | `string` | *(none)* | Name of a specific agent to use |
| `--profile` | | `string` | *(default profile)* | LLM profile to use (overrides agent/auto-select) |

### Examples

```bash
# Chat in the current directory
pga chat

# Chat in a specific project
pga chat --path ~/projects/my-app

# Chat using a specific agent
pga chat --agent code-reviewer

# Chat using a specific LLM profile
pga chat --profile gpt4-turbo

# Combine options
pga chat -p ~/projects/my-app -a code-reviewer --profile gpt4-turbo
```

### Interactive Slash Commands

Inside a chat session, the following slash commands are available:

| Command | Description |
|---|---|
| `/help` | Show all available slash commands |
| `/exit`, `/quit` | End the session |
| `/clear` | Clear conversation history and start fresh |
| `/agents` | List all available agents for this project |
| `/agent <name>` | Switch to a named agent |
| `/agent` | Show the currently active agent |
| `/profile <name>` | Switch LLM profile mid-session |
| `/profile` | Show the currently active profile |
| `/status` | Show session status (path, agent, profile, message count) |
| `/multiline` | Enter multi-line input mode (two empty lines to finish) |

### How It Works

1. PGA loads configuration from `~/.powergentic/config.{json,yaml,yml}` (with optional local override merging)
2. Discovers agents from the project directory (`AGENTS.md`, `agents/*.agent.md`, etc.)
3. Builds a system prompt from global + agent-specific instructions
4. Creates a chat session with the resolved LLM profile
5. Enters a interactive chat loop: your input → LLM → tool calls (if any) → response
6. Tool calls are automatically executed (with safety approvals as configured)
7. The conversation history is maintained for the duration of the session

---

## `pga explain`

Ask the AI agent to explain a command, error message, code snippet, or concept. This is a single-shot command — it sends one prompt and exits.

```bash
pga explain <text> [options]
```

### Arguments

| Argument | Required | Description |
|---|---|---|
| `text` | Yes | The command, code, or concept to explain |

### Options

| Option | Alias | Type | Default | Description |
|---|---|---|---|---|
| `--path` | `-p` | `string` | Current directory | Project root directory |
| `--profile` | | `string` | *(default profile)* | LLM profile to use |

### Examples

```bash
# Explain a git command
pga explain "git rebase -i HEAD~3"

# Explain a shell pipeline
pga explain "find . -name '*.log' -mtime +30 -delete"

# Explain an error message
pga explain "ECONNREFUSED 127.0.0.1:5432"

# Explain a code concept
pga explain "What is dependency injection?"

# Use a specific LLM profile
pga explain "async/await in C#" --profile gpt4
```

### Output

The agent provides a structured explanation including:
- What it does
- How it works
- Important caveats or notes

---

## `pga suggest`

Ask the AI agent to suggest a shell command for a given task. This is a single-shot command.

```bash
pga suggest <text> [options]
```

### Arguments

| Argument | Required | Description |
|---|---|---|
| `text` | Yes | What you want to accomplish |

### Options

| Option | Alias | Type | Default | Description |
|---|---|---|---|---|
| `--path` | `-p` | `string` | Current directory | Project root directory |
| `--profile` | | `string` | *(default profile)* | LLM profile to use |
| `--shell` | `-s` | `string` | *(auto-detected)* | Target shell (`bash`, `zsh`, `powershell`, `cmd`) |

### Shell Auto-Detection

PGA automatically detects your current shell:
- **macOS/Linux**: Reads the `SHELL` environment variable (defaults to `zsh`)
- **Windows**: Checks `COMSPEC` for PowerShell, falls back to `cmd`

### Examples

```bash
# Find large files
pga suggest "find all files larger than 100MB"

# Docker operations
pga suggest "remove all stopped Docker containers and unused images"

# Git operations
pga suggest "squash the last 5 commits into one"

# Specify target shell
pga suggest "list all running services" --shell powershell

# Networking
pga suggest "check which process is using port 3000"
```

### Output

The agent provides:
1. The exact command(s) to run
2. A brief explanation of what each command does
3. Any warnings or prerequisites

---

## `pga config`

Manage PGA configuration — LLM profiles, settings, and validation.

```bash
pga config <subcommand>
```

### `pga config init`

Create the configuration file at `~/.powergentic/config.json` with default values. PGA also supports `config.yaml` and `config.yml` formats — you can rename or convert the file after initialization.

```bash
pga config init
```

If the configuration already exists, this is a no-op.

### `pga config show`

Display the current configuration in a tree view. API keys are masked for security.

```bash
pga config show
```

### `pga config add-profile <name>`

Add a new LLM profile interactively. Launches a wizard that prompts for provider, endpoint, credentials, etc.

```bash
pga config add-profile my-azure
pga config add-profile local-ollama
```

The wizard flow:
1. Select provider: `azure-openai`, `azure-ai-foundry`, or `ollama`
2. Enter provider-specific settings (endpoint, deployment, API key, model, etc.)
3. Set a display name
4. Optionally set as the default profile

### `pga config remove-profile <name>`

Remove an LLM profile by name.

```bash
pga config remove-profile old-profile
```

If the removed profile was the default, PGA automatically promotes another profile to default.

### `pga config list-profiles`

Display a table of all configured LLM profiles.

```bash
pga config list-profiles
```

Output example:

```
╭──────────┬──────────────┬─────────────────────────────────┬─────────╮
│ Name     │ Provider     │ Details                         │ Default │
├──────────┼──────────────┼─────────────────────────────────┼─────────┤
│ azure    │ azure-openai │ gpt-4o @ https://my.openai.az.. │ ✓       │
│ local    │ ollama       │ llama3 @ http://localhost:11434  │         │
╰──────────┴──────────────┴─────────────────────────────────┴─────────╯
```

### `pga config set-default <name>`

Set the default LLM profile.

```bash
pga config set-default local
```

### `pga config validate`

Check the configuration for errors and report them.

```bash
pga config validate
```

Validation checks:
- At least one profile is configured
- The default profile exists
- Each profile has required fields for its provider (endpoint, deployment, API key, model, etc.)

---

## `pga init`

Initialize a project directory with agent configuration files.

```bash
pga init [options]
```

### Options

| Option | Alias | Type | Default | Description |
|---|---|---|---|---|
| `--path` | `-p` | `string` | Current directory | Project directory to initialize |

### What It Creates

| File/Directory | Purpose |
|---|---|
| `AGENTS.md` | Global AI instructions for the project |
| `agents/` | Directory for custom agent definitions |
| `agents/code-reviewer.agent.md` | Example agent — a code review specialist |

### Examples

```bash
# Initialize current directory
pga init

# Initialize a specific project
pga init --path ~/projects/my-app
```

If `AGENTS.md` or `agents/` already exists, they are skipped (no overwriting).

---

## Global Options

All commands support these built-in options:

| Option | Description |
|---|---|
| `-h`, `--help` | Show help and usage information |
| `--version` | Show version information |

```bash
pga --help
pga chat --help
pga config --help
```

## Exit Codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | Error (configuration invalid, LLM call failed, etc.) |
