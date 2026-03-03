using Microsoft.Extensions.AI;
using Pga.Core.Chat;

namespace Pga.Tests.Chat;

public class MessageHistoryTests
{
    [Fact]
    public void NewHistory_IsEmpty()
    {
        var history = new MessageHistory();

        Assert.Equal(0, history.Count);
        Assert.Empty(history.Messages);
    }

    [Fact]
    public void AddUserMessage_IncreasesCount()
    {
        var history = new MessageHistory();

        history.AddUserMessage("Hello");

        Assert.Equal(1, history.Count);
        Assert.Equal(ChatRole.User, history.Messages[0].Role);
    }

    [Fact]
    public void AddAssistantMessage_AddsCorrectRole()
    {
        var history = new MessageHistory();

        history.AddAssistantMessage("I can help!");

        Assert.Equal(1, history.Count);
        Assert.Equal(ChatRole.Assistant, history.Messages[0].Role);
    }

    [Fact]
    public void AddSystemMessage_InsertsAtBeginning()
    {
        var history = new MessageHistory();

        history.AddUserMessage("User message");
        history.AddSystemMessage("System message");

        Assert.Equal(2, history.Count);
        Assert.Equal(ChatRole.System, history.Messages[0].Role);
        Assert.Equal(ChatRole.User, history.Messages[1].Role);
    }

    [Fact]
    public void AddSystemMessage_ReplacesExistingSystemMessage()
    {
        var history = new MessageHistory();

        history.AddSystemMessage("First system message");
        history.AddUserMessage("User message");
        history.AddSystemMessage("Second system message");

        Assert.Equal(2, history.Count);
        Assert.Equal(ChatRole.System, history.Messages[0].Role);
        Assert.Equal(ChatRole.User, history.Messages[1].Role);
        // The system message should be the second one
        Assert.Contains("Second system message", history.Messages[0].Text);
    }

    [Fact]
    public void AddToolResult_AddsToolRole()
    {
        var history = new MessageHistory();

        history.AddToolResult("call-123", "Tool result content");

        Assert.Equal(1, history.Count);
        Assert.Equal(ChatRole.Tool, history.Messages[0].Role);
    }

    [Fact]
    public void AddMessage_AddsRawMessage()
    {
        var history = new MessageHistory();
        var msg = new ChatMessage(ChatRole.Assistant, "Raw message");

        history.AddMessage(msg);

        Assert.Equal(1, history.Count);
        Assert.Equal(ChatRole.Assistant, history.Messages[0].Role);
    }

    [Fact]
    public void ToList_ReturnsNewList()
    {
        var history = new MessageHistory();
        history.AddUserMessage("Test");

        var list1 = history.ToList();
        var list2 = history.ToList();

        Assert.Equal(1, list1.Count);
        Assert.NotSame(list1, list2); // Should be a new list each time
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        var history = new MessageHistory();
        history.AddSystemMessage("System");
        history.AddUserMessage("User");
        history.AddAssistantMessage("Assistant");

        Assert.Equal(3, history.Count);

        history.Clear();

        Assert.Equal(0, history.Count);
        Assert.Empty(history.Messages);
    }

    [Fact]
    public void MultipleMessages_MaintainsOrder()
    {
        var history = new MessageHistory();

        history.AddSystemMessage("System prompt");
        history.AddUserMessage("Hello");
        history.AddAssistantMessage("Hi there!");
        history.AddUserMessage("How are you?");
        history.AddAssistantMessage("I'm great!");

        Assert.Equal(5, history.Count);
        Assert.Equal(ChatRole.System, history.Messages[0].Role);
        Assert.Equal(ChatRole.User, history.Messages[1].Role);
        Assert.Equal(ChatRole.Assistant, history.Messages[2].Role);
        Assert.Equal(ChatRole.User, history.Messages[3].Role);
        Assert.Equal(ChatRole.Assistant, history.Messages[4].Role);
    }

    [Fact]
    public void ToList_ContainsAllMessages()
    {
        var history = new MessageHistory();
        history.AddSystemMessage("System");
        history.AddUserMessage("User");
        history.AddAssistantMessage("Assistant");

        var list = history.ToList();

        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void Messages_IsReadOnlyView()
    {
        var history = new MessageHistory();
        history.AddUserMessage("Test");

        var messages = history.Messages;

        Assert.IsAssignableFrom<IReadOnlyList<ChatMessage>>(messages);
    }
}
