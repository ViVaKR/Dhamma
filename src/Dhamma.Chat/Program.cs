using Dhamma.Chat.Hubs;
using Dhamma.Core.Chat;
using Dhamma.Infrastructure;
using Dhamma.Infrastructure.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(x => x.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// gRPC 클라이언트 + IAnswerStreamer + IConversationStore 를 한 번에 등록
builder.Services.AddDhammaChatInfrastructure(
    builder.Configuration["Services:Ai:Address"] ?? "http://localhost:5100");
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddScoped<ChatOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.Run();
