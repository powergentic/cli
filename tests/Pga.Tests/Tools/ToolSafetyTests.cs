using Pga.Core.Configuration;
using Pga.Core.Tools;

namespace Pga.Tests.Tools;

public class ToolSafetyTests
{
    private static IAgentTool CreateMockTool(string name, ToolSafetyLevel level)
    {
        // Use the real registry to get a tool with the desired safety level
        var registry = ToolRegistry.CreateDefault(Path.GetTempPath());
        return level switch
        {
            ToolSafetyLevel.ReadOnly => registry.Get("file_read")!,
            ToolSafetyLevel.Write => registry.Get("file_write")!,
            ToolSafetyLevel.Execute => registry.Get("shell_execute")!,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [Fact]
    public async Task CheckApproval_AutoApprove_AlwaysReturnsTrue()
    {
        var config = new ToolSafetyConfig { Mode = "auto-approve" };
        var safety = new ToolSafety(config);

        var readTool = CreateMockTool("file_read", ToolSafetyLevel.ReadOnly);
        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);
        var execTool = CreateMockTool("shell_execute", ToolSafetyLevel.Execute);

        Assert.True(await safety.CheckApproval(readTool, "reading file"));
        Assert.True(await safety.CheckApproval(writeTool, "writing file"));
        Assert.True(await safety.CheckApproval(execTool, "executing command"));
    }

    [Fact]
    public async Task CheckApproval_PromptWrites_AutoApprovesReadOnly()
    {
        var callbackCalled = false;
        var config = new ToolSafetyConfig { Mode = "prompt-writes" };
        var safety = new ToolSafety(config, (name, desc) =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        var readTool = CreateMockTool("file_read", ToolSafetyLevel.ReadOnly);

        var result = await safety.CheckApproval(readTool, "reading file");

        Assert.True(result);
        Assert.False(callbackCalled); // Should NOT have prompted
    }

    [Fact]
    public async Task CheckApproval_PromptWrites_PromptsForWriteTool()
    {
        var callbackCalled = false;
        var config = new ToolSafetyConfig { Mode = "prompt-writes" };
        var safety = new ToolSafety(config, (name, desc) =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);

        var result = await safety.CheckApproval(writeTool, "writing file");

        Assert.True(result);
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task CheckApproval_PromptWrites_PromptsForExecuteTool()
    {
        var callbackCalled = false;
        var config = new ToolSafetyConfig { Mode = "prompt-writes" };
        var safety = new ToolSafety(config, (name, desc) =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        var execTool = CreateMockTool("shell_execute", ToolSafetyLevel.Execute);

        var result = await safety.CheckApproval(execTool, "running command");

        Assert.True(result);
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task CheckApproval_PromptAlways_PromptsForReadOnly()
    {
        var callbackCalled = false;
        var config = new ToolSafetyConfig { Mode = "prompt-always" };
        var safety = new ToolSafety(config, (name, desc) =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        var readTool = CreateMockTool("file_read", ToolSafetyLevel.ReadOnly);

        var result = await safety.CheckApproval(readTool, "reading file");

        Assert.True(result);
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task CheckApproval_PromptAlways_PromptsForWriteTool()
    {
        var callbackCalled = false;
        var config = new ToolSafetyConfig { Mode = "prompt-always" };
        var safety = new ToolSafety(config, (name, desc) =>
        {
            callbackCalled = true;
            return Task.FromResult(true);
        });

        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);

        var result = await safety.CheckApproval(writeTool, "writing file");

        Assert.True(result);
        Assert.True(callbackCalled);
    }

    [Fact]
    public async Task CheckApproval_CallbackDenies_ReturnsFalse()
    {
        var config = new ToolSafetyConfig { Mode = "prompt-writes" };
        var safety = new ToolSafety(config, (name, desc) => Task.FromResult(false));

        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);

        var result = await safety.CheckApproval(writeTool, "writing file");

        Assert.False(result);
    }

    [Fact]
    public async Task CheckApproval_NoCallback_AutoApproves()
    {
        var config = new ToolSafetyConfig { Mode = "prompt-writes" };
        var safety = new ToolSafety(config); // No callback provided

        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);

        // Should auto-approve when no callback is set (fallback behavior)
        var result = await safety.CheckApproval(writeTool, "writing file");

        Assert.True(result);
    }

    [Fact]
    public async Task CheckApproval_DefaultMode_TreatedAsPromptWrites()
    {
        // Unknown/default mode should behave like prompt-writes
        var config = new ToolSafetyConfig { Mode = "some-unknown-mode" };
        var callbackCalledForRead = false;
        var callbackCalledForWrite = false;

        var safety1 = new ToolSafety(config, (name, desc) =>
        {
            callbackCalledForRead = true;
            return Task.FromResult(true);
        });

        var readTool = CreateMockTool("file_read", ToolSafetyLevel.ReadOnly);
        await safety1.CheckApproval(readTool, "reading file");
        Assert.False(callbackCalledForRead); // ReadOnly should be auto-approved

        var safety2 = new ToolSafety(config, (name, desc) =>
        {
            callbackCalledForWrite = true;
            return Task.FromResult(true);
        });

        var writeTool = CreateMockTool("file_write", ToolSafetyLevel.Write);
        await safety2.CheckApproval(writeTool, "writing file");
        Assert.True(callbackCalledForWrite); // Write should prompt
    }
}
