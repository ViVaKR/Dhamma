// OllamaChatClient.cs
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Dhamma.Core.Chat;
using Microsoft.Extensions.Options;

namespace Dhamma.Infrastructure.Ollama;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options)
{
    public async IAsyncEnumerable<AnswerChunk> StreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var body = new
        {
            model = options.Value.Model,
            stream = true,
            think = options.Value.Think,
            messages = new object[]
            {
                new { role = "system", content = options.Value.SystemPrompt },
                new { role = "user",   content = question }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(body)
        };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Length == 0) continue;

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var m))
            {
                if (m.TryGetProperty("thinking", out var t) &&
                    t.GetString() is { Length: > 0 } thinking)
                    yield return new AnswerChunk(thinking, IsThinking: true);

                if (m.TryGetProperty("content", out var c) &&
                    c.GetString() is { Length: > 0 } text)
                    yield return new AnswerChunk(text);
            }

            if (root.TryGetProperty("done", out var d) && d.GetBoolean())
                yield break;
        }
    }
}
