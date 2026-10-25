namespace Dhamma.Infrastructure.Ollama;

public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string SystemPrompt { get; set; } = "";
}
