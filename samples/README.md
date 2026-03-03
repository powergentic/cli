# Sample Agent Configurations

Ready-to-use agent configurations for common project types. Copy the files from any sample folder into your project root to get started immediately.

## Usage

```bash
# Option 1: Copy an entire sample into your project
cp -r samples/aspnet-mvc/* /path/to/your/project/

# Option 2: Copy just the AGENTS.md
cp samples/react-nodejs/AGENTS.md /path/to/your/project/

# Option 3: Use 'pga init' first, then replace with a sample
cd /path/to/your/project
pga init
cp samples/python/AGENTS.md .
cp samples/python/agents/* agents/
```

## Available Samples

| Sample | Directory | Description |
|---|---|---|
| **ASP.NET MVC** | [`aspnet-mvc/`](aspnet-mvc/) | C# ASP.NET Core MVC with Entity Framework, xUnit |
| **React + Node.js** | [`react-nodejs/`](react-nodejs/) | React frontend with Node.js/Express backend |
| **Python** | [`python/`](python/) | Python project with FastAPI, pytest, type hints |

## What's Included

Each sample contains:

```
sample-name/
├── AGENTS.md                          # Global project instructions
└── agents/
    ├── code-reviewer.agent.md         # Code review specialist
    ├── test-writer.agent.md           # Test generation specialist
    └── <project-specific>.agent.md    # Domain-specific agents
```

## Customizing

After copying the files into your project:

1. **Edit `AGENTS.md`** — Update the project description, tech stack, and coding standards to match your actual project
2. **Edit agents** — Adjust instructions, add/remove tools, set LLM profiles
3. **Add your own agents** — Create new `*.agent.md` files in the `agents/` directory

## Tips

- **Start with `AGENTS.md` only** — You don't need custom agents to get value. A well-written `AGENTS.md` dramatically improves AI assistance quality.
- **Be specific** — The more detail you provide about your project's conventions, the better the AI will follow them.
- **Iterate** — Start with a sample, run `pga chat`, and refine the instructions based on what the AI gets right or wrong.
- **Use tool restrictions** — For agents that should only read (not modify) code, use the `tools` or `disabledTools` frontmatter fields.
