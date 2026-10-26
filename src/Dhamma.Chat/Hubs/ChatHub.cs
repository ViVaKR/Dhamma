using System.Runtime.CompilerServices;
using Dhamma.Chat.Models;
using Dhamma.Contracts.Ai;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace Dhamma.Chat.Hubs;

public sealed record ChatChunk(string Text, bool IsThinking);

public class ChatHub(ChatService.ChatServiceClient ai) : Hub
{
    public async IAsyncEnumerable<string> Ask(
        string question,
        List<ChatTurnDto>? history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var req = new AskRequest { Question = question };

        if (history is not null)
        {
            req.History.Add(history.Select(x => x.ToProto()));
        }

        using var call = ai.Ask(req, cancellationToken: ct);

        await foreach (var c in call.ResponseStream.ReadAllAsync(ct))
        {
            yield return c.Text;
        }
    }
}
