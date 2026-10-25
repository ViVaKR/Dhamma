using System.Runtime.CompilerServices;
using Dhamma.Contracts.Ai;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace Dhamma.Chat.Hubs;

public class ChatHub(ChatService.ChatServiceClient ai) : Hub
{
    public async IAsyncEnumerable<string> Ask(
        string question,
        [EnumeratorCancellation] CancellationToken ct)
    {
        using var call = ai.Ask(new AskRequest { Question = question }, cancellationToken: ct);

        await foreach (var c in call.ResponseStream.ReadAllAsync(ct))
        {
            yield return c.Text;
        }
    }
}
