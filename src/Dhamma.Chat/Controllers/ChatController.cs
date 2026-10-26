using Dhamma.Chat.Models;
using Dhamma.Contracts.Ai;
using Dhamma.Core.Chat;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Dhamma.Chat.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(ChatOrchestrator orchestrator) : ControllerBase
{
    // ChatController.cs
    public sealed record AskBody(Guid ConversationId, string Question);

    [HttpPost]
    public async Task Ask([FromBody] AskBody body, [FromQuery] bool thinking, CancellationToken ct)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        await foreach (var chunk in orchestrator.AskAsync(body.ConversationId, body.Question, ct))
        {
            if (chunk.IsThinking && !thinking) continue;

            await Response.WriteAsync(chunk.Text, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
