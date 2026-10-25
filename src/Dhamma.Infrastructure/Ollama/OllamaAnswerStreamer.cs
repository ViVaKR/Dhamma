using System.Runtime.CompilerServices;
using Dhamma.Core.Chat;

namespace Dhamma.Infrastructure.Ollama;

public sealed class OllamaAnswerStreamer(OllamaChatClient chatClient) : IAnswerStreamer
{
    public async IAsyncEnumerable<string> StreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // OllamaChatClient의 스트리밍 로직을 그대로 위임하여 토큰을 토해냅니다.
        await foreach (var token in chatClient.StreamAsync(question, ct))
        {
            yield return token;
        }
    }
}
