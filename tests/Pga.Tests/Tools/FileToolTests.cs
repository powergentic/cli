using Microsoft.Extensions.AI;
using Pga.Core.Tools;
using Xunit;

namespace Pga.Tests.Tools;

public class FileReadToolTests : IDisposable
{
    private readonly string _testDir;

    public FileReadToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_fileread_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void FileReadTool_HasCorrectMetadata()
    {
        var tool = new FileReadTool();
        Assert.Equal("file_read", tool.Name);
        Assert.Equal(ToolSafetyLevel.ReadOnly, tool.SafetyLevel);
        Assert.NotNull(tool.Description);
    }

    [Fact]
    public async Task ReadFile_ReturnsContents()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "line1\nline2\nline3");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("line1", text);
        Assert.Contains("line2", text);
        Assert.Contains("line3", text);
    }

    [Fact]
    public async Task ReadFile_WithLineRange_ReturnsSubset()
    {
        var filePath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(filePath, "line1\nline2\nline3\nline4\nline5");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["startLine"] = 2,
            ["endLine"] = 4
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("line2", text);
        Assert.Contains("line3", text);
        Assert.Contains("line4", text);
        Assert.DoesNotContain("line5", text);
    }

    [Fact]
    public async Task ReadFile_NonexistentFile_ReturnsError()
    {
        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = "/nonexistent/file.txt"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task ReadFile_LineNumbersAreOneBased()
    {
        var filePath = Path.Combine(_testDir, "numbered.txt");
        File.WriteAllText(filePath, "alpha\nbeta\ngamma");

        var tool = new FileReadTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("1:", text);
    }
}

public class FileWriteToolTests : IDisposable
{
    private readonly string _testDir;

    public FileWriteToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_filewrite_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void FileWriteTool_HasCorrectMetadata()
    {
        var tool = new FileWriteTool();
        Assert.Equal("file_write", tool.Name);
        Assert.Equal(ToolSafetyLevel.Write, tool.SafetyLevel);
    }

    [Fact]
    public async Task WriteFile_CreatesFileWithContent()
    {
        var filePath = Path.Combine(_testDir, "output.txt");

        var tool = new FileWriteTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["content"] = "hello world"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Successfully", text);
        Assert.True(File.Exists(filePath));
        Assert.Equal("hello world", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task WriteFile_CreatesParentDirectories()
    {
        var filePath = Path.Combine(_testDir, "sub", "deep", "file.txt");

        var tool = new FileWriteTool();
        var func = tool.ToAIFunction();
        await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["content"] = "nested content"
        }));

        Assert.True(File.Exists(filePath));
        Assert.Equal("nested content", File.ReadAllText(filePath));
    }
}

public class FileEditToolTests : IDisposable
{
    private readonly string _testDir;

    public FileEditToolTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "pga_test_fileedit_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void FileEditTool_HasCorrectMetadata()
    {
        var tool = new FileEditTool();
        Assert.Equal("file_edit", tool.Name);
        Assert.Equal(ToolSafetyLevel.Write, tool.SafetyLevel);
    }

    [Fact]
    public async Task EditFile_ReplacesExactMatch()
    {
        var filePath = Path.Combine(_testDir, "edit.txt");
        File.WriteAllText(filePath, "Hello World");

        var tool = new FileEditTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["oldString"] = "World",
            ["newString"] = "Universe"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Successfully", text);
        Assert.Equal("Hello Universe", File.ReadAllText(filePath));
    }

    [Fact]
    public async Task EditFile_NonexistentFile_ReturnsError()
    {
        var tool = new FileEditTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = "/nonexistent/file.txt",
            ["oldString"] = "foo",
            ["newString"] = "bar"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("Error", text);
    }

    [Fact]
    public async Task EditFile_StringNotFound_ReturnsError()
    {
        var filePath = Path.Combine(_testDir, "edit.txt");
        File.WriteAllText(filePath, "Hello World");

        var tool = new FileEditTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["oldString"] = "NOTFOUND",
            ["newString"] = "replacement"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("not found", text);
    }

    [Fact]
    public async Task EditFile_MultipleMatches_ReturnsError()
    {
        var filePath = Path.Combine(_testDir, "edit.txt");
        File.WriteAllText(filePath, "foo bar foo baz");

        var tool = new FileEditTool();
        var func = tool.ToAIFunction();
        var result = await func.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["filePath"] = filePath,
            ["oldString"] = "foo",
            ["newString"] = "qux"
        }));

        var text = result?.ToString() ?? "";
        Assert.Contains("multiple", text, StringComparison.OrdinalIgnoreCase);
    }
}
