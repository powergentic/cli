using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Pga.Core.Configuration;

/// <summary>
/// Configuration provider that reads and writes PGA configuration as YAML files (.yaml / .yml).
/// </summary>
public sealed class YamlConfigProvider : IConfigProvider
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public IReadOnlyList<string> SupportedExtensions { get; } = new[] { ".yaml", ".yml" };

    public bool CanHandle(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".yml", StringComparison.OrdinalIgnoreCase);
    }

    public PgaConfiguration Load(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);

        var yaml = File.ReadAllText(filePath);
        return Deserializer.Deserialize<PgaConfiguration>(yaml)
               ?? new PgaConfiguration();
    }

    public void Save(string filePath, PgaConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(config);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var yaml = Serializer.Serialize(config);
        File.WriteAllText(filePath, yaml);
    }

    public PgaConfiguration Merge(PgaConfiguration baseConfig, PgaConfiguration overrideConfig)
    {
        ArgumentNullException.ThrowIfNull(baseConfig);
        ArgumentNullException.ThrowIfNull(overrideConfig);

        return ConfigMerger.Merge(baseConfig, overrideConfig);
    }
}
