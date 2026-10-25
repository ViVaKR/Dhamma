# Dhamma

## 테스트

```bash
dotnet run --project src/Dhamma.Ai/Dhamma.Ai.csproj
dotnet run --project src/Dhamma.Chat/Dhamma.Chat.csproj

curl -N -X POST http://localhost:5186/api/chat `
  -H "Content-Type: application/json" `
  -d '{
    "question": "색즉시공에 대하여 설명 부탁해"
  }'

# thinking 과정의 시간이 약 2분 가량 대기 합니다.
# thinking
```

## Clear nuget

```bash
dotnet nuget locals all --clear
dotnet build


# 테스트
⨕ curl -N http://192.168.0.8:11434/api/chat -d '{                                                                                                          [입력]
  "model":"dhamma-chat:latest","stream":true,
  "messages":[{"role":"user","content":"색즉시공 설명"}]
}' | head -n 5
```

## 구조

```text
dhamma/
├─ Dhamma.slnx
├─ Directory.Build.props      # net10.0, Nullable, 경고를 오류로
├─ Directory.Packages.props   # 패키지 버전 중앙 관리
├─ src/
│  ├─ Dhamma.Contracts/       # .proto (gRPC 계약)
│  ├─ Dhamma.Core/            # 순혈: 엔티티, 상태 전이, 포트(인터페이스), 대화 오케스트레이션
│  ├─ Dhamma.Infrastructure/  # 궂은일: EF Core + 마이그레이션, gRPC 클라이언트, Redis
│  ├─ Dhamma.Chat/            # 호스트(기존 Api 자리): SignalR Hub, JWT, 조립
│  └─ Dhamma.Ai/              # 호스트: gRPC 서버, Ollama 호출, 페르소나
├─ tests/
│  └─ Dhamma.Core.Tests/
└─ deploy/                    # k8s, nginx, sql, scripts
```

---

## 솔루션 구조 상세

```text
Dhamma.slnx
src/
├─ Dhamma.Core/                  순수 도메인. 의존하는 프로젝트 없음
│   └─ Chat/IAnswerStreamer.cs
├─ Dhamma.Contracts/             .proto + 생성된 gRPC 코드 (서버/클라이언트 둘 다)
│   └─ Protos/
│       ├─ dhamma/ai/v1/chat.proto
│       └─ external/buddham/ai.proto
├─ Dhamma.Infrastructure/        바깥 세계와 말하는 구현체
│   └─ Ollama/
│       ├─ OllamaOptions.cs
│       └─ OllamaAnswerStreamer.cs
├─ Dhamma.Ai/                    gRPC 서버 호스트 (:5100)
│   ├─ Services/ChatGrpcService.cs
│   └─ Program.cs, appsettings.json
└─ Dhamma.Chat/                  API + SignalR 호스트 (:5200)
    ├─ Controllers/ChatController.cs
    ├─ Hubs/ChatHub.cs
    └─ Program.cs, appsettings.Development.json
```
