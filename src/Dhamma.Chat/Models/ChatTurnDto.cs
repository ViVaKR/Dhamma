// Dhamma.Chat/Models/ChatTurnDto.cs
using Dhamma.Contracts.Ai;

namespace Dhamma.Chat.Models;

public sealed record ChatTurnDto(string Role, string Content)
{
    public ChatMesage ToProto() => new() { Role = Role, Content = Content };
}
