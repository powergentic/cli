using Pga.Core.Tools;

namespace Pga.Tests.Tools;

public class ToolRegistryExtendedTests
{
    [Fact]
    public void Register_CustomTool_IsRetrievable()
    {
        var registry = new ToolRegistry();
        var tool = new FileReadTool();

        registry.Register(tool);

        var retrieved = registry.Get("file_read");
        Assert.NotNull(retrieved);
        Assert.Same(tool, retrieved);
    }

    [Fact]
    public void Register_DuplicateName_OverridesPrevious()
    {
        var registry = new ToolRegistry();
        var tool1 = new FileReadTool();
        var tool2 = new FileReadTool();

        registry.Register(tool1);
        registry.Register(tool2);

        var retrieved = registry.Get("file_read");
        Assert.Same(tool2, retrieved);
    }

    [Fact]
    public void Get_CaseInsensitive()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());

        Assert.NotNull(registry.Get("File_Read"));
        Assert.NotNull(registry.Get("FILE_READ"));
        Assert.NotNull(registry.Get("file_read"));
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var registry = new ToolRegistry();
        registry.Register(new FileReadTool());
        registry.Register(new FileWriteTool());

        var all = registry.GetAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetAITools_WithBothAllowedAndDisabled_AppliesBoth()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var allowed = new[] { "file_read", "file_write", "shell_execute" };
        var disabled = new[] { "shell_execute" };

        var tools = registry.GetAITools(allowed, disabled);

        Assert.Equal(2, tools.Count);
    }

    [Fact]
    public void GetAITools_EmptyAllowed_ReturnsAllMinusDisabled()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var disabled = new[] { "shell_execute", "file_write", "file_edit" };

        var tools = registry.GetAITools(new List<string>(), disabled);

        Assert.Equal(6, tools.Count); // 9 total - 3 disabled = 6
    }

    [Fact]
    public void GetAITools_NullAllowed_ReturnsAllMinusDisabled()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var disabled = new[] { "shell_execute" };

        var tools = registry.GetAITools(null, disabled);

        Assert.Equal(8, tools.Count); // 9 total - 1 disabled = 8
    }

    [Fact]
    public void GetAITools_NullBoth_ReturnsAll()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());

        var tools = registry.GetAITools(null, null);

        Assert.True(tools.Count >= 9);
    }

    [Fact]
    public void CreateDefault_AllToolsHaveDescriptions()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tools = registry.GetAll();

        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description),
                $"Tool '{tool.Name}' has no description.");
        }
    }

    [Fact]
    public void CreateDefault_AllToolsHaveNames()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tools = registry.GetAll();

        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Name),
                $"A tool has no name.");
        }
    }

    [Fact]
    public void CreateDefault_AllToolsCreateAIFunctions()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tools = registry.GetAll();

        foreach (var tool in tools)
        {
            var aiFunc = tool.ToAIFunction();
            Assert.NotNull(aiFunc);
            Assert.Equal(tool.Name, aiFunc.Name);
        }
    }

    [Fact]
    public void CreateDefault_ToolNamesAreUnique()
    {
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        var tools = registry.GetAll();
        var names = tools.Select(t => t.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
    }
}
