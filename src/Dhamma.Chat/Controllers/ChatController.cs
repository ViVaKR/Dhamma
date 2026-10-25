using Dhamma.Contracts.Ai;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dhamma.Chat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(ChatService.ChatServiceClient ai) : ControllerBase
{
    public sealed record AskBody(string Question);

    [HttpPost]
    public async Task Ask([FromBody] AskBody body, CancellationToken ct)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        using var call = ai.Ask(new AskRequest { Question = body.Question }, cancellationToken: ct);

        await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
        {
            await Response.WriteAsync(chunk.Text, ct);
            await Response.Body.FlushAsync(ct); // 토큰이 오는 대로 바로 내보냄
        }
    }
}
