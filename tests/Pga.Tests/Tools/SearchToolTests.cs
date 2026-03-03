using Microsoft.Extensions.AI;
using Pga.Core.Tools;
using Xunit;

namespace Pga.Tests.Tools;

public class FileSearchToolTests : IDisposable
{
    private readonly string _testDir;

    public FileSearchToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_search_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void FileSearchTool_HasCorrectMetadata()
    {
        var tool = new FileSearchTool();
        Assert.Equal("file_search", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public async Task SearchFiles_FindsMatchingFiles()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.cs"), "content");
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "content");

        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = _testDir,
            ["pattern"] = "*.cs"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("test.cs", text);
        Assert.DoesNotContain("test.txt", text);
    }

    [Fact]
    public async Task SearchFiles_NonexistentDirectory_ReturnsError()
    {
        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = "/nonexistent/dir",
            ["pattern"] = "*.cs"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task SearchFiles_NoMatches_ReturnsMessage()
    {
        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = _testDir,
            ["pattern"] = "*.xyz"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("No files found", text);
    }

    [Fact]
    public async Task SearchFiles_RespectsMaxResults()
    {
        for (int i = 0; i < 10; i++)
            File.WriteAllText(Path.Combine(_testDir, $"file{i}.cs"), "content");

        var tool = new FileSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["rootDirectory"] = _testDir,
            ["pattern"] = "*.cs",
            ["maxResults"] = 3
        }));

        var text = result?.ToString() ?? "";
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 3);
    }
}

public class GrepSearchToolTests : IDisposable
{
    private readonly string _testDir;

    public GrepSearchToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_grep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void GrepSearchTool_HasCorrectMetadata()
    {
        var tool = new GrepSearchTool();
        Assert.Equal("grep_search", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
    }

    [Fact]
    public async Task GrepSearch_FindsTextMatch()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "hello world\ngoodbye world");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "hello"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("hello world", text);
        Assert.Contains(":1:", text); // line number
    }

    [Fact]
    public async Task GrepSearch_IsCaseInsensitive()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "Hello World");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "hello"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Hello World", text);
    }

    [Fact]
    public async Task GrepSearch_WithRegex_FindsMatches()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "foo123\nbar456\nfoo789");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = @"foo\d+",
            ["isRegex"] = true
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("foo123", text);
        Assert.Contains("foo789", text);
    }

    [Fact]
    public async Task GrepSearch_NonexistentDirectory_ReturnsError()
    {
        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = "/nonexistent/dir",
            ["pattern"] = "test"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task GrepSearch_NoMatches_ReturnsMessage()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "hello world");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "NOTFOUND_xyz"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("No matches found", text);
    }

    [Fact]
    public async Task GrepSearch_InvalidRegex_ReturnsError()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "content");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "[invalid",
            ["isRegex"] = true
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task GrepSearch_WithFilePattern_FiltersByExtension()
    {
        File.WriteAllText(Path.Combine(_testDir, "test.cs"), "findme here");
        File.WriteAllText(Path.Combine(_testDir, "test.txt"), "findme there");

        var tool = new GrepSearchTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["directory"] = _testDir,
            ["pattern"] = "findme",
            ["filePattern"] = "*.cs"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("test.cs", text);
        Assert.DoesNotContain("test.txt", text);
    }
}
