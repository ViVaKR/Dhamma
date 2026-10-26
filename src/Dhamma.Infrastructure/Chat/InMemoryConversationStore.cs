using System.Collections.Concurrent;
using Dhamma.Core.Chat;

namespace Dhamma.Infrastructure.Chat;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, Conversation> _items = new();

    public Task<Conversation?> GetAsync(Guid id, CancellationToken ct)
    {
        return Task.FromResult(_items.GetValueOrDefault(id));
    }

    public Task SaveAsync(Conversation conversation, CancellationToken ct)
    {
        _items[conversation.Id] = conversation;
        return Task.CompletedTask;
    }
}
