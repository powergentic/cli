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
    private static IShellExecuteProvider CreateProvider()
        => OperatingSystem.IsWindows()
            ? new WindowsShellExecuteProvider()
            : new LinuxShellExecuteProvider();

    [Fact]
    public void ShellExecuteTool_HasCorrectMetadata()
    {
        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        Assert.Equal("shell_execute", tool.Name);
        Assert.Equal(ToolSafetyLevel.Execute, tool.SafetyLevel);
    }

    [Fact]
    public async Task ShellExecute_SimpleCommand_ReturnsOutput()
    {
        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "echo hello"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Exit code: 0", text);
    }

    [Fact]
    public async Task ShellExecute_PathTraversal_IsRejected()
    {
        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "cat ../../etc/passwd"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("path traversal", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShellExecute_CdDotDot_IsRejected()
    {
        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "cd .. && ls"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("path traversal", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShellExecute_WorkingDirectoryOutsideRoot_IsRejected()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), "pga_shell_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDir);

        try
        {
            var tool = new ShellExecuteTool(rootDir, CreateProvider());
            var func = tool.ToAIFunction();
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["command"] = "echo hello",
                ["workingDirectory"] = Path.GetTempPath()
            }));

            var text = result?.ToString() ?? "";
            Assert.Contains("must be within the project root", text);
        }
        finally
        {
            Directory.Delete(rootDir, true);
        }
    }

    [Fact]
    public async Task ShellExecute_SubdirectoryWorkingDirectory_IsAllowed()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), "pga_shell_test_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(rootDir, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            var tool = new ShellExecuteTool(rootDir, CreateProvider());
            var func = tool.ToAIFunction();
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["command"] = "echo hello",
                ["workingDirectory"] = subDir
            }));

            var text = result?.ToString() ?? "";
            Assert.Contains("Exit code: 0", text);
        }
        finally
        {
            Directory.Delete(rootDir, true);
        }
    }
}

public class ShellExecuteProviderTests
{
    [Fact]
    public void LinuxProvider_DetectsPathTraversal()
    {
        var provider = new LinuxShellExecuteProvider();

        Assert.True(provider.ContainsPathTraversal("cat ../../etc/passwd"));
        Assert.True(provider.ContainsPathTraversal("cd .."));
        Assert.True(provider.ContainsPathTraversal("cd ../somewhere"));
        Assert.True(provider.ContainsPathTraversal("ls .."));
        Assert.False(provider.ContainsPathTraversal("echo hello"));
        Assert.False(provider.ContainsPathTraversal("cat file.txt"));
    }

    [Fact]
    public void WindowsProvider_DetectsPathTraversal()
    {
        var provider = new WindowsShellExecuteProvider();

        Assert.True(provider.ContainsPathTraversal(@"type ..\..\etc\passwd"));
        Assert.True(provider.ContainsPathTraversal("cd .."));
        Assert.True(provider.ContainsPathTraversal("cd ../somewhere"));
        Assert.True(provider.ContainsPathTraversal(@"cd ..\somewhere"));
        Assert.True(provider.ContainsPathTraversal("dir .."));
        Assert.False(provider.ContainsPathTraversal("echo hello"));
        Assert.False(provider.ContainsPathTraversal("type file.txt"));
    }

    [Fact]
    public void LinuxProvider_ValidatesDirectory_WithinRoot()
    {
        var provider = new LinuxShellExecuteProvider();
        var root = "/home/user/project";

        Assert.NotNull(provider.ResolveAndValidateDirectory("/home/user/project/src", root));
        Assert.NotNull(provider.ResolveAndValidateDirectory(null, root));
        Assert.Null(provider.ResolveAndValidateDirectory("/home/other", root));
        Assert.Null(provider.ResolveAndValidateDirectory("/home/user/project/../other", root));
    }

    [Fact]
    public void WindowsProvider_ValidatesDirectory_WithinRoot()
    {
        var provider = new WindowsShellExecuteProvider();

        // Use temp path for a root that exists on any OS (the validation uses Path.GetFullPath)
        var root = Path.Combine(Path.GetTempPath(), "testroot");
        var sub = Path.Combine(root, "sub");

        Assert.NotNull(provider.ResolveAndValidateDirectory(sub, root));
        Assert.NotNull(provider.ResolveAndValidateDirectory(null, root));
    }

    [Fact]
    public void CreatePlatformProvider_ReturnsCorrectType()
    {
        var provider = ShellExecuteTool.CreatePlatformProvider();
        if (OperatingSystem.IsWindows())
            Assert.IsType<WindowsShellExecuteProvider>(provider);
        else
            Assert.IsType<LinuxShellExecuteProvider>(provider);
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
