using Dhamma.Contracts.Ai;
using Dhamma.Core.Chat;
using Grpc.Core;

namespace Dhamma.Ai.Services;

public sealed class ChatGrpcService(IAnswerStreamer answers) : ChatService.ChatServiceBase
{
    public override async Task Ask(
        AskRequest request,
        IServerStreamWriter<AskResponse> stream,
        ServerCallContext ctx)
    {
        await foreach (var token in answers.StreamAsync(request.Question, ctx.CancellationToken))
        {
            await stream.WriteAsync(new AskResponse { Text = token }, ctx.CancellationToken);
        }
    }
}
