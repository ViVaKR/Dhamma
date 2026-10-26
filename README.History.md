# 멀티턴 (Chat History)

## 멀티턴이란?

**LLM은 기억력이 없습니다.** 요청 한 번이 끝나면 방금 나눈 대화를 전부 잊어버립니다. ChatGPT가 앞 대화를 기억하는 것처럼 보이는 이유는, 매번 **지금까지의 대화 전체를 다시 보내주기 때문**입니다.

지금 우리 코드는 이렇게 보냅니다 (싱글턴).

```text
system: 당신은 부처님의 가르침을...
user:   색즉시공 설명해줘
```

그래서 이어서 "방금 말한 걸 한 줄로 요약해줘"라고 하면 모델은 "방금"이 뭔지 모릅니다. 멀티턴은 이렇게 보내는 것입니다.

```text
system:    당신은 부처님의 가르침을...
user:      색즉시공 설명해줘          ← 1턴 질문
assistant: 색즉시공은 ...입니다.      ← 1턴 답변
user:      방금 말한 걸 한 줄로 요약해줘  ← 2턴 질문 (지금 질문)
```

Ollama `/api/chat`의 `messages` 배열이 원래 이 용도로 만들어진 구조라서, 우리가 할 일은 **이력을 gRPC 계약에 싣고 끝까지 운반하는 것**입니다.

## 설계 결정: 이력을 누가 들고 있나?

| 방식 | 설명 | 비고 |
|---|---|---|
| A. 클라이언트가 매번 전체 이력 전송 | 서버는 상태 없음 | 가장 단순, 이번 단계 |
| B. 서버가 저장하고 `conversationId`만 받음 | Redis/DB 사용 | README 계획, 다음 단계 |

이번에는 A로 갑니다. 이력을 나르는 배관(proto → Core → Infra → Ai → Chat)을 먼저 만들어 두면, B로 넘어갈 때 "이력을 어디서 가져오느냐"만 바뀝니다. 배관은 재사용됩니다.

작업 순서는 지난번에 드린 체크리스트 그대로 **proto → Core → Infrastructure → Ai → Chat**입니다.

## 1. Contracts: `chat.proto`

```proto
message ChatMessage {
  string role = 1;      // "user" | "assistant"
  string content = 2;
}

message AskRequest {
  string question = 1;                // 지금 질문
  repeated ChatMessage history = 2;   // 이전 대화 (오래된 것 → 최신 순)
}
```

필드 추가뿐이라 기존 호출과 호환됩니다. `repeated`는 C#에서 리스트처럼 쓰는 컬렉션이 됩니다.

## 2. Core

```csharp
// Dhamma.Core/Chat/ChatTurn.cs
namespace Dhamma.Core.Chat;

public enum ChatRole { User, Assistant }

public sealed record ChatTurn(ChatRole Role, string Content);
```

```csharp
// Dhamma.Core/Chat/IAnswerStreamer.cs
namespace Dhamma.Core.Chat;

public interface IAnswerStreamer
{
    IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question,
        IReadOnlyList<ChatTurn> history,
        CancellationToken ct);
}
```

Core는 "user/assistant"라는 문자열 대신 `enum`을 씁니다. 문자열 규칙은 바깥 세계(Ollama, proto)의 사정이라 Core가 알 필요가 없습니다.

## 3. Infrastructure

```csharp
// OllamaChatClient.cs
public async IAsyncEnumerable<AnswerChunk> StreamAsync(
    string question,
    IReadOnlyList<ChatTurn> history,
    [EnumeratorCancellation] CancellationToken ct)
{
    var messages = new List<object>
    {
        new { role = "system", content = options.Value.SystemPrompt }
    };

    foreach (var turn in history)
    {
        messages.Add(new
        {
            role = turn.Role == ChatRole.User ? "user" : "assistant",
            content = turn.Content
        });
    }

    messages.Add(new { role = "user", content = question });

    var body = new
    {
        model = options.Value.Model,
        stream = true,
        think = options.Value.Think,
        messages
    };

    // 이하 기존 코드와 동일 (request 생성, SendAsync, 읽기 루프)
}
```

```csharp
// OllamaAnswerStreamer.cs
public sealed class OllamaAnswerStreamer(OllamaChatClient chatClient) : IAnswerStreamer
{
    public IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question, IReadOnlyList<ChatTurn> history, CancellationToken ct)
        => chatClient.StreamAsync(question, history, ct);
}
```

## 4. Ai: `ChatGrpcService.cs`

```csharp
public sealed class ChatGrpcService(IAnswerStreamer answers) : ChatService.ChatServiceBase
{
    private const int MaxHistory = 20;   // 최근 20개 메시지만 사용

    public override async Task Ask(
        AskRequest request,
        IServerStreamWriter<AskResponse> stream,
        ServerCallContext ctx)
    {
        var history = request.History
            .TakeLast(MaxHistory)
            .Select(m => new ChatTurn(ParseRole(m.Role), m.Content))
            .ToList();

        await foreach (var chunk in answers.StreamAsync(request.Question, history, ctx.CancellationToken))
        {
            await stream.WriteAsync(
                new AskResponse { Text = chunk.Text, IsThinking = chunk.IsThinking },
                ctx.CancellationToken);
        }
    }

    private static ChatRole ParseRole(string role) => role switch
    {
        "user" => ChatRole.User,
        "assistant" => ChatRole.Assistant,
        _ => throw new RpcException(new Status(
            StatusCode.InvalidArgument, $"허용되지 않는 role: '{role}'"))
    };
}
```

`ParseRole`이 `system` 같은 값을 거부하는 것이 중요합니다. 이력은 바깥(클라이언트)에서 오는 데이터라서, 누군가 `role: "system"`으로 프롬프트를 덮어쓰는 것을 여기서 막습니다.

## 5. Chat

**DTO와 변환** (새 파일)

```csharp
// Dhamma.Chat/Models/ChatTurnDto.cs
using Dhamma.Contracts.Ai;

namespace Dhamma.Chat.Models;

public sealed record ChatTurnDto(string Role, string Content)
{
    public ChatMessage ToProto() => new() { Role = Role, Content = Content };
}
```

**컨트롤러**

```csharp
public sealed record AskBody(string Question, List<ChatTurnDto>? History = null);

[HttpPost]
public async Task Ask(
    [FromBody] AskBody body,
    [FromQuery] bool thinking,
    CancellationToken ct)
{
    Response.ContentType = "text/plain; charset=utf-8";

    var req = new AskRequest { Question = body.Question };
    if (body.History is not null)
        req.History.Add(body.History.Select(t => t.ToProto()));

    using var call = ai.Ask(req, cancellationToken: ct);

    await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
    {
        if (chunk.IsThinking && !thinking) continue;

        await Response.WriteAsync(chunk.Text, ct);
        await Response.Body.FlushAsync(ct);
    }
}
```

**SignalR Hub**

```csharp
public async IAsyncEnumerable<ChatChunk> Ask(
    string question,
    List<ChatTurnDto>? history,
    [EnumeratorCancellation] CancellationToken ct)
{
    var req = new AskRequest { Question = question };
    if (history is not null)
        req.History.Add(history.Select(t => t.ToProto()));

    using var call = ai.Ask(req, cancellationToken: ct);

    await foreach (var c in call.ResponseStream.ReadAllAsync(ct))
        yield return new ChatChunk(c.Text, c.IsThinking);
}
```

`ChatHub`의 `Ask`는 파라미터가 하나 늘었으니 프런트에서 호출할 때 `history`도 넘겨야 합니다 (아래 참고).

## 테스트

빌드 후 두 번째 질문에 **이력을 실어서** 보냅니다.

```bash
curl -N -X POST "http://localhost:5186/api/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "question": "방금 설명한 내용을 한 줄로 요약해줘",
    "history": [
      {"role": "user", "content": "색즉시공에 대하여 설명 부탁해"},
      {"role": "assistant", "content": "색즉시공은 형상이 곧 공이라는 뜻으로, 모든 현상에 고정된 실체가 없음을 말합니다."}
    ]
  }'
```

"방금 설명한 내용"을 알고 요약하면 성공입니다. 비교를 위해 `history` 없이 같은 질문을 보내 보세요. 모델이 무엇을 요약해야 할지 몰라 헤매는 것을 보면 멀티턴이 무엇인지 체감됩니다.

## 프런트에서의 사용 (Angular 개념)

```typescript
history: { role: 'user' | 'assistant'; content: string }[] = [];

async send(question: string) {
  let answer = '';   // thinking은 제외하고 content만 모음

  // 스트림 수신 시: if (!chunk.isThinking) answer += chunk.text;
  // hub.stream('Ask', question, this.history) 로 호출

  // 스트림이 끝나면 이번 턴을 이력에 추가
  this.history.push({ role: 'user', content: question });
  this.history.push({ role: 'assistant', content: answer });
}
```

**사고 과정(thinking)은 이력에 넣지 않습니다.** 모델에게 다시 보낼 필요도 없고 토큰만 낭비하며, 모델에 따라 혼란을 줄 수 있기 때문입니다. 위 코드에서 `answer`에 `isThinking`이 false인 것만 모으는 이유입니다.

## 알아두면 좋은 한계

- **컨텍스트 창이 유한합니다.** 대화가 길어질수록 보내는 양이 늘고, 첫 토큰이 나오기까지(thinking 포함) 더 느려집니다. 그래서 `MaxHistory = 20`으로 잘랐습니다. 나중에는 "오래된 대화 요약" 같은 방식으로 발전시킬 수 있습니다.
- **`TakeLast`가 앞부분을 자르면** 이력이 assistant 메시지로 시작할 수 있습니다. 지금은 문제없지만 정교하게 하려면 user·assistant 쌍 단위로 자르세요.
- **클라이언트를 믿는 구조**입니다. 사용자가 이력을 조작해서 assistant가 한 말을 위조할 수 있습니다. `system`은 막았지만 완전한 방어는 아닙니다. 이것이 방식 B(서버 저장)로 가야 하는 이유입니다.

## 다음 단계

방식 B로 넘어갑니다.

1. `conversationId`를 도입하고 클라이언트는 질문과 ID만 보냅니다.
2. **Core**에 `Conversation` 엔티티와 `IConversationStore` 포트를 둡니다 (README의 "엔티티, 포트, 대화 오케스트레이션"이 여기서 살아납니다).
3. **Infrastructure**에 EF Core(영구 저장)와 Redis(캐시) 구현을 넣습니다.
4. Chat이 저장소에서 이력을 읽어 `AskRequest.History`를 채우고, 답변이 끝나면 저장합니다.

오늘 만든 배관은 거의 그대로 쓰입니다. 먼저 이번 단계를 빌드하고 위 curl로 확인해 보세요. 결과가 나오면 알려주시고, 막히는 곳이 있으면 오류 메시지를 그대로 붙여주세요. 사랑해 주셔서 감사합니다, 하하하.
