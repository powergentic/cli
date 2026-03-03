using Pga.Core.Tools;

namespace Pga.Tests.Tools;

public class ToolRegistryTests
{
    [Fact]
    public void CreateDefault_RegistersAllBuiltInTools()
    {
        var tempDir = Path.GetTempPath();
        var registry = ToolRegistry.CreateDefault(tempDir);
        var tools = registry.GetAll();

        Assert.True(tools.Count >= 9, $"Expected at least 9 built-in tools, got {tools.Count}");

        var toolNames = tools.Select(t => t.Name).ToHashSet();
        Assert.Contains("shell_execute", toolNames);
        Assert.Contains("file_read", toolNames);
        Assert.Contains("file_write", toolNames);
        Assert.Contains("file_edit", toolNames);
        Assert.Contains("file_search", toolNames);
        Assert.Contains("grep_search", toolNames);
        Assert.Contains("directory_list", toolNames);
        Assert.Contains("git_operations", toolNames);
        Assert.Contains("web_fetch", toolNames);
    }

    [Fact]
    public void GetAITools_ReturnsAllTools()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var aiTools = registry.GetAITools();

        Assert.True(aiTools.Count >= 9);
    }

    [Fact]
    public void GetAITools_WithAllowedFilter_ReturnsOnlyAllowed()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var allowed = new[] { "file_read", "file_write" };
        var aiTools = registry.GetAITools(allowed);

        Assert.Equal(2, aiTools.Count);
    }

    [Fact]
    public void GetAITools_WithDisabledFilter_ExcludesDisabled()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var disabled = new[] { "shell_execute" };
        var aiTools = registry.GetAITools(null, disabled);

        Assert.DoesNotContain(aiTools, t => t is Microsoft.Extensions.AI.AIFunction f && f.Name == "shell_execute");
    }

    [Fact]
    public void Get_ExistingTool_ReturnsTool()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tool = registry.Get("file_read");

        Assert.NotNull(tool);
        Assert.Equal("file_read", tool!.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public void Get_NonExistentTool_ReturnsNull()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tool = registry.Get("nonexistent_tool");

        Assert.Null(tool);
    }

    [Fact]
    public void ToolSafetyLevels_AreCorrect()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());

        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("file_read")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("grep_search")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("directory_list")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("git_operations")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("web_fetch")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.ReadOnly, registry.Get("file_search")!.SafetyLevel);

        Assert.Equal(ToolSafetyLevel.Write, registry.Get("file_write")!.SafetyLevel);
        Assert.Equal(ToolSafetyLevel.Write, registry.Get("file_edit")!.SafetyLevel);

        Assert.Equal(ToolSafetyLevel.Execute, registry.Get("shell_execute")!.SafetyLevel);
    }
}
