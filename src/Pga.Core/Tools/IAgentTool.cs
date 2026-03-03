using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Pga.Core.Tools;

/// <summary>
/// Defines the safety level of an agent tool.
/// </summary>
public enum ToolSafetyLevel
{
    /// <summary>Read-only operations that don't modify the filesystem or system state.</summary>
    ReadOnly,

    /// <summary>Operations that modify files or system state.</summary>
    Write,

    /// <summary>Operations that execute arbitrary commands.</summary>
    Execute
}

/// <summary>
/// Base interface for all PGA agent tools.
/// Each tool provides metadata and an AIFunction for the LLM to invoke.
/// </summary>
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    ToolSafetyLevel SafetyLevel { get; }
    AIFunction ToAIFunction();
}
