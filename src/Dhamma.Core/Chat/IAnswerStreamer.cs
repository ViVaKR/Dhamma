namespace Dhamma.Core.Chat;

public interface IAnswerStreamer
{
    IAsyncEnumerable<AnswerChunk> StreamAsync(string question, CancellationToken ct);
}
