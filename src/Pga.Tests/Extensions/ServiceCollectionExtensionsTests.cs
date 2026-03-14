using Microsoft.Extensions.DependencyInjection;
using Pga.Core.Agents;
using Pga.Core.Configuration;
using Pga.Core.Extensions;
using Pga.Core.Tools;

namespace Pga.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPgaCore_RegistersConfigManager()
    {
        var services = new ServiceCollection();
        services.AddPgaCore();

        var provider = services.BuildServiceProvider();
        var configManager = provider.GetService<ConfigManager>();

        Assert.NotNull(configManager);
    }

    [Fact]
    public void AddPgaCore_RegistersAgentLoader()
    {
        var services = new ServiceCollection();
        services.AddPgaCore();

        var provider = services.BuildServiceProvider();
        var loader = provider.GetService<AgentLoader>();

        Assert.NotNull(loader);
    }

    [Fact]
    public void AddPgaCore_RegistersAgentMarkdownParser()
    {
        var services = new ServiceCollection();
        services.AddPgaCore();

        var provider = services.BuildServiceProvider();
        var parser = provider.GetService<AgentMarkdownParser>();

        Assert.NotNull(parser);
    }

    [Fact]
    public void AddPgaCore_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddPgaCore();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddPgaTools_RegistersToolRegistry()
    {
        var services = new ServiceCollection();
        services.AddPgaTools(Path.GetTempPath());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<ToolRegistry>();

        Assert.NotNull(registry);
    }

    [Fact]
    public void AddPgaTools_RegistryContainsDefaultTools()
    {
        var services = new ServiceCollection();
        services.AddPgaTools(Path.GetTempPath());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetService<ToolRegistry>();

        Assert.NotNull(registry);
        var tools = registry!.GetAll();
        Assert.True(tools.Count >= 9);
    }

    [Fact]
    public void AddPgaTools_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var result = services.AddPgaTools(Path.GetTempPath());

        Assert.Same(services, result);
    }

    [Fact]
    public void AddPgaCore_And_AddPgaTools_WorkTogether()
    {
        var services = new ServiceCollection();
        services.AddPgaCore();
        services.AddPgaTools(Path.GetTempPath());

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ConfigManager>());
        Assert.NotNull(provider.GetService<AgentLoader>());
        Assert.NotNull(provider.GetService<AgentMarkdownParser>());
        Assert.NotNull(provider.GetService<ToolRegistry>());
    }

    [Fact]
    public void AddPgaCore_RegistersSingletons()
    {
        var services = new ServiceCollection();
        services.AddPgaCore();

        var provider = services.BuildServiceProvider();
        var cm1 = provider.GetService<ConfigManager>();
        var cm2 = provider.GetService<ConfigManager>();

        Assert.Same(cm1, cm2);
    }
}
