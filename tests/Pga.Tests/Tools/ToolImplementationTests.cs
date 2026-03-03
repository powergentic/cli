using Microsoft.Extensions.AI;
using Pga.Core.Tools;
using Xunit;

namespace Pga.Tests.Tools;

public class DirectoryListToolTests : IDisposable
{
    private readonly string _testDir;

    public DirectoryListToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_dirlist_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void DirectoryListTool_HasCorrectMetadata()
    {
        var tool = new DirectoryListTool();
        Assert.Equal("directory_list", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public async Task ListDirectory_ShowsFilesAndFolders()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, "subdir"));
        File.WriteAllText(Path.Combine(_testDir, "file.txt"), "content");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("subdir/", text);
        Assert.Contains("file.txt", text);
    }

    [Fact]
    public async Task ListDirectory_NonexistentPath_ReturnsError()
    {
        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = "/nonexistent/path"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task ListDirectory_EmptyDir_ReturnsEmptyMessage()
    {
        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("empty", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListDirectory_ExcludesHiddenByDefault()
    {
        File.WriteAllText(Path.Combine(_testDir, ".hidden"), "content");
        File.WriteAllText(Path.Combine(_testDir, "visible.txt"), "content");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("visible.txt", text);
        Assert.DoesNotContain(".hidden", text);
    }

    [Fact]
    public async Task ListDirectory_IncludesHiddenWhenRequested()
    {
        File.WriteAllText(Path.Combine(_testDir, ".hidden"), "content");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir,
            ["includeHidden"] = true
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains(".hidden", text);
    }

    [Fact]
    public async Task ListDirectory_Recursive_ShowsNestedContent()
    {
        var subDir = Path.Combine(_testDir, "child");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "content");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir,
            ["maxDepth"] = 2
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("child/", text);
        Assert.Contains("nested.txt", text);
    }

    [Fact]
    public async Task ListDirectory_ExcludesNodeModulesAndBin()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, "node_modules"));
        Directory.CreateDirectory(Path.Combine(_testDir, "bin"));
        Directory.CreateDirectory(Path.Combine(_testDir, "src"));

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("src/", text);
        Assert.DoesNotContain("node_modules", text);
        Assert.DoesNotContain("bin", text);
    }
}

public class GitOperationsToolTests
{
    [Fact]
    public void GitOperationsTool_HasCorrectMetadata()
    {
        var tool = new GitOperationsTool("/tmp");
        Assert.Equal("git_operations", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public async Task GitOperations_DisallowedOperation_ReturnsError()
    {
        var tool = new GitOperationsTool("/tmp");
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["operation"] = "push"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("not allowed", text);
    }

    [Fact]
    public async Task GitOperations_AllowedOperation_DoesNotReturnNotAllowedError()
    {
        // This runs in a temp dir that likely isn't a git repo, but it should not say "not allowed"
        var tool = new GitOperationsTool("/tmp");
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["operation"] = "status"
        }));

        var text = result?.ToString() ?? "";
        Assert.DoesNotContain("not allowed", text);
    }

    [Fact]
    public async Task GitOperations_StatusInGitRepo_Succeeds()
    {
        // Use this project's own git repo
        var repoDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(repoDir, ".git")))
            return; // skip if not running from git checkout

        var tool = new GitOperationsTool(repoDir);
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["operation"] = "status"
        }));

        var text = result?.ToString() ?? "";
        // Should return some git status output
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.DoesNotContain("Error executing git", text);
    }
}

public class ShellExecuteToolTests
{
    [Fact]
    public void ShellExecuteTool_HasCorrectMetadata()
    {
        var tool = new ShellExecuteTool("/tmp");
        Assert.Equal("shell_execute", tool.Name);
        Assert.Equal(ToolSafetyLevel.Execute, tool.SafetyLevel);
    }

    [Fact]
    public async Task ShellExecute_SimpleCommand_ReturnsOutput()
    {
        var tool = new ShellExecuteTool("/tmp");
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "echo hello"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Exit code: 0", text);
    }
}

public class WebFetchToolTests
{
    [Fact]
    public void WebFetchTool_HasCorrectMetadata()
    {
        var tool = new WebFetchTool();
        Assert.Equal("web_fetch", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public async Task WebFetch_InvalidUrl_ReturnsError()
    {
        var tool = new WebFetchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["url"] = "not-a-url"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }
}
