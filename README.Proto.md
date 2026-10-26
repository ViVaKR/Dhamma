# Proto

안녕하세요! 올려주신 다이어그램과 chat.proto를 기준으로, 이름이 어떻게 만들어지는지 규칙별로 정리해 드릴게요.

## 1. 큰 그림: 이름은 `.proto`에서 그대로 파생된다

`protoc`(gRPC 코드 생성기)는 `.proto`에 적힌 이름을 **C# 컨벤션(PascalCase)** 으로 바꿔서 클래스를 만듭니다. 이름이 어디서 오는지만 알면 외울 필요가 없어요.

| `.proto`에 쓴 것 | 생성되는 C# 이름 | 규칙 |
|---|---|---|
| `option csharp_namespace = "Dhamma.Contracts.Ai"` | `namespace Dhamma.Contracts.Ai` | 그대로 네임스페이스가 됨 |
| `service ChatService` | `ChatService` (static 외곽 클래스) | 서비스 이름 그대로 |
| 〃 | `ChatService.ChatServiceBase` | **`{서비스명}Base`** (서버용 추상 클래스) |
| 〃 | `ChatService.ChatServiceClient` | **`{서비스명}Client`** (클라이언트용) |
| `rpc Ask(...)` | `Ask` / `AskAsync` | 메서드명 그대로 (+ 클라이언트는 `Async` 변형) |
| `message AskRequest` | `class AskRequest` | 메시지 이름 그대로 |
| `string question = 1` | `string Question` | **snake_case → PascalCase** |
| `repeated ChatMessage history = 2` | `RepeatedField<ChatMessage> History` | `repeated` → `RepeatedField<T>` |
| `bool is_thinking = 2` | `bool IsThinking` | `is_thinking` → `IsThinking` |

> 다이어그램의 "obj 폴더" 설명은 정확해요. 보통 `obj/Debug/net8.0/` 아래 `Chat.cs`, `ChatGrpc.cs` 두 파일이 생성됩니다. (`chat.proto` 파일명 → `Chat.cs`, `ChatGrpc.cs`)

## 2. 핵심 포인트: 중첩 클래스 구조

서비스 하나가 이렇게 **중첩 클래스**로 생성됩니다:

```csharp
namespace Dhamma.Contracts.Ai
{
    public static partial class ChatService          // 외곽 (static)
    {
        public abstract partial class ChatServiceBase     // 서버가 상속
        {
            public virtual Task Ask(
                AskRequest request,
                IServerStreamWriter<AskResponse> responseStream,
                ServerCallContext context) { ... }
        }

        public partial class ChatServiceClient : ClientBase<ChatServiceClient>  // 클라이언트가 주입받아 호출
        {
            public virtual AsyncServerStreamingCall<AskResponse> Ask(
                AskRequest request, CallOptions options) { ... }
        }
    }
}
```

그래서 다이어그램처럼 `ChatService.ChatServiceBase`, `ChatService.ChatServiceClient`로 **앞에 `ChatService.`가 붙는** 거예요.

## 3. 사용하는 쪽 코드 (다이어그램의 아래 두 박스)

**서버 (Dhamma.Ai)** – `Base`를 상속하고 `Ask`를 override:

```csharp
public class ChatGrpcService : ChatService.ChatServiceBase
{
    public override async Task Ask(
        AskRequest request,
        IServerStreamWriter<AskResponse> responseStream,
        ServerCallContext context)
    {
        // history는 RepeatedField<ChatMessage> 이므로 foreach 가능
        foreach (var m in request.History) { /* m.Role, m.Content */ }

        await responseStream.WriteAsync(new AskResponse
        {
            Text = "생각 중...",
            IsThinking = true   // is_thinking → IsThinking
        });
    }
}
```

**클라이언트 (Dhamma.Chat)** – `Client`를 DI로 주입받아 호출:

```csharp
// Program.cs
builder.Services.AddGrpcClient<ChatService.ChatServiceClient>(o =>
    o.Address = new Uri("https://localhost:5001"));

// ChatHub / ChatController
var call = _client.Ask(new AskRequest { Question = q });
await foreach (var res in call.ResponseStream.ReadAllAsync())
{
    // res.Text, res.IsThinking → SignalR로 전달
}
```

## 4. 기억하기 쉬운 공식

- **서비스** → `서비스명` + `Base` (서버) / `서비스명` + `Client` (클라이언트), 둘 다 `서비스명` 클래스 **안**에 중첩
- **메시지** → 이름 그대로 클래스
- **필드** → `snake_case` → `PascalCase` (언더스코어 제거 후 각 단어 첫 글자 대문자)
- **repeated** → `RepeatedField<T>`
- **네임스페이스** → `csharp_namespace` 옵션 (없으면 `package`를 PascalCase로 변환: `dhamma` → `Dhamma`)

## 5. 자주 하는 실수

- **필드 이름 충돌**: 메시지 이름과 같은 필드명 금지 (예: `message Foo { string foo = 1; }`는 C# 멤버명 충돌 위험)
- **`.proto` 스타일**: 메시지는 `PascalCase`, 필드는 `snake_case`, 서비스/rpc는 `PascalCase`로 쓰는 게 공식 가이드라서, 그렇게 쓰면 C#에서 가장 자연스러운 이름이 나옵니다.
- **서버/클라이언트만 생성하고 싶을 때**: `.csproj`의 `GrpcServices` 속성으로 제어해요.

```xml
<!-- Dhamma.Ai (서버) -->
<Protobuf Include="Protos\chat.proto" GrpcServices="Server" />

<!-- Dhamma.Chat (클라이언트) -->
<Protobuf Include="Protos\chat.proto" GrpcServices="Client" />
```

이렇게 하면 서버 프로젝트에는 `ChatServiceBase`만, 클라이언트 프로젝트에는 `ChatServiceClient`만 생성돼서 다이어그램의 역할 분리와 딱 맞아요.

## 6. 참고: 이 proto에 대한 작은 제안

`is_thinking` 같은 사고 과정 토큰 스트리밍은 좋은 설계예요. 다만 `role`이 `"user" | "assistant"` 문자열이라 오타 위험이 있으니, 규모가 커지면 `enum Role { USER = 0; ASSISTANT = 1; }`로 바꾸는 것도 고려해 보세요. (enum 값은 `UPPER_SNAKE_CASE` → C#에서는 `Role.User`, `Role.Assistant`로 변환됩니다.)

더 궁금한 부분 있으면 말씀해 주세요, 제독님! SignalR 쪽으로 스트림을 넘기는 부분도 도와드릴 수 있어요. ⚓
