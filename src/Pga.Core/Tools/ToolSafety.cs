using Pga.Core.Configuration;

namespace Pga.Core.Tools;

/// <summary>
/// Handles tool execution safety by prompting for user approval based on safety level configuration.
/// </summary>
public sealed class ToolSafety
{
    private readonly ToolSafetyConfig _config;
    private readonly Func<string, string, Task<bool>>? _approvalCallback;

    public ToolSafety(ToolSafetyConfig config, Func<string, string, Task<bool>>? approvalCallback = null)
    {
        _config = config;
        _approvalCallback = approvalCallback;
    }

    /// <summary>
    /// Checks whether a tool invocation should proceed, potentially asking for user approval.
    /// Returns true if the tool is approved for execution.
    /// </summary>
    public async Task<bool> CheckApproval(IAgentTool tool, string description)
    {
        var safetyLevel = tool.SafetyLevel;

        switch (_config.Mode.ToLowerInvariant())
        {
            case "auto-approve":
                return true;

            case "prompt-always":
                return await PromptUser(tool.Name, description);

            case "prompt-writes":
            default:
                if (safetyLevel == ToolSafetyLevel.ReadOnly)
                    return true;
                return await PromptUser(tool.Name, description);
        }
    }

    private async Task<bool> PromptUser(string toolName, string description)
    {
        if (_approvalCallback != null)
            return await _approvalCallback(toolName, description);

        // Fallback: auto-approve if no callback is set
        return true;
    }
}
