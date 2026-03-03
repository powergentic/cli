using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Registry of all available agent tools with filtering and lookup capabilities.
/// </summary>
public sealed class ToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a tool.
    /// </summary>
    public void Register(IAgentTool tool) => _tools[tool.Name] = tool;

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    public IAgentTool? Get(string name) =>
        _tools.TryGetValue(name, out var tool) ? tool : null;

    /// <summary>
    /// Returns all registered tools.
    /// </summary>
    public IReadOnlyList<IAgentTool> GetAll() => _tools.Values.ToList();

    /// <summary>
    /// Returns all tools as AIFunction instances for the LLM.
    /// </summary>
    public IList<AITool> GetAITools() =>
        _tools.Values.Select(t => (AITool)t.ToAIFunction()).ToList();

    /// <summary>
    /// Returns filtered tools as AIFunction instances.
    /// If allowedTools is empty, returns all tools (minus disabled).
    /// </summary>
    public IList<AITool> GetAITools(IEnumerable<string>? allowedTools, IEnumerable<string>? disabledTools = null)
    {
        var allowed = allowedTools?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var disabled = disabledTools?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        return _tools.Values
            .Where(t => !disabled.Contains(t.Name))
            .Where(t => allowed == null || allowed.Count == 0 || allowed.Contains(t.Name))
            .Select(t => (AITool)t.ToAIFunction())
            .ToList();
    }

    /// <summary>
    /// Creates and populates a registry with all built-in tools.
    /// </summary>
    public static ToolRegistry CreateDefault(string workingDirectory)
    {
        var registry = new ToolRegistry();
        registry.Register(new ShellExecuteTool(workingDirectory));
        registry.Register(new FileReadTool());
        registry.Register(new FileWriteTool());
        registry.Register(new FileEditTool());
        registry.Register(new FileSearchTool());
        registry.Register(new GrepSearchTool());
        registry.Register(new DirectoryListTool());
        registry.Register(new GitOperationsTool(workingDirectory));
        registry.Register(new WebFetchTool());
        return registry;
    }
}
