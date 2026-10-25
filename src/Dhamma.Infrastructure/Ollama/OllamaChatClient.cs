using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Dhamma.Infrastructure.Ollama;

public class OllamaChatClient(HttpClient http, IOptions<OllamaOptions> options)
{
    public async IAsyncEnumerable<string> StreamAsync(
        string question,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var body = new
        {
            model = options.Value.Model,
            stream = true,
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

        // 헤더만 받고 바로 스트림 읽기 시작 (없으면 전체가 끝날 때까지 버퍼링됨)
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));

        while (await reader.ReadLineAsync(ct) is { } line) // Ollama 는 한 줄에 JSON 하나(NDJSON)
        {
            if (line.Length == 0) continue;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var m) &&
                m.TryGetProperty("content", out var c) &&
                c.GetString() is { Length: > 0 } text)
                yield return text;
            if (root.TryGetProperty("done", out var d) && d.GetBoolean())
                yield break;
        }

    }
}
