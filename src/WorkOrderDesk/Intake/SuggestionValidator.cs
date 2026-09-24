using System.Text.Json;

namespace WorkOrderDesk.Intake;

// Last gate before anything from the model (or a form post) is stored.
public class SuggestionValidator
{
    // Do not name this TryParse. Minimal APIs treat that as a binder and the
    // app will not start.
    public bool TryReadJson(string? json, out Suggestion? suggestion)
    {
        suggestion = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        var trimmed = json.Trim();

        // Chat models sometimes wrap the object in a markdown fence.
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return false;
            }

            trimmed = trimmed[start..(end + 1)];
        }

        LlmJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<LlmJson>(trimmed, JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed is null)
        {
            return false;
        }

        return TryNormalize(parsed.Category, parsed.Urgency, parsed.Reply, parsed.Reason, "model", out suggestion);
    }

    // A person only edits category, urgency, and the reply. Reason already exists
    // on the suggestion; we do not ask them to rewrite it.
    public bool TryDecision(string? category, string? urgency, string? reply, out Suggestion? suggestion)
    {
        return TryNormalize(category, urgency, reply, "Saved by the desk.", "rules", out suggestion);
    }

    public bool TryNormalize(
        string? category,
        string? urgency,
        string? reply,
        string? reason,
        string source,
        out Suggestion? suggestion)
    {
        suggestion = null;

        category = category?.Trim().ToLowerInvariant();
        urgency = urgency?.Trim().ToLowerInvariant();
        reply = reply?.Trim();
        reason = reason?.Trim();

        if (!Labels.IsCategory(category) || !Labels.IsUrgency(urgency))
        {
            return false;
        }

        if (string.IsNullOrEmpty(reply) || reply.Length > 500)
        {
            return false;
        }

        if (string.IsNullOrEmpty(reason) || reason.Length > 300)
        {
            return false;
        }

        suggestion = new Suggestion
        {
            Category = category!,
            Urgency = urgency!,
            Reply = reply,
            Reason = reason,
            Source = source,
            Confident = true
        };

        return true;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class LlmJson
    {
        public string? Category { get; set; }
        public string? Urgency { get; set; }
        public string? Reply { get; set; }
        public string? Reason { get; set; }
    }
}
