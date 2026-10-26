using Dhamma.Chat.Models;
using Dhamma.Contracts.Ai;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dhamma.Chat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(ChatService.ChatServiceClient ai) : ControllerBase
{
    public sealed record AskBody(string Question, List<ChatTurnDto>? History = null);

    [HttpPost]
    public async Task Ask([FromBody] AskBody body, [FromQuery] bool thinking, CancellationToken ct)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        var req = new AskRequest { Question = body.Question };
        if (body.History is not null)
        {
            req.History.Add(body.History.Select(x => x.ToProto()));
        }

        using var call = ai.Ask(req, cancellationToken: ct);

        await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
        {
            if (chunk.IsThinking && !thinking) continue;

            await Response.WriteAsync(chunk.Text, ct);
            await Response.Body.FlushAsync(ct); // 토큰이 오는 대로 바로 내보냄
        }
    }
}
