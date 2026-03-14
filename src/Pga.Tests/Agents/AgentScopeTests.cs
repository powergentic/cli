using Pga.Core.Agents;

namespace Pga.Tests.Agents;

public class AgentScopeTests
{
    [Fact]
    public void Applies_GlobalScope_AlwaysReturnsTrue()
    {
        var scope = new AgentScope { IsGlobal = true, BasePath = "/some/path" };

        Assert.True(scope.Applies("/any/path"));
        Assert.True(scope.Applies("/completely/different"));
        Assert.True(scope.Applies("/some/path/child"));
    }

    [Fact]
    public void Applies_EmptyBasePath_ReturnsTrue()
    {
        var scope = new AgentScope { IsGlobal = false, BasePath = "" };

        Assert.True(scope.Applies("/any/path"));
    }

    [Fact]
    public void Applies_MatchingPath_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var childDir = Path.Combine(tempDir, "sub", "deep");
        Directory.CreateDirectory(childDir);

        try
        {
            var scope = new AgentScope { IsGlobal = false, BasePath = tempDir };

            Assert.True(scope.Applies(childDir));
            Assert.True(scope.Applies(Path.Combine(tempDir, "file.txt")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Applies_NonMatchingPath_ReturnsFalse()
    {
        var tempDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir1);
        Directory.CreateDirectory(tempDir2);

        try
        {
            var scope = new AgentScope { IsGlobal = false, BasePath = tempDir1 };

            Assert.False(scope.Applies(tempDir2));
        }
        finally
        {
            Directory.Delete(tempDir1, true);
            Directory.Delete(tempDir2, true);
        }
    }

    [Fact]
    public void Applies_ExactBasePath_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var scope = new AgentScope { IsGlobal = false, BasePath = tempDir };

            Assert.True(scope.Applies(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void DefaultScope_IsNotGlobal()
    {
        var scope = new AgentScope();

        Assert.False(scope.IsGlobal);
        Assert.Equal(string.Empty, scope.BasePath);
    }
}
