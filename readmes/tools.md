# Built-in Tools

PGA provides 9 built-in tools that the AI agent can invoke during a conversation. These tools give the agent the ability to read files, search code, execute commands, and more — similar to GitHub Copilot in VS Code.

## Tool Safety Levels

Each tool is assigned a safety level that determines whether user approval is required:

| Level | Description | Approval Behavior |
|---|---|---|
| **ReadOnly** | Does not modify files or system state | Auto-approved (unless `prompt-always` mode) |
| **Write** | Modifies files | Requires approval in `prompt-writes` and `prompt-always` modes |
| **Execute** | Runs arbitrary commands | Requires approval in `prompt-writes` and `prompt-always` modes |

See [Configuration — Tool Safety](configuration.md#tool-safety) to configure approval behavior.

---

## `shell_execute`

Execute a shell command and return its output.

| Property | Value |
|---|---|
| **Safety Level** | Execute |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `command` | `string` | Yes | The shell command to execute |
| `workingDirectory` | `string` | No | Working directory. Defaults to the project root |

### Behavior

- Uses `zsh` on macOS/Linux, `cmd.exe` on Windows
- Captures both stdout and stderr
- Returns exit code + combined output
- Output is truncated at 50,000 characters

### Example Invocations

The AI might call this tool to:
- Run build commands: `dotnet build`
- Run tests: `npm test`
- Check system state: `docker ps`
- Install dependencies: `pip install -r requirements.txt`

---

## `file_read`

Read the contents of a file, optionally specifying a line range.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `path` | `string` | Yes | Absolute path to the file |
| `startLine` | `int` | No | Starting line number (1-based) |
| `endLine` | `int` | No | Ending line number (1-based, inclusive) |

### Behavior

- Returns lines prefixed with line numbers (e.g., `42: let x = 10;`)
- If `startLine`/`endLine` are omitted, reads the entire file
- Output is truncated at 100,000 characters

---

## `file_write`

Create a new file or overwrite an existing file.

| Property | Value |
|---|---|
| **Safety Level** | Write |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `path` | `string` | Yes | Absolute path to the file |
| `content` | `string` | Yes | Content to write |

### Behavior

- Automatically creates parent directories if they don't exist
- Overwrites the file if it already exists
- Returns a confirmation with the character count

---

## `file_edit`

Edit an existing file by replacing a specific string with new content.

| Property | Value |
|---|---|
| **Safety Level** | Write |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `path` | `string` | Yes | Absolute path to the file |
| `oldString` | `string` | Yes | Exact text to find and replace (must match exactly, including whitespace) |
| `newString` | `string` | Yes | Replacement text |

### Behavior

- Performs an exact, case-sensitive string match
- **Rejects the edit** if `oldString` is not found in the file
- **Rejects the edit** if `oldString` matches multiple locations (ambiguity guard)
- Replaces exactly one occurrence per call
- The agent may make multiple `file_edit` calls to change different parts of a file

---

## `file_search`

Search for files matching a glob pattern.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `directory` | `string` | Yes | Root directory to search from |
| `pattern` | `string` | Yes | Glob pattern (e.g., `**/*.cs`, `src/**/*.ts`) |
| `maxResults` | `int` | No | Maximum results to return. Default: 50 |

### Behavior

- Uses standard glob matching
- Automatically excludes: `node_modules`, `bin`, `obj`, `.git`, `dist`, `__pycache__`
- Returns relative paths from the search directory

### Glob Pattern Examples

| Pattern | Matches |
|---|---|
| `**/*.cs` | All C# files recursively |
| `src/**/*.ts` | All TypeScript files under `src/` |
| `*.json` | JSON files in the root directory only |
| `**/test*` | All files/dirs starting with "test" |

---

## `grep_search`

Search file contents with text or regex patterns.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `directory` | `string` | Yes | Directory to search in |
| `pattern` | `string` | Yes | Search pattern (plain text or regex) |
| `isRegex` | `bool` | No | Whether `pattern` is a regular expression. Default: `false` |
| `filePattern` | `string` | No | Glob to filter which files to search (e.g., `*.cs`) |
| `maxResults` | `int` | No | Maximum matches to return. Default: 50 |

### Behavior

- Case-insensitive matching for plain text
- Compiled regex for regex patterns
- Skips common non-project directories (`node_modules`, `bin`, `obj`, `.git`, etc.)
- Returns results as `file:line: matching text`

### Example Patterns

| Pattern | `isRegex` | Matches |
|---|---|---|
| `TODO` | `false` | Lines containing "TODO" (case-insensitive) |
| `console\.log` | `true` | Lines with `console.log` |
| `public\s+class\s+\w+` | `true` | C# class declarations |
| `import.*from` | `true` | JavaScript/TypeScript imports |

---

## `directory_list`

List the contents of a directory.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `path` | `string` | Yes | Absolute path to the directory |
| `showHidden` | `bool` | No | Include hidden files/directories. Default: `false` |
| `maxDepth` | `int` | No | Recursion depth. `1` = immediate children only. Default: `1` |

### Behavior

- Directories are suffixed with `/`
- Automatically excludes: `node_modules`, `bin`, `obj`, `.git`
- Maximum 500 entries returned

### Output Format

```
src/
tests/
README.md
package.json
tsconfig.json
```

---

## `git_operations`

Perform read-only git operations.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `operation` | `string` | Yes | Git operation to perform (see list below) |
| `args` | `string` | No | Additional arguments for the git command |

### Allowed Operations

| Operation | Git Command | Description |
|---|---|---|
| `status` | `git status` | Working tree status |
| `log` | `git log` | Commit history |
| `diff` | `git diff` | Show changes |
| `show` | `git show` | Show commit details |
| `branch` | `git branch` | List branches |
| `blame` | `git blame` | Line-by-line authorship |
| `remote` | `git remote` | List remotes |
| `stash-list` | `git stash list` | List stashes |
| `rev-parse` | `git rev-parse` | Parse revisions |
| `describe` | `git describe` | Describe commit |
| `shortlog` | `git shortlog` | Summarize log |
| `tag` | `git tag` | List tags |

### Behavior

- **Only read-only operations** are allowed. Mutating git commands (`commit`, `push`, `merge`, `checkout`, etc.) are blocked
- Output is truncated at 50,000 characters
- Sets `GIT_TERMINAL_PROMPT=0` to prevent interactive prompts

### Example Invocations

```
operation: "log", args: "--oneline -20"
operation: "diff", args: "HEAD~1"
operation: "blame", args: "src/main.ts"
operation: "branch", args: "-a"
```

---

## `web_fetch`

Fetch the text content of a web page.

| Property | Value |
|---|---|
| **Safety Level** | ReadOnly |

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `url` | `string` | Yes | URL to fetch (must be `http://` or `https://`) |
| `query` | `string` | No | Optional query to help focus on relevant content |

### Behavior

- Only `http` and `https` URLs are accepted
- 30-second timeout
- For HTML responses:
  - Strips `<script>` and `<style>` blocks
  - Removes all HTML tags
  - Normalizes whitespace
- Output is truncated at 50,000 characters

---

## Tool Invocation Flow

When the AI decides to use a tool during a conversation:

1. **LLM requests tool call** — The model returns a function call request with tool name and parameters
2. **Safety check** — PGA checks the tool's safety level against your configured mode
3. **Approval prompt** (if needed) — You see the tool name and action, and choose to approve or deny
4. **Execution** — The tool runs and returns its result
5. **Result sent to LLM** — The tool output is added to the conversation and the LLM generates its next response
6. **Iteration** — The LLM may call additional tools (up to 25 tool-call iterations per message)

### Tool Call Display

When `showToolCalls` is enabled in your config (default), you'll see:

```
🔧 file_read: Reading /path/to/file.cs (lines 1-50)
✓ Tool result: 50 lines read

🔧 grep_search: Searching for "TODO" in /path/to/project
✓ Tool result: 7 matches found
```

### Maximum Iterations

PGA allows up to **25 tool call iterations** per user message to prevent infinite loops. If the limit is reached, the conversation continues with whatever context has been gathered.
