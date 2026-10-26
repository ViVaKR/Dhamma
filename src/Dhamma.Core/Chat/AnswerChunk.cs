namespace Dhamma.Core.Chat;

public readonly record struct AnswerChunk(string Text, bool IsThinking = false);
