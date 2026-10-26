using Dhamma.Core.Chat;

namespace Dhamma.Infrastructure.Ollama;

public sealed class OllamaAnswerStreamer(OllamaChatClient chatClient) : IAnswerStreamer
{
    public IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question,
        IReadOnlyList<ChatTurn> history,
        CancellationToken ct)
        => chatClient.StreamAsync(question, history, ct);
}
