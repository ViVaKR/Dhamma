// tests/Dhamma.Core.Tests/Fakes.cs
using System.Runtime.CompilerServices;
using Dhamma.Core.Chat;

namespace Dhamma.Core.Tests;

public sealed class FakeAnswerStreamer(params AnswerChunk[] chunks) : IAnswerStreamer
{
    public IReadOnlyList<ChatTurn>? ReceivedHistory { get; private set; }
    public bool ThrowMidway { get; init; }

    public async IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question, IReadOnlyList<ChatTurn> history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ReceivedHistory = history.ToList();   // 호출 시점의 스냅샷
        foreach (var c in chunks)
        {
            yield return c;
            await Task.Yield();
        }
        if (ThrowMidway) throw new InvalidOperationException("중간 실패");
    }
}

public sealed class FakeStore : IConversationStore
{
    public Dictionary<Guid, Conversation> Items { get; } = new();
    public Task<Conversation?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(Items.GetValueOrDefault(id));
    public Task SaveAsync(Conversation c, CancellationToken ct)
    {
        Items[c.Id] = c;
        return Task.CompletedTask;
    }
}
