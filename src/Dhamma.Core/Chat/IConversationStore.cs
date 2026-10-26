namespace Dhamma.Core.Chat;

public interface IConversationStore
{
    Task<Conversation?> GetAsync(Guid id, CancellationToken ct);
    Task SaveAsync(Conversation conversation, CancellationToken ct);
}
