using System.Text.Json;

namespace Pga.Core.Configuration;

/// <summary>
/// Configuration provider that reads and writes PGA configuration as JSON files.
/// </summary>
public sealed class JsonConfigProvider : IConfigProvider
{
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".json" };

    public bool CanHandle(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        return Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase);
    }

    public PgaConfiguration Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, PgaJsonContext.Default.PgaConfiguration)
               ?? new PgaConfiguration();
    }

    public void Save(string filePath, PgaConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(config);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, PgaJsonContext.Default.PgaConfiguration);
        File.WriteAllText(filePath, json);
    }

    public PgaConfiguration Merge(PgaConfiguration baseConfig, PgaConfiguration overrideConfig)
    {
        ArgumentNullException.ThrowIfNull(baseConfig);
        ArgumentNullException.ThrowIfNull(overrideConfig);

        return ConfigMerger.Merge(baseConfig, overrideConfig);
    }
}
