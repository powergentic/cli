namespace Pga.Core.Configuration;

/// <summary>
/// Abstraction for loading and saving PGA configuration from/to a specific file format.
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    /// The file extensions this provider supports (e.g., ".json", ".yaml", ".yml").
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Determines whether this provider can handle the given file path based on its extension.
    /// </summary>
    bool CanHandle(string filePath);

    /// <summary>
    /// Loads a <see cref="PgaConfiguration"/> from the specified file path.
    /// </summary>
    PgaConfiguration Load(string filePath);

    /// <summary>
    /// Saves a <see cref="PgaConfiguration"/> to the specified file path.
    /// </summary>
    void Save(string filePath, PgaConfiguration config);

    /// <summary>
    /// Merges values from an override configuration into a base configuration.
    /// Non-null/non-default values in <paramref name="overrideConfig"/> replace those in <paramref name="baseConfig"/>.
    /// </summary>
    PgaConfiguration Merge(PgaConfiguration baseConfig, PgaConfiguration overrideConfig);
}
