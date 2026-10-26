// Dhamma.Core/Chat/ChatOrchestrator.cs
using System.Runtime.CompilerServices;
using System.Text;

namespace Dhamma.Core.Chat;

public sealed class ChatOrchestrator(IAnswerStreamer answers, IConversationStore store)
{
    public async IAsyncEnumerable<AnswerChunk> AskAsync(Guid conversationId, string question, [EnumeratorCancellation] CancellationToken ct)
    {
        var conversation = await store.GetAsync(conversationId, ct)
            ?? new Conversation(conversationId, DateTimeOffset.UtcNow);

        var answer = new StringBuilder();

        await foreach (var chunk in answers.StreamAsync(question, conversation.Turns, ct))
        {
            if (!chunk.IsThinking) answer.Append(chunk.Text);   // thinking은 저장하지 않음
            yield return chunk;
        }

        // 정상 완료된 경우에만 저장
        if (answer.Length > 0)
        {
            conversation.AddExchange(question, answer.ToString());
            await store.SaveAsync(conversation, ct);
        }
    }
}
