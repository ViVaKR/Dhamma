namespace Dhamma.Core.Chat;

public enum ChatRole { User, Assistant }

public sealed record ChatTurn(ChatRole Role, string Content);
