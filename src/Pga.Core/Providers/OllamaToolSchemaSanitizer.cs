using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Pga.Core.Providers;

/// <summary>
/// A delegating chat client that sanitizes tool parameter JSON schemas
/// for compatibility with Ollama's API. Ollama expects "type": "string"
/// but Microsoft.Extensions.AI generates "type": ["string", "null"] for nullable params.
/// </summary>
internal sealed class OllamaToolSchemaSanitizer : DelegatingChatClient
{
    public OllamaToolSchemaSanitizer(IChatClient inner) : base(inner) { }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        options = SanitizeOptions(options);
        return base.GetResponseAsync(messages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        options = SanitizeOptions(options);
        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private static ChatOptions? SanitizeOptions(ChatOptions? options)
    {
        if (options?.Tools is null or { Count: 0 }) return options;

        var sanitizedTools = new List<AITool>();
        foreach (var tool in options.Tools)
        {
            if (tool is AIFunction aiFunction && aiFunction.JsonSchema is JsonElement schema)
            {
                var node = JsonNode.Parse(schema.GetRawText());
                if (node is JsonObject root && SanitizeProperties(root))
                {
                    var newSchema = JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());
                    sanitizedTools.Add(new SanitizedAIFunction(aiFunction, newSchema));
                    continue;
                }
            }
            sanitizedTools.Add(tool);
        }

        // Clone options with sanitized tools
        return new ChatOptions
        {
            Tools = sanitizedTools,
            Temperature = options.Temperature,
            TopP = options.TopP,
            TopK = options.TopK,
            MaxOutputTokens = options.MaxOutputTokens,
            StopSequences = options.StopSequences,
            FrequencyPenalty = options.FrequencyPenalty,
            PresencePenalty = options.PresencePenalty,
            Seed = options.Seed,
            ResponseFormat = options.ResponseFormat,
            ModelId = options.ModelId,
            ToolMode = options.ToolMode,
            AdditionalProperties = options.AdditionalProperties
        };
    }

    /// <summary>
    /// Recursively find "type": ["string", "null"] and convert to "type": "string".
    /// </summary>
    private static bool SanitizeProperties(JsonObject obj)
    {
        var changed = false;

        if (obj.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject properties)
        {
            var nullableProps = new List<string>();

            foreach (var (propName, propValue) in properties)
            {
                if (propValue is not JsonObject propObj) continue;

                if (propObj.TryGetPropertyValue("type", out var typeNode) && typeNode is JsonArray typeArray)
                {
                    var nonNullType = typeArray
                        .Select(t => t?.GetValue<string>())
                        .FirstOrDefault(t => t != "null");

                    if (nonNullType != null)
                    {
                        propObj["type"] = nonNullType;
                        nullableProps.Add(propName);
                        changed = true;
                    }
                }

                if (SanitizeProperties(propObj))
                    changed = true;
            }

            // Remove nullable props from "required" array
            if (nullableProps.Count > 0 &&
                obj.TryGetPropertyValue("required", out var reqNode) && reqNode is JsonArray requiredArray)
            {
                for (int i = requiredArray.Count - 1; i >= 0; i--)
                {
                    var name = requiredArray[i]?.GetValue<string>();
                    if (name != null && nullableProps.Contains(name))
                    {
                        requiredArray.RemoveAt(i);
                        changed = true;
                    }
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// Wraps an existing AIFunction but with a patched JsonSchema.
    /// Delegates all invocation to the original function.
    /// </summary>
    private sealed class SanitizedAIFunction : DelegatingAIFunction
    {
        private readonly JsonElement _sanitizedSchema;

        public SanitizedAIFunction(AIFunction inner, JsonElement sanitizedSchema) : base(inner)
        {
            _sanitizedSchema = sanitizedSchema;
        }

        public override JsonElement JsonSchema => _sanitizedSchema;
    }
}
