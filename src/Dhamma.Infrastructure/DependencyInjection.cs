// Dhamma.Infrastructure/DependencyInjection.cs
using Dhamma.Contracts.Ai;
using Dhamma.Core.Chat;
using Dhamma.Infrastructure.AiGateway;
using Dhamma.Infrastructure.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace Dhamma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDhammaChatInfrastructure(
        this IServiceCollection services, string aiAddress)
    {
        services.AddGrpcClient<ChatService.ChatServiceClient>(o => o.Address = new Uri(aiAddress));
        services.AddScoped<IAnswerStreamer, GrpcAnswerStreamer>();
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();
        return services;
    }
}
