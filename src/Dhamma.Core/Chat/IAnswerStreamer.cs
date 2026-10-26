// Dhamma.Core/Chat/IAnswerStreamer.cs
namespace Dhamma.Core.Chat;

public interface IAnswerStreamer
{
    IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question,
        IReadOnlyList<ChatTurn> history,
        CancellationToken ct);
}
