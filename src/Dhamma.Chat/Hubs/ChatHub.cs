using System.Runtime.CompilerServices;
using Dhamma.Core.Chat;
using Microsoft.AspNetCore.SignalR;

namespace Dhamma.Chat.Hubs;
// ChatHub.cs
public sealed record ChatChunk(string Text, bool IsThinking);

public class ChatHub(ChatOrchestrator orchestrator) : Hub
{
    public async IAsyncEnumerable<ChatChunk> Ask(
        Guid conversationId,
        string question,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var c in orchestrator.AskAsync(conversationId, question, ct))
            yield return new ChatChunk(c.Text, c.IsThinking);
    }
}
