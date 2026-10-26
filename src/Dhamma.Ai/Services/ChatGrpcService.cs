using Dhamma.Contracts.Ai;
using Dhamma.Core.Chat;
using Grpc.Core;

namespace Dhamma.Ai.Services;

public sealed class ChatGrpcService(IAnswerStreamer answers) : ChatService.ChatServiceBase
{
    private const int MaxHistory = 20; // 최근 20개 메시지만 사용
    public override async Task Ask(
        AskRequest request,
        IServerStreamWriter<AskResponse> stream,
        ServerCallContext ctx)
    {
        var history = request.History
            .TakeLast(MaxHistory)
            .Select(x => new ChatTurn(ParseRole(x.Role), x.Content))
            .ToList();

        await foreach (var chunk in answers.StreamAsync(request.Question, history, ctx.CancellationToken))
        {
            await stream.WriteAsync(
                new AskResponse { Text = chunk.Text, IsThinking = chunk.IsThinking },
                ctx.CancellationToken);
        }
    }

    private static ChatRole ParseRole(string role) => role switch
    {
        "user" => ChatRole.User,
        "assistant" => ChatRole.Assistant,
        _ => throw new RpcException(new Status(StatusCode.InvalidArgument, $"허용되지 않는 Role: '{role}'"))
    };
}
