namespace Dhamma.Core.Chat;

public sealed class Conversation(Guid id, DateTimeOffset createdAt)
{
    private readonly List<ChatTurn> _turns = [];

    public Guid Id { get; set; } = id;
    public DateTimeOffset CreatedAt { get; set; } = createdAt;
    public IReadOnlyList<ChatTurn> Turns => _turns;

    // 한번의 문답을 한꺼번에 추가 (질문만 남고 답이 없는 상태를 방지)
    public void AddExchange(string question, string answer)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("질분이 비어 있습니다.", nameof(question));

        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("답변이 비어 있습니다.", nameof(answer));

        _turns.Add(new ChatTurn(ChatRole.User, question));
        _turns.Add(new ChatTurn(ChatRole.Assistant, answer));
    }
}
