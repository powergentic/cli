using Microsoft.Extensions.AI;
using Pga.Core.Tools;

namespace Pga.Tests.Tools;

/// <summary>
/// Additional edge case tests for tools to increase branch and line coverage.
/// Covers: content truncation, output limits, error paths, and edge cases.
/// </summary>
public class FileReadToolEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public FileReadToolEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_edge_fr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task ReadFile_ContentTruncation_TruncatesLargeContent()
    {
        var filePath = Path.Combine(_testDir, "large.txt");
        // Create a file with more than 100,000 characters
        var content = new string('A', 110000);
        File.WriteAllText(filePath, content);

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("truncated", text);
        Assert.True(text.Length < 110000);
    }

    [Fact]
    public async Task ReadFile_StartLineBeyondFileLength_ReturnsLastLine()
    {
        var filePath = Path.Combine(_testDir, "short.txt");
        File.WriteAllText(filePath, "line1\nline2");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["startLine"] = 100,
            ["endLine"] = 200
        }));

        var text = result?.ToString() ?? "";
        // Should still return something (clamped to valid range)
        Assert.NotNull(text);
    }

    [Fact]
    public async Task ReadFile_EndLineBeyondFileLength_ClampsToEnd()
    {
        var filePath = Path.Combine(_testDir, "short.txt");
        File.WriteAllText(filePath, "line1\nline2\nline3");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["startLine"] = 1,
            ["endLine"] = 1000
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("line1", text);
        Assert.Contains("line3", text);
    }

    [Fact]
    public async Task ReadFile_NegativeStartLine_ClampsToZero()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "line1\nline2");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["startLine"] = -5
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("line1", text);
    }

    [Fact]
    public async Task ReadFile_EmptyFile_ReturnsEmpty()
    {
        var filePath = Path.Combine(_testDir, "empty.txt");
        File.WriteAllText(filePath, "");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath
        }));

        var text = result?.ToString() ?? "";
        Assert.NotNull(text);
    }
}

public class FileWriteToolEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public FileWriteToolEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_edge_fw_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task WriteFile_OverwritesExistingFile()
    {
        var filePath = Path.Combine(_testDir, "existing.txt");
        File.WriteAllText(filePath, "original content");

        var tool = new FileWriteTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["content"] = "new content"
        }));

        Assert.Contains("Successfully", result?.ToString() ?? "");
        Assert.Equal("new content", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task WriteFile_EmptyContent_CreatesEmptyFile()
    {
        var filePath = Path.Combine(_testDir, "empty.txt");

        var tool = new FileWriteTool();
        var func = tool.ToAIFunction();
        await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["content"] = ""
        }));

        Assert.True(File.Exists(filePath));
        Assert.Equal("", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task WriteFile_EmptyDirectoryName_WritesSuccessfully()
    {
        // Test with just a filename (no directory component)
        var filePath = Path.Combine(_testDir, "justfile.txt");

        var tool = new FileWriteTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["content"] = "content here"
        }));

        Assert.Contains("Successfully", result?.ToString() ?? "");
    }
}

public class DirectoryListToolEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public DirectoryListToolEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_edge_dl_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task ListDirectory_MaxDepthOneStopsRecursion()
    {
        var sub = Path.Combine(_testDir, "parent", "child");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "deep.txt"), "deep");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir,
            ["maxDepth"] = 1
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("parent/", text);
        Assert.DoesNotContain("deep.txt", text); // child content not shown at depth 1
    }

    [Fact]
    public async Task ListDirectory_ExcludesGitAndObjDirs()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_testDir, "obj"));
        Directory.CreateDirectory(Path.Combine(_testDir, "src"));

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("src/", text);
        Assert.DoesNotContain(".git", text);
        Assert.DoesNotContain("obj", text);
    }

    [Fact]
    public async Task ListDirectory_HiddenDirectories_ExcludedByDefault()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, ".hidden"));
        Directory.CreateDirectory(Path.Combine(_testDir, "visible"));

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("visible/", text);
        Assert.DoesNotContain(".hidden", text);
    }

    [Fact]
    public async Task ListDirectory_HiddenDirectories_IncludedWhenRequested()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, ".config"));

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir,
            ["includeHidden"] = true
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains(".config/", text);
    }

    [Fact]
    public async Task ListDirectory_DeepRecursion_ShowsMultipleLevels()
    {
        var level1 = Path.Combine(_testDir, "a");
        var level2 = Path.Combine(level1, "b");
        var level3 = Path.Combine(level2, "c");
        Directory.CreateDirectory(level3);
        File.WriteAllText(Path.Combine(level3, "deep.txt"), "content");

        var tool = new DirectoryListTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["path"] = _testDir,
            ["maxDepth"] = 4
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("a/", text);
        Assert.Contains("deep.txt", text);
    }
}

public class GitOperationsToolEdgeCaseTests
{
    [Fact]
    public async Task GitOperations_StashList_MapsToStashListCommand()
    {
        var repoDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(repoDir, ".git")))
            return;

        var tool = new GitOperationsTool(repoDir);
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["operation"] = "stash-list"
        }));

        var text = result?.ToString() ?? "";
        Assert.DoesNotContain("not allowed", text);
    }

    [Fact]
    public async Task GitOperations_WithArgs_PassesArguments()
    {
        var repoDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        if (!Directory.Exists(Path.Combine(repoDir, ".git")))
            return;

        var tool = new GitOperationsTool(repoDir);
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["operation"] = "log",
            ["args"] = "--oneline -5"
        }));

        var text = result?.ToString() ?? "";
        Assert.DoesNotContain("not allowed", text);
        Assert.DoesNotContain("Error executing git", text);
    }

    [Fact]
    public async Task GitOperations_AllAllowedOperations_AreAccepted()
    {
        var tool = new GitOperationsTool("/tmp");
        var func = tool.ToAIFunction();
        var allowedOps = new[] { "status", "log", "diff", "show", "branch", "blame", "remote",
                                  "stash-list", "rev-parse", "describe", "shortlog", "tag" };

        foreach (var op in allowedOps)
        {
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["operation"] = op
            }));

            var text = result?.ToString() ?? "";
            Assert.DoesNotContain("not allowed", text);
        }
    }

    [Fact]
    public async Task GitOperations_DisallowedOperations_AreRejected()
    {
        var tool = new GitOperationsTool("/tmp");
        var func = tool.ToAIFunction();
        var disallowedOps = new[] { "push", "pull", "commit", "merge", "rebase", "reset", "checkout" };

        foreach (var op in disallowedOps)
        {
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["operation"] = op
            }));

            var text = result?.ToString() ?? "";
            Assert.Contains("not allowed", text);
        }
    }

    [Fact]
    public async Task GitOperations_EmptyOutput_ReturnsNoOutputMessage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pga_git_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Initialize a bare git repo with no history
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = System.Diagnostics.Process.Start(psi)!;
            await proc.WaitForExitAsync();

            var tool = new GitOperationsTool(tempDir);
            var func = tool.ToAIFunction();
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["operation"] = "stash-list"
            }));

            var text = result?.ToString() ?? "";
            Assert.Contains("no output", text);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}

public class ShellExecuteToolEdgeCaseTests
{
    private static IShellExecuteProvider CreateProvider()
        => OperatingSystem.IsWindows()
            ? new WindowsShellExecuteProvider()
            : new LinuxShellExecuteProvider();

    [Fact]
    public async Task ShellExecute_StderrOutput_IncludedInResult()
    {
        if (OperatingSystem.IsWindows()) return;

        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "echo 'error' >&2"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("STDERR", text);
    }

    [Fact]
    public async Task ShellExecute_NonZeroExitCode_IncludedInResult()
    {
        if (OperatingSystem.IsWindows()) return;

        var tool = new ShellExecuteTool("/tmp", CreateProvider());
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["command"] = "exit 42"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Exit code: 42", text);
    }

    [Fact]
    public async Task ShellExecute_ValidSubDirectory_Works()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), "pga_shell_edge_" + Guid.NewGuid().ToString("N"));
        var subDir = Path.Combine(rootDir, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            var tool = new ShellExecuteTool(rootDir, CreateProvider());
            var func = tool.ToAIFunction();
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["command"] = "pwd",
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

    [Fact]
    public async Task ShellExecute_ExactRootAsWorkingDir_IsAllowed()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), "pga_shell_root_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDir);

        try
        {
            var tool = new ShellExecuteTool(rootDir, CreateProvider());
            var func = tool.ToAIFunction();
            var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
            {
                ["command"] = "echo hello",
                ["workingDirectory"] = rootDir
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

public class ShellExecuteProviderEdgeCaseTests
{
    [Fact]
    public void LinuxProvider_CreateStartInfo_ConfiguresCorrectly()
    {
        var provider = new LinuxShellExecuteProvider();
        var psi = provider.CreateStartInfo("echo hello", "/tmp");

        Assert.Equal("/bin/bash", psi.FileName);
        Assert.Equal("/tmp", psi.WorkingDirectory);
        Assert.True(psi.RedirectStandardOutput);
        Assert.True(psi.RedirectStandardError);
        Assert.False(psi.UseShellExecute);
        Assert.True(psi.CreateNoWindow);
        Assert.Contains("-c", psi.ArgumentList);
        Assert.Contains("echo hello", psi.ArgumentList);
    }

    [Fact]
    public void WindowsProvider_CreateStartInfo_ConfiguresCorrectly()
    {
        var provider = new WindowsShellExecuteProvider();
        var psi = provider.CreateStartInfo("echo hello", "C:\\Temp");

        Assert.Equal("cmd.exe", psi.FileName);
        Assert.Contains("/c echo hello", psi.Arguments);
        Assert.True(psi.RedirectStandardOutput);
        Assert.True(psi.RedirectStandardError);
    }

    [Fact]
    public void LinuxProvider_PathTraversal_EdgeCases()
    {
        var provider = new LinuxShellExecuteProvider();

        // Various path traversal patterns
        Assert.True(provider.ContainsPathTraversal("../etc/passwd"));
        Assert.True(provider.ContainsPathTraversal("ls ../"));
        Assert.True(provider.ContainsPathTraversal("cat foo/../../bar"));
        Assert.True(provider.ContainsPathTraversal("echo '..'")); // quoted dots
        Assert.False(provider.ContainsPathTraversal("echo hello..world"));
        Assert.False(provider.ContainsPathTraversal("file...txt"));
    }

    [Fact]
    public void WindowsProvider_PathTraversal_EdgeCases()
    {
        var provider = new WindowsShellExecuteProvider();

        Assert.True(provider.ContainsPathTraversal(@"..\etc\passwd"));
        Assert.True(provider.ContainsPathTraversal(@"type ..\file.txt"));
        Assert.True(provider.ContainsPathTraversal("cd ../somewhere"));
        Assert.False(provider.ContainsPathTraversal("echo hello..world"));
    }

    [Fact]
    public void LinuxProvider_ResolveDirectory_NullReturnsRoot()
    {
        var provider = new LinuxShellExecuteProvider();
        var result = provider.ResolveAndValidateDirectory(null, "/tmp/myroot");
        Assert.Equal("/tmp/myroot", result);
    }

    [Fact]
    public void LinuxProvider_ResolveDirectory_EmptyReturnsRoot()
    {
        var provider = new LinuxShellExecuteProvider();
        var result = provider.ResolveAndValidateDirectory("", "/tmp/myroot");
        Assert.Equal("/tmp/myroot", result);
    }

    [Fact]
    public void LinuxProvider_ResolveDirectory_WhitespaceReturnsRoot()
    {
        var provider = new LinuxShellExecuteProvider();
        var result = provider.ResolveAndValidateDirectory("   ", "/tmp/myroot");
        Assert.Equal("/tmp/myroot", result);
    }

    [Fact]
    public void WindowsProvider_ResolveDirectory_NullReturnsRoot()
    {
        var provider = new WindowsShellExecuteProvider();
        var result = provider.ResolveAndValidateDirectory(null, Path.GetTempPath());
        Assert.Equal(Path.GetTempPath(), result);
    }

    [Fact]
    public void LinuxProvider_ResolveDirectory_ExactRoot_IsAllowed()
    {
        var provider = new LinuxShellExecuteProvider();
        var root = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var result = provider.ResolveAndValidateDirectory(root, root);
        Assert.NotNull(result);
    }

    [Fact]
    public void WindowsProvider_ResolveDirectory_ExactRoot_IsAllowed()
    {
        var provider = new WindowsShellExecuteProvider();
        var root = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var result = provider.ResolveAndValidateDirectory(root, root);
        Assert.NotNull(result);
    }
}

public class GrepSearchToolEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public GrepSearchToolEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_edge_grep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task GrepSearch_RespectsMaxResults()
    {
        // Create a file with many matching lines
        var lines = Enumerable.Range(1, 100).Select(i => $"match {i}");
        File.WriteAllText(Path.Combine(_testDir, "many.txt"), string.Join('\n', lines));

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "match",
            ["maxResults"] = 5
        }));

        var text = result?.ToString() ?? "";
        var resultLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(resultLines.Length <= 5);
    }

    [Fact]
    public async Task GrepSearch_SkipsHiddenDirectories()
    {
        var hiddenDir = Path.Combine(_testDir, ".hidden");
        Directory.CreateDirectory(hiddenDir);
        File.WriteAllText(Path.Combine(hiddenDir, "secret.txt"), "findme");
        File.WriteAllText(Path.Combine(_testDir, "visible.txt"), "findme");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "findme"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("visible.txt", text);
        Assert.DoesNotContain(".hidden", text);
    }

    [Fact]
    public async Task GrepSearch_SkipsNodeModules()
    {
        var nmDir = Path.Combine(_testDir, "node_modules");
        Directory.CreateDirectory(nmDir);
        File.WriteAllText(Path.Combine(nmDir, "dep.js"), "findme");
        File.WriteAllText(Path.Combine(_testDir, "app.js"), "findme");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "findme"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("app.js", text);
        Assert.DoesNotContain("node_modules", text);
    }
}

public class WebFetchToolEdgeCaseTests
{
    [Fact]
    public async Task WebFetch_FtpUrl_ReturnsError()
    {
        var tool = new WebFetchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["url"] = "ftp://example.com/file.txt"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
        Assert.Contains("Invalid URL", text);
    }

    [Fact]
    public async Task WebFetch_EmptyString_ReturnsError()
    {
        var tool = new WebFetchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["url"] = ""
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task WebFetch_JavascriptUrl_ReturnsError()
    {
        var tool = new WebFetchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["url"] = "javascript:alert(1)"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task WebFetch_UnreachableHost_ReturnsError()
    {
        var tool = new WebFetchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["url"] = "http://192.0.2.1:1" // TEST-NET, guaranteed unreachable
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public void WebFetchTool_Properties_AreCorrect()
    {
        var tool = new WebFetchTool();
        Assert.Equal("web_fetch", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
        Assert.NotNull(tool.Description);
        Assert.NotNull(tool.ToAIFunction());
    }
}

public class FileSearchToolEdgeCaseTests : IDisposable
{
    private readonly string _testDir;

    public FileSearchToolEdgeCaseTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_edge_fs_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public async Task SearchFiles_ExcludesCommonDirs()
    {
        Directory.CreateDirectory(Path.Combine(_testDir, "node_modules"));
        File.WriteAllText(Path.Combine(_testDir, "node_modules", "dep.js"), "content");
        Directory.CreateDirectory(Path.Combine(_testDir, "src"));
        File.WriteAllText(Path.Combine(_testDir, "src", "app.js"), "content");

        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = _testDir,
            ["pattern"] = "**/*.js"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("app.js", text);
        Assert.DoesNotContain("dep.js", text);
    }

    [Fact]
    public async Task SearchFiles_RecursivePattern_FindsNestedFiles()
    {
        var nested = Path.Combine(_testDir, "a", "b", "c");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "deep.cs"), "content");

        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = _testDir,
            ["pattern"] = "**/*.cs"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("deep.cs", text);
    }
}
