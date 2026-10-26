// tests/Dhamma.Core.Tests/ChatOrchestratorTests.cs
using Dhamma.Core.Chat;

namespace Dhamma.Core.Tests;

public class ChatOrchestratorTests
{
    private static async Task<List<AnswerChunk>> Drain(IAsyncEnumerable<AnswerChunk> s)
    {
        var list = new List<AnswerChunk>();
        await foreach (var c in s) list.Add(c);
        return list;
    }

    [Fact]
    public async Task 정상_완료시_질문과_답변이_저장된다()
    {
        var store = new FakeStore();
        var streamer = new FakeAnswerStreamer(new AnswerChunk("안녕"), new AnswerChunk("하세요"));
        var sut = new ChatOrchestrator(streamer, store);
        var id = Guid.NewGuid();

        await Drain(sut.AskAsync(id, "인사", default));

        var turns = store.Items[id].Turns;
        Assert.Equal(2, turns.Count);
        Assert.Equal(new ChatTurn(ChatRole.User, "인사"), turns[0]);
        Assert.Equal(new ChatTurn(ChatRole.Assistant, "안녕하세요"), turns[1]);
    }

    [Fact]
    public async Task thinking은_저장되지_않는다()
    {
        var store = new FakeStore();
        var streamer = new FakeAnswerStreamer(
            new AnswerChunk("생각중...", IsThinking: true), new AnswerChunk("답"));
        var sut = new ChatOrchestrator(streamer, store);
        var id = Guid.NewGuid();

        await Drain(sut.AskAsync(id, "질문", default));

        Assert.Equal("답", store.Items[id].Turns[1].Content);
    }

    [Fact]
    public async Task 두번째_질문에는_이전_이력이_전달된다()
    {
        var store = new FakeStore();
        var id = Guid.NewGuid();

        await Drain(new ChatOrchestrator(
            new FakeAnswerStreamer(new AnswerChunk("A1")), store).AskAsync(id, "Q1", default));

        var second = new FakeAnswerStreamer(new AnswerChunk("A2"));
        await Drain(new ChatOrchestrator(second, store).AskAsync(id, "Q2", default));

        Assert.Equal(2, second.ReceivedHistory!.Count);
        Assert.Equal("Q1", second.ReceivedHistory[0].Content);
    }

    [Fact]
    public async Task 스트림_중간에_실패하면_저장되지_않는다()
    {
        var store = new FakeStore();
        var streamer = new FakeAnswerStreamer(new AnswerChunk("일부")) { ThrowMidway = true };
        var sut = new ChatOrchestrator(streamer, store);
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Drain(sut.AskAsync(id, "질문", default)));

        Assert.False(store.Items.ContainsKey(id));
    }

    [Fact]
    public async Task 대화_ID가_다르면_이력이_분리된다()
    {
        var store = new FakeStore();
        await Drain(new ChatOrchestrator(
            new FakeAnswerStreamer(new AnswerChunk("A")), store).AskAsync(Guid.NewGuid(), "Q", default));

        var other = new FakeAnswerStreamer(new AnswerChunk("B"));
        await Drain(new ChatOrchestrator(other, store).AskAsync(Guid.NewGuid(), "Q", default));

        Assert.Empty(other.ReceivedHistory!);
    }
}
