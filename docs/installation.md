# Installation

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

Verify your .NET installation:

```bash
dotnet --version
# Should print 10.0.x or later
```

## Building from Source

Clone the repository and build:

```bash
git clone https://github.com/powergentic/PowergenticAgent.git
cd PowergenticAgent
dotnet build
```

### Running in Development Mode

Use `dotnet run` from the CLI project directory:

```bash
dotnet run --project src/Pga.Cli -- chat
dotnet run --project src/Pga.Cli -- explain "git rebase -i"
dotnet run --project src/Pga.Cli -- suggest "find files larger than 100MB"
```

> **Note:** Arguments after `--` are passed to the PGA CLI; arguments before `--` are consumed by `dotnet run`.

## Publishing a Self-Contained Binary

PGA is configured for single-file, self-contained publishing. This produces a standalone `pga` binary that doesn't require the .NET SDK on the target machine.

### macOS (Apple Silicon)

```bash
dotnet publish src/Pga.Cli -c Release -r osx-arm64
```

The binary will be at:
```
src/Pga.Cli/bin/Release/net10.0/osx-arm64/publish/pga
```

### macOS (Intel)

```bash
dotnet publish src/Pga.Cli -c Release -r osx-x64
```

### Linux (x64)

```bash
dotnet publish src/Pga.Cli -c Release -r linux-x64
```

### Windows (x64)

```bash
dotnet publish src/Pga.Cli -c Release -r win-x64
```

The binary will be `pga.exe` on Windows.

## Installing the Binary

After publishing, copy the binary to a directory in your `PATH`:

### macOS / Linux

```bash
# Example: copy to /usr/local/bin
sudo cp src/Pga.Cli/bin/Release/net10.0/osx-arm64/publish/pga /usr/local/bin/pga
sudo chmod +x /usr/local/bin/pga
```

Or add the publish directory to your shell profile:

```bash
# In ~/.zshrc or ~/.bashrc
export PATH="$PATH:/path/to/PowergenticAgent/src/Pga.Cli/bin/Release/net10.0/osx-arm64/publish"
```

### Windows

Add the publish directory to your system `PATH` environment variable, or copy `pga.exe` to a directory already in your `PATH`.

## Verifying the Installation

```bash
pga --help
```

Expected output:

```
Description:
  PGA (Powergentic CLI) — AI-powered coding assistant CLI

Usage:
  pga [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  chat       Start an interactive chat session with the AI agent.
  explain    Ask the AI agent to explain a command, error, or concept.
  suggest    Ask the AI agent to suggest a shell command for a given task.
  config     Manage PGA configuration (LLM profiles, settings).
  init       Initialize a project with AGENTS.md and agents/ folder.
```

## Running Tests

```bash
dotnet test
```

## Publish Settings

The CLI project (`src/Pga.Cli/Pga.Cli.csproj`) includes these publish settings:

| Setting | Value | Purpose |
|---|---|---|
| `PublishSingleFile` | `true` | Produces a single executable file |
| `SelfContained` | `true` | Includes the .NET runtime — no SDK needed on target |
| `PublishTrimmed` | `true` | Trims unused code to reduce binary size |
| `AssemblyName` | `pga` | Output binary is named `pga` (not `Pga.Cli`) |

## Next Steps

- [Getting Started](getting-started.md) — First-time setup and your first chat
- [Configuration](configuration.md) — Setting up LLM providers
