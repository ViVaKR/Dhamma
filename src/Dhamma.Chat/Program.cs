using Dhamma.Chat.Hubs;
using Dhamma.Contracts.Ai;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(x => x.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddGrpcClient<ChatService.ChatServiceClient>(o => o.Address = new Uri("http://localhost:5100"));
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub");
app.Run();
