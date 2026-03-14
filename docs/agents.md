# Agent System

PGA uses a file-based agent system modeled after the GitHub Copilot agent convention. Agents are markdown files that define custom instructions, tool access, and LLM profile preferences for the AI assistant.

## Overview

There are two types of agent files:

| File | Scope | Purpose |
|---|---|---|
| `AGENTS.md` | Global | Project-wide instructions that apply to every conversation |
| `*.agent.md` | Named agent | A specialized agent with its own instructions and tool set |

## AGENTS.md — Global Instructions

Place an `AGENTS.md` file at the root of your project to provide global instructions that the AI always follows when working in that directory.

```
my-project/
├── AGENTS.md          ← Global instructions
├── src/
├── tests/
└── package.json
```

### Format

`AGENTS.md` is a plain markdown file. No frontmatter is required (though it's supported).

```markdown
# Project Instructions for AI Agent

You are an AI assistant working on the MyApp project.

## Technology Stack
- Backend: .NET 10 / C# / ASP.NET Core
- Frontend: React with TypeScript
- Database: PostgreSQL
- Testing: xUnit, Jest

## Coding Standards
- Use meaningful variable and function names
- All public methods must have XML documentation comments
- Follow SOLID principles
- Maximum cyclomatic complexity: 10

## Project Structure
- `src/Api/` — REST API controllers and middleware
- `src/Core/` — Domain models and business logic
- `src/Data/` — Entity Framework repositories
- `tests/` — Unit and integration tests

## Important Rules
- Never modify migration files directly
- Always add unit tests for new business logic
- Use the repository pattern for data access
```

### How It Works

When you start a chat session (`pga chat`) from a project directory, PGA:

1. Looks for `AGENTS.md` at the project root
2. Reads its content and includes it in the system prompt sent to the LLM
3. The AI follows these instructions throughout the conversation

---

## *.agent.md — Named Agents

Named agents are markdown files with YAML frontmatter that define specialized AI behaviors. They live in the `agents/` directory.

```
my-project/
├── AGENTS.md
├── agents/
│   ├── code-reviewer.agent.md
│   ├── test-writer.agent.md
│   └── docs-writer.agent.md
└── src/
```

### File Format

```markdown
---
name: code-reviewer
description: Reviews code for quality, best practices, and potential issues
profile: gpt4
tools:
  - file_read
  - grep_search
  - directory_list
  - git_operations
disabledTools:
  - shell_execute
  - file_write
---

# Code Reviewer Agent

You are an expert code reviewer. When reviewing code:

1. **Check for bugs** — Look for potential null references, off-by-one errors, race conditions
2. **Assess code quality** — Evaluate naming, structure, and SOLID principles
3. **Review security** — Identify potential security vulnerabilities
4. **Suggest improvements** — Offer specific, actionable suggestions with code examples
5. **Check tests** — Verify that tests adequately cover the changes

Be constructive and explain the *why* behind each suggestion.
```

### Frontmatter Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | No | Agent name. Defaults to filename (e.g., `code-reviewer` from `code-reviewer.agent.md`) |
| `description` | `string` | No | Short description shown when listing agents |
| `profile` | `string` | No | Preferred LLM profile name. Can be overridden by `--profile` |
| `tools` | `string[]` | No | Whitelist of tools this agent can use. If empty, all tools are available |
| `disabledTools` | `string[]` | No | Tools explicitly blocked for this agent |
| `metadata` | `object` | No | Arbitrary key-value metadata |

### Naming Convention

The agent name is derived from the filename by removing the `.agent.md` extension:

| Filename | Agent Name |
|---|---|
| `code-reviewer.agent.md` | `code-reviewer` |
| `test-writer.agent.md` | `test-writer` |
| `docs-writer.agent.md` | `docs-writer` |

If the frontmatter includes a `name` field, it overrides the filename-derived name.

---

## Agent Discovery

PGA searches for agents in these locations (in order):

| Location | Scope |
|---|---|
| `AGENTS.md` | Global instructions (project root) |
| `.powergentic/agents/*.agent.md` | Root-level named agents |
| `.github/agents/*.agent.md` | Alternative location (GitHub convention) |
| `<subdir>/agents/*.agent.md` | Scoped agents (apply only within `<subdir>`) |

### Skipped Directories

During agent discovery, the following directories are automatically skipped:

- `node_modules`
- `bin`, `obj`
- `dist`, `build`
- `.git`
- `__pycache__`
- `vendor`, `packages`
- Any directory starting with `.` (except `.github`)

---

## Scoped Agents

Agents can be scoped to specific directories within a project. This is useful in monorepos where different parts of the codebase have different conventions.

### Example: Monorepo with Scoped Agents

```
my-monorepo/
├── AGENTS.md                              ← Global instructions
├── agents/
│   └── general-assistant.agent.md         ← Available everywhere
├── frontend/
│   ├── agents/
│   │   └── react-expert.agent.md          ← Only for frontend/
│   ├── src/
│   └── package.json
├── backend/
│   ├── agents/
│   │   └── dotnet-expert.agent.md         ← Only for backend/
│   ├── src/
│   └── Backend.csproj
└── infrastructure/
    ├── agents/
    │   └── terraform-expert.agent.md      ← Only for infrastructure/
    └── main.tf
```

### How Scoping Works

When PGA resolves agents for a given working path:

1. **Global agent** (`AGENTS.md`) always applies
2. **Root-level agents** (`.powergentic/agents/*.agent.md`, `.github/agents/*.agent.md`) always apply
3. **Scoped agents** only apply if the working path is within their scope directory

For example, when working in `frontend/src/`:
- ✅ `AGENTS.md` — applies (global)
- ✅ `agents/general-assistant.agent.md` — applies (root-level)
- ✅ `frontend/agents/react-expert.agent.md` — applies (scope matches)
- ❌ `backend/agents/dotnet-expert.agent.md` — does NOT apply (different scope)
- ❌ `infrastructure/agents/terraform-expert.agent.md` — does NOT apply (different scope)

---

## Using Agents

### List Available Agents

From the CLI:

```bash
# Inside a chat session
/agents
```

### Select an Agent at Startup

```bash
pga chat --agent code-reviewer
```

### Switch Agents Mid-Session

```bash
# Inside a chat session
/agent code-reviewer
```

### Check Current Agent

```bash
# Inside a chat session
/agent
# Output: Current agent: code-reviewer
```

---

## Tool Filtering

Agents can control which tools the AI has access to.

### Whitelist (Allow Specific Tools)

If `tools` is specified in the frontmatter, only those tools are available:

```yaml
---
tools:
  - file_read
  - grep_search
  - directory_list
---
```

### Blacklist (Disable Specific Tools)

Use `disabledTools` to block specific tools while keeping all others:

```yaml
---
disabledTools:
  - shell_execute
  - file_write
  - file_edit
---
```

### Available Tool Names

| Tool | Description |
|---|---|
| `shell_execute` | Execute shell commands |
| `file_read` | Read file contents |
| `file_write` | Create or overwrite files |
| `file_edit` | Edit files via search-and-replace |
| `file_search` | Search for files by glob pattern |
| `grep_search` | Search file contents with text/regex |
| `directory_list` | List directory contents |
| `git_operations` | Read-only git operations |
| `web_fetch` | Fetch web page content |

See [Tools](tools.md) for full details on each tool.

---

## System Prompt Construction

When a chat session starts, PGA builds the system prompt in this order:

1. **Base system prompt** — Default PGA instructions (tool usage guidelines, safety rules)
2. **Global instructions** — Content from `AGENTS.md`
3. **Agent instructions** — Content from the selected `*.agent.md` (if an agent is specified)
4. **Scoped agent instructions** — Content from any scoped agents matching the working path

This layered approach means agents **extend** the global instructions rather than replacing them.

---

## Scaffolding

Use `pga init` to quickly create the agent file structure:

```bash
cd ~/my-project
pga init
```

This creates:
- `AGENTS.md` with a template you can customize
- `agents/code-reviewer.agent.md` as an example agent

---

## Example Agents

### Test Writer

```markdown
---
name: test-writer
description: Writes comprehensive unit tests
profile: gpt4
tools:
  - file_read
  - file_write
  - file_search
  - grep_search
  - directory_list
---

# Test Writer Agent

You are an expert at writing unit tests. When asked to write tests:

1. Read the source file to understand the code
2. Identify all public methods and edge cases
3. Write comprehensive tests covering:
   - Happy path scenarios
   - Error/exception cases
   - Boundary conditions
   - Null/empty input handling
4. Follow the existing test conventions in the project
5. Use appropriate assertion methods
6. Write descriptive test names using the pattern: `MethodName_Scenario_ExpectedResult`
```

### Documentation Writer

```markdown
---
name: docs-writer
description: Writes and updates project documentation
tools:
  - file_read
  - file_write
  - file_search
  - directory_list
  - grep_search
disabledTools:
  - shell_execute
---

# Documentation Writer Agent

You are a technical documentation specialist. When writing docs:

1. Use clear, concise language
2. Include code examples where appropriate
3. Follow the existing documentation style in the project
4. Add links to related documentation
5. Include a table of contents for long documents
6. Use proper markdown formatting
```

### Security Auditor

```markdown
---
name: security-auditor
description: Audits code for security vulnerabilities
tools:
  - file_read
  - grep_search
  - file_search
  - directory_list
  - git_operations
disabledTools:
  - shell_execute
  - file_write
  - file_edit
---

# Security Auditor Agent

You are a security expert. When auditing code:

1. Check for common vulnerabilities (OWASP Top 10)
2. Look for hardcoded secrets, credentials, and API keys
3. Identify SQL injection, XSS, and CSRF risks
4. Review authentication and authorization logic
5. Check dependency versions for known CVEs
6. Assess input validation and sanitization
7. Report findings with severity levels (Critical/High/Medium/Low)
```
