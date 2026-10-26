using Dhamma.Core.Chat;

namespace Dhamma.Infrastructure.Ollama;

public sealed class OllamaAnswerStreamer(OllamaChatClient chatClient) : IAnswerStreamer
{
    public IAsyncEnumerable<AnswerChunk> StreamAsync(string question, CancellationToken ct)
        => chatClient.StreamAsync(question, ct);
}
