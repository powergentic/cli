using Microsoft.Extensions.AI;

namespace Pga.Core.Chat;

/// <summary>
/// Tracks message history for a chat session with token-awareness.
/// </summary>
public sealed class MessageHistory
{
    private readonly List<ChatMessage> _messages = new();

    /// <summary>
    /// All messages in the conversation.
    /// </summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>
    /// Adds a system message (typically at the start).
    /// </summary>
    public void AddSystemMessage(string content)
    {
        // Replace existing system message if present
        _messages.RemoveAll(m => m.Role == ChatRole.System);
        _messages.Insert(0, new ChatMessage(ChatRole.System, content));
    }

    /// <summary>
    /// Adds a user message.
    /// </summary>
    public void AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.User, content));
    }

    /// <summary>
    /// Adds an assistant message.
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(ChatRole.Assistant, content));
    }

    /// <summary>
    /// Adds a tool result message.
    /// </summary>
    public void AddToolResult(string callId, string result)
    {
        _messages.Add(new ChatMessage(ChatRole.Tool, result));
    }

    /// <summary>
    /// Adds a raw ChatMessage (e.g., from streaming responses with tool calls).
    /// </summary>
    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
    }

    /// <summary>
    /// Gets all messages as a list suitable for sending to the LLM.
    /// </summary>
    public IList<ChatMessage> ToList() => new List<ChatMessage>(_messages);

    /// <summary>
    /// Clears all messages.
    /// </summary>
    public void Clear() => _messages.Clear();

    /// <summary>
    /// Gets the number of messages.
    /// </summary>
    public int Count => _messages.Count;
}
