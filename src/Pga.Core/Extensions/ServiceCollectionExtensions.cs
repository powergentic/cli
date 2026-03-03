using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pga.Core.Agents;
using Pga.Core.Configuration;
using Pga.Core.Tools;

namespace Pga.Core.Extensions;

/// <summary>
/// Extension methods for registering PGA services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all PGA core services.
    /// </summary>
    public static IServiceCollection AddPgaCore(this IServiceCollection services)
    {
        services.AddSingleton<ConfigManager>();
        services.AddSingleton<AgentLoader>();
        services.AddSingleton<AgentMarkdownParser>();
        return services;
    }

    /// <summary>
    /// Registers PGA tools for a specific working directory.
    /// </summary>
    public static IServiceCollection AddPgaTools(this IServiceCollection services, string workingDirectory)
    {
        services.AddSingleton(sp => ToolRegistry.CreateDefault(workingDirectory));
        return services;
    }
}
