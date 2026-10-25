namespace Dhamma.Core.Chat;

public interface IAnswerStreamer
{
    IAsyncEnumerable<string> StreamAsync(string question, CancellationToken ct);
}
