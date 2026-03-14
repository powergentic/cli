using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Pga.Core.Providers;

namespace Pga.Tests.Providers;

public class OllamaToolSchemaSanitizerTests
{
    private sealed class CapturingChatClient : IChatClient
    {
        public ChatOptions? CapturedOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CapturedOptions = options;
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "test")]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> chatMessages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CapturedOptions = options;
            return EmptyStream();
        }

        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private static async IAsyncEnumerable<ChatResponseUpdate> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class TestSchemaFunction : DelegatingAIFunction
    {
        private readonly JsonElement _customSchema;

        public TestSchemaFunction(AIFunction inner, JsonElement customSchema) : base(inner)
        {
            _customSchema = customSchema;
        }

        public override JsonElement JsonSchema => _customSchema;
    }

    private static AIFunction CreateToolWithSchema(string name, string schemaJson)
    {
        var baseFunc = AIFunctionFactory.Create(
            (string input) => "result",
            new AIFunctionFactoryOptions { Name = name, Description = $"Test tool {name}" });
        var schema = JsonDocument.Parse(schemaJson).RootElement;
        return new TestSchemaFunction(baseFunc, schema);
    }

    [Fact]
    public async Task GetResponseAsync_NullOptions_PassesThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], null);

        Assert.Null(inner.CapturedOptions);
    }

    [Fact]
    public async Task GetResponseAsync_EmptyTools_PassesThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var options = new ChatOptions { Tools = new List<AITool>() };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.NotNull(inner.CapturedOptions);
    }

    [Fact]
    public async Task GetResponseAsync_NullableTypeArray_ConvertedToSingleType()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("test_tool", """
        {
            "type": "object",
            "properties": {
                "name": { "type": ["string", "null"] },
                "age": { "type": "integer" }
            },
            "required": ["name", "age"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        var sanitizedTool = inner.CapturedOptions!.Tools![0] as AIFunction;
        Assert.NotNull(sanitizedTool);
        var schemaNode = JsonNode.Parse(sanitizedTool!.JsonSchema.GetRawText())!;

        var nameType = schemaNode["properties"]!["name"]!["type"];
        Assert.Equal(JsonValueKind.String, nameType!.GetValueKind());
        Assert.Equal("string", nameType.GetValue<string>());

        var ageType = schemaNode["properties"]!["age"]!["type"];
        Assert.Equal("integer", ageType!.GetValue<string>());
    }

    [Fact]
    public async Task GetResponseAsync_NullableProp_RemovedFromRequired()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("test_tool", """
        {
            "type": "object",
            "properties": {
                "name": { "type": ["string", "null"] },
                "count": { "type": "integer" }
            },
            "required": ["name", "count"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        var sanitizedTool = inner.CapturedOptions!.Tools![0] as AIFunction;
        var schemaNode = JsonNode.Parse(sanitizedTool!.JsonSchema.GetRawText())!;
        var required = schemaNode["required"]!.AsArray();
        var requiredNames = required.Select(r => r!.GetValue<string>()).ToList();

        Assert.DoesNotContain("name", requiredNames);
        Assert.Contains("count", requiredNames);
    }

    [Fact]
    public async Task GetResponseAsync_NestedProperties_SanitizedRecursively()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("nested_tool", """
        {
            "type": "object",
            "properties": {
                "config": {
                    "type": "object",
                    "properties": {
                        "host": { "type": ["string", "null"] }
                    },
                    "required": ["host"]
                }
            },
            "required": ["config"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        var sanitizedTool = inner.CapturedOptions!.Tools![0] as AIFunction;
        var schemaNode = JsonNode.Parse(sanitizedTool!.JsonSchema.GetRawText())!;

        var hostType = schemaNode["properties"]!["config"]!["properties"]!["host"]!["type"];
        Assert.Equal("string", hostType!.GetValue<string>());

        var nestedRequired = schemaNode["properties"]!["config"]!["required"]!.AsArray();
        Assert.Empty(nestedRequired);
    }

    [Fact]
    public async Task GetResponseAsync_NoNullableTypes_NoSchemaChange()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("clean_tool", """
        {
            "type": "object",
            "properties": {
                "name": { "type": "string" },
                "count": { "type": "integer" }
            },
            "required": ["name", "count"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.NotNull(inner.CapturedOptions);
        Assert.Single(inner.CapturedOptions!.Tools!);
    }

    [Fact]
    public async Task GetResponseAsync_PreservesOtherChatOptions()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("test_tool", """
        {
            "type": "object",
            "properties": { "name": { "type": ["string", "null"] } }
        }
        """);

        var options = new ChatOptions
        {
            Tools = new List<AITool> { tool },
            Temperature = 0.7f,
            TopP = 0.9f,
            MaxOutputTokens = 1000,
            ModelId = "test-model",
            Seed = 42
        };

        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.Equal(0.7f, inner.CapturedOptions!.Temperature);
        Assert.Equal(0.9f, inner.CapturedOptions.TopP);
        Assert.Equal(1000, inner.CapturedOptions.MaxOutputTokens);
        Assert.Equal("test-model", inner.CapturedOptions.ModelId);
        Assert.Equal(42, inner.CapturedOptions.Seed);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_SanitizesOptions()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("search_tool", """
        {
            "type": "object",
            "properties": { "query": { "type": ["string", "null"] } },
            "required": ["query"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await foreach (var _ in sanitizer.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "search")], options)) { }

        var sanitizedTool = inner.CapturedOptions!.Tools![0] as AIFunction;
        var schemaNode = JsonNode.Parse(sanitizedTool!.JsonSchema.GetRawText())!;
        Assert.Equal("string", schemaNode["properties"]!["query"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task GetStreamingResponseAsync_NullOptions_PassesThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        await foreach (var _ in sanitizer.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")], null)) { }

        Assert.Null(inner.CapturedOptions);
    }

    [Fact]
    public async Task GetResponseAsync_MultipleNullableProps_AllSanitized()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("multi", """
        {
            "type": "object",
            "properties": {
                "name": { "type": ["string", "null"] },
                "query": { "type": ["string", "null"] },
                "count": { "type": ["integer", "null"] }
            },
            "required": ["name", "query", "count"]
        }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        var sanitizedTool = inner.CapturedOptions!.Tools![0] as AIFunction;
        var schemaNode = JsonNode.Parse(sanitizedTool!.JsonSchema.GetRawText())!;

        Assert.Equal("string", schemaNode["properties"]!["name"]!["type"]!.GetValue<string>());
        Assert.Equal("string", schemaNode["properties"]!["query"]!["type"]!.GetValue<string>());
        Assert.Equal("integer", schemaNode["properties"]!["count"]!["type"]!.GetValue<string>());
        Assert.Empty(schemaNode["required"]!.AsArray());
    }

    [Fact]
    public async Task GetResponseAsync_MultipleTools_AllSanitized()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool1 = CreateToolWithSchema("tool1", """
        { "type": "object", "properties": { "path": { "type": ["string", "null"] } } }
        """);
        var tool2 = CreateToolWithSchema("tool2", """
        { "type": "object", "properties": { "url": { "type": ["string", "null"] } } }
        """);

        var options = new ChatOptions { Tools = new List<AITool> { tool1, tool2 } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.Equal(2, inner.CapturedOptions!.Tools!.Count);

        foreach (var aiTool in inner.CapturedOptions.Tools)
        {
            var aiFunc = aiTool as AIFunction;
            Assert.NotNull(aiFunc);
            var node = JsonNode.Parse(aiFunc!.JsonSchema.GetRawText())!;
            foreach (var (_, propVal) in node["properties"]!.AsObject())
            {
                Assert.Equal(JsonValueKind.String, propVal!.AsObject()["type"]!.GetValueKind());
            }
        }
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsResponseFromInnerClient()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var response = await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);
        Assert.NotNull(response);
        Assert.Equal("test", response.Text);
    }

    [Fact]
    public async Task GetResponseAsync_NullToolsList_PassesThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var options = new ChatOptions { Tools = null };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.NotNull(inner.CapturedOptions);
    }

    [Fact]
    public async Task GetResponseAsync_SchemaWithNoProperties_PassesThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = CreateToolWithSchema("no_props", """{ "type": "object" }""");

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.Single(inner.CapturedOptions!.Tools!);
    }

    [Fact]
    public async Task GetResponseAsync_NonAIFunctionTool_PassedThrough()
    {
        var inner = new CapturingChatClient();
        var sanitizer = new OllamaToolSchemaSanitizer(inner);

        var tool = AIFunctionFactory.Create(
            (string name) => "result",
            new AIFunctionFactoryOptions { Name = "simple_tool", Description = "Simple tool" });

        var options = new ChatOptions { Tools = new List<AITool> { tool } };
        await sanitizer.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")], options);

        Assert.Single(inner.CapturedOptions!.Tools!);
    }
}
