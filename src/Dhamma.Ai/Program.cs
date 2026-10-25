using Dhamma.Ai.Services;
using Dhamma.Core.Chat;
using Dhamma.Infrastructure.Ollama;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. gRPC 서버 기능 활성화
builder.Services.AddGrpc();

// 2. Ollama 설정 및 인프라 주입
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));

// OllamaChatClient와 HttpClient 주입 설정!
builder.Services.AddHttpClient<OllamaChatClient>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddScoped<IAnswerStreamer, OllamaAnswerStreamer>();

var app = builder.Build();

app.MapGrpcService<ChatGrpcService>();
app.MapGet("/", () => "Dhamma.Ai gRPC Server is running!");
app.Run();
