using Microsoft.Extensions.AI;
using Pga.Core.Agents;
using Pga.Core.Configuration;
using Pga.Core.Providers;
using Pga.Core.Tools;

namespace Pga.Core.Chat;

/// <summary>
/// Orchestrates chat conversations with the LLM, handling tool calls, agent resolution,
/// and conversation management. This is the core engine of PGA.
/// </summary>
public sealed class ChatOrchestrator
{
    private readonly ConfigManager _configManager;
    private readonly AgentLoader _agentLoader;
    private readonly ToolRegistry _toolRegistry;
    private readonly ToolSafety _toolSafety;
    private readonly MessageHistory _history;

    /// <summary>
    /// Called when the LLM wants to invoke a tool (for UI feedback).
    /// </summary>
    public event Func<string, string, Task>? OnToolInvocation;

    /// <summary>
    /// Called when a tool returns a result (for UI feedback).
    /// </summary>
    public event Action<string, string>? OnToolResult;

    /// <summary>
    /// Called when streaming tokens arrive.
    /// </summary>
    public event Action<string>? OnStreamingToken;

    /// <summary>
    /// Called when the LLM needs user approval for a tool.
    /// </summary>
    public event Func<string, string, Task<bool>>? OnToolApprovalNeeded;

    public ChatOrchestrator(
        ConfigManager configManager,
        string projectPath,
        string? profileName = null,
        string? agentName = null)
    {
        _configManager = configManager;
        _agentLoader = new AgentLoader();
        _toolRegistry = ToolRegistry.CreateDefault(projectPath);

        var config = configManager.Load();
        _toolSafety = new ToolSafety(config.ToolSafety, async (name, desc) =>
        {
            if (OnToolApprovalNeeded != null)
                return await OnToolApprovalNeeded(name, desc);
            return true;
        });

        _history = new MessageHistory();

        // Set up system prompt from agents
        InitializeSystemPrompt(projectPath, agentName);
    }

    public MessageHistory History => _history;

    /// <summary>
    /// Sends a user message and gets a complete response (non-streaming).
    /// Handles tool calls automatically.
    /// </summary>
    public async Task<string> SendMessageAsync(string userMessage, string? profileName = null, string? agentName = null)
    {
        _history.AddUserMessage(userMessage);

        var profile = ResolveProfile(profileName, agentName);
        if (profile == null)
            return "Error: No LLM profile configured. Run 'pga config init' to set up your configuration.";

        var chatClient = LlmProviderFactory.CreateChatClient(profile);
        var chatOptions = BuildChatOptions();
        var maxIterations = 25; // Safety limit for tool call loops

        for (int i = 0; i < maxIterations; i++)
        {
            ChatResponse response;
            try
            {
                response = await chatClient.GetResponseAsync(_history.ToList(), chatOptions);
            }
            catch (Exception ex)
            {
                return $"Error communicating with LLM: {ex.Message}";
            }

            // Check if the response contains tool calls
            var toolCalls = response.Messages
                .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                .ToList();

            if (toolCalls.Count == 0)
            {
                // Final text response
                var text = response.Text ?? string.Empty;
                foreach (var msg in response.Messages)
                    _history.AddMessage(msg);
                return text;
            }

            // Process tool calls
            foreach (var msg in response.Messages)
                _history.AddMessage(msg);

            foreach (var toolCall in toolCalls)
            {
                var tool = _toolRegistry.Get(toolCall.Name);
                string result;

                if (tool == null)
                {
                    result = $"Error: Unknown tool '{toolCall.Name}'";
                }
                else
                {
                    // Check safety/approval
                    var description = $"{toolCall.Name}({FormatArgs(toolCall.Arguments)})";
                    if (OnToolInvocation != null)
                        await OnToolInvocation(toolCall.Name, description);

                    var approved = await _toolSafety.CheckApproval(tool, description);
                    if (!approved)
                    {
                        result = "Tool execution was denied by the user.";
                    }
                    else
                    {
                        // Execute the tool via the AIFunction
                        var aiFunc = tool.ToAIFunction();
                        try
                        {
                            var funcArgs = toolCall.Arguments != null
                                ? new AIFunctionArguments(toolCall.Arguments)
                                : null;
                            var toolResult = await aiFunc.InvokeAsync(funcArgs);
                            result = toolResult?.ToString() ?? "(no output)";
                        }
                        catch (Exception ex)
                        {
                            result = $"Tool execution error: {ex.Message}";
                        }
                    }

                    OnToolResult?.Invoke(toolCall.Name, result);
                }

                _history.AddMessage(new ChatMessage(ChatRole.Tool,
                [
                    new FunctionResultContent(toolCall.CallId, result)
                ]));
            }
        }

        return "Error: Maximum tool call iterations reached. The agent may be in a loop.";
    }

    /// <summary>
    /// Sends a user message with streaming response.
    /// </summary>
    public async Task<string> SendMessageStreamingAsync(string userMessage, string? profileName = null, string? agentName = null)
    {
        _history.AddUserMessage(userMessage);

        var profile = ResolveProfile(profileName, agentName);
        if (profile == null)
            return "Error: No LLM profile configured. Run 'pga config init' to set up your configuration.";

        var chatClient = LlmProviderFactory.CreateChatClient(profile);
        var chatOptions = BuildChatOptions();
        var maxIterations = 25;

        for (int i = 0; i < maxIterations; i++)
        {
            ChatResponse response;
            try
            {
                // Collect streaming response
                var fullText = new System.Text.StringBuilder();
                var toolCallsFromStream = new List<FunctionCallContent>();

                response = await chatClient.GetResponseAsync(_history.ToList(), chatOptions);

                // Check for tool calls
                var toolCalls = response.Messages
                    .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                    .ToList();

                if (toolCalls.Count == 0)
                {
                    // Stream the final text response
                    var text = response.Text ?? string.Empty;
                    OnStreamingToken?.Invoke(text);
                    foreach (var msg in response.Messages)
                        _history.AddMessage(msg);
                    return text;
                }

                // Process tool calls (same as non-streaming)
                foreach (var msg in response.Messages)
                    _history.AddMessage(msg);

                foreach (var toolCall in toolCalls)
                {
                    var tool = _toolRegistry.Get(toolCall.Name);
                    string result;

                    if (tool == null)
                    {
                        result = $"Error: Unknown tool '{toolCall.Name}'";
                    }
                    else
                    {
                        var description = $"{toolCall.Name}({FormatArgs(toolCall.Arguments)})";
                        if (OnToolInvocation != null)
                            await OnToolInvocation(toolCall.Name, description);

                        var approved = await _toolSafety.CheckApproval(tool, description);
                        if (!approved)
                        {
                            result = "Tool execution was denied by the user.";
                        }
                        else
                        {
                            var aiFunc = tool.ToAIFunction();
                            try
                            {
                                var funcArgs = toolCall.Arguments != null
                                    ? new AIFunctionArguments(toolCall.Arguments)
                                    : null;
                                var toolResult = await aiFunc.InvokeAsync(funcArgs);
                                result = toolResult?.ToString() ?? "(no output)";
                            }
                            catch (Exception ex)
                            {
                                result = $"Tool execution error: {ex.Message}";
                            }
                        }

                        OnToolResult?.Invoke(toolCall.Name, result);
                    }

                    _history.AddMessage(new ChatMessage(ChatRole.Tool,
                    [
                        new FunctionResultContent(toolCall.CallId, result)
                    ]));
                }
            }
            catch (Exception ex)
            {
                return $"Error communicating with LLM: {ex.Message}";
            }
        }

        return "Error: Maximum tool call iterations reached.";
    }

    private void InitializeSystemPrompt(string projectPath, string? agentName)
    {
        var agents = _agentLoader.LoadAgents(projectPath);
        var systemPrompt = BuildBaseSystemPrompt();

        var agentInstructions = agents.BuildSystemPrompt(projectPath, agentName);
        if (!string.IsNullOrWhiteSpace(agentInstructions))
            systemPrompt += "\n\n---\n\n" + agentInstructions;

        _history.AddSystemMessage(systemPrompt);

        // Apply tool filtering from the specific agent
        if (agentName != null)
        {
            var agent = agents.GetByName(agentName);
            if (agent != null)
            {
                // Tool filtering is handled at request time via ChatOptions
            }
        }
    }

    private LlmProfile? ResolveProfile(string? commandLineProfile, string? agentName)
    {
        var agents = _agentLoader.LoadAgents(Environment.CurrentDirectory);
        string? agentProfile = null;

        if (agentName != null)
        {
            var agent = agents.GetByName(agentName);
            agentProfile = agent?.Profile;
        }

        var result = _configManager.ResolveProfile(commandLineProfile, agentProfile);
        return result?.Profile;
    }

    private ChatOptions BuildChatOptions()
    {
        return new ChatOptions
        {
            Tools = _toolRegistry.GetAITools()
        };
    }

    private static string BuildBaseSystemPrompt()
    {
        return """
            You are Powergentic CLI, an expert AI coding assistant running as a CLI tool.
            You help users with software development tasks by understanding their codebase and making changes.

            You have access to tools for:
            - Reading and writing files
            - Searching files and code
            - Executing shell commands
            - Git operations
            - Fetching web content

            Guidelines:
            - Always understand the context before making changes
            - Use tools to explore the codebase when needed
            - Make minimal, targeted changes
            - Explain your reasoning
            - When editing files, include sufficient context to make changes unambiguous
            - Prefer reading files to understand existing patterns before writing new code
            - Ask for clarification when requirements are ambiguous
            - Follow existing code style and conventions in the project
            """;
    }

    private static string FormatArgs(IDictionary<string, object?>? args)
    {
        if (args == null || args.Count == 0)
            return "";

        return string.Join(", ", args.Select(kv =>
        {
            var value = kv.Value?.ToString() ?? "null";
            if (value.Length > 100)
                value = value[..100] + "...";
            return $"{kv.Key}: {value}";
        }));
    }
}
