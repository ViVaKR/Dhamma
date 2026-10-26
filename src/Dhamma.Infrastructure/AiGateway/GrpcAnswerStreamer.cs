// Dhamma.Infrastructure/AiGateway/GrpcAnswerStreamer.cs
using System.Runtime.CompilerServices;
using Dhamma.Contracts.Ai;
using Dhamma.Core.Chat;
using Grpc.Core;

namespace Dhamma.Infrastructure.AiGateway;

public sealed class GrpcAnswerStreamer(ChatService.ChatServiceClient ai) : IAnswerStreamer
{
    public async IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question,
        IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var req = new AskRequest { Question = question };
        foreach (var turn in history)
        {
            req.History.Add(new ChatMessage
            {
                Role = turn.Role == ChatRole.User ? "user" : "assistant",
                Content = turn.Content
            });
        }

        using var call = ai.Ask(req, cancellationToken: ct);

        await foreach (var r in call.ResponseStream.ReadAllAsync(ct))
            yield return new AnswerChunk(r.Text, r.IsThinking);
    }
}
