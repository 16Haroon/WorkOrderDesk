using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WorkOrderDesk.Intake;

// One POST to /chat/completions. Returns the message content, or null if
// anything about the round trip looks off. The pipeline decides what to do then.
public class OpenAiCompatibleClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _options;
    private readonly ILogger<OpenAiCompatibleClient> _logger;

    public OpenAiCompatibleClient(
        HttpClient http,
        IOptions<LlmOptions> options,
        ILogger<OpenAiCompatibleClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> CompleteJsonAsync(string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        var payload = new
        {
            model = _options.Model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = body }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, Combine("chat/completions"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Model call failed with {Status}.", (int)response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(raw);
            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return null;
            }

            return choices[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Model call did not return usable JSON.");
            return null;
        }
    }

    private Uri Combine(string path)
    {
        var root = _options.BaseUrl.TrimEnd('/') + "/";
        return new Uri(new Uri(root), path);
    }

    private const string SystemPrompt =
        "You sort building maintenance requests. Reply with JSON only, keys: " +
        "category, urgency, reply, reason. " +
        "category must be one of plumbing, electrical, hvac, appliance, other. " +
        "urgency must be one of emergency, urgent, normal, low. " +
        "reply is one or two sentences for the resident. reason is one short sentence.";
}
