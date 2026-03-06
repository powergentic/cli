using System.Text.Json.Serialization;

namespace Pga.Core.Configuration;

/// <summary>
/// Source-generated JSON serializer context for PGA configuration types.
/// Required for AOT/trimmed builds where reflection-based serialization is disabled.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PgaConfiguration))]
[JsonSerializable(typeof(LlmProfile))]
[JsonSerializable(typeof(AutoSelectConfig))]
[JsonSerializable(typeof(AutoSelectRule))]
[JsonSerializable(typeof(ToolSafetyConfig))]
[JsonSerializable(typeof(UiConfig))]
internal partial class PgaJsonContext : JsonSerializerContext;
