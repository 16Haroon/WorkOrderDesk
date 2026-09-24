using System.ComponentModel.DataAnnotations.Schema;
using WorkOrderDesk.Intake;

namespace WorkOrderDesk.Data;

// One request through the desk. Suggested* is the machine guess.
// Final* is what a person saved. Both stay on the row.
public class WorkRequest
{
    public int Id { get; set; }

    public string Body { get; set; } = "";

    // UTC. SQLite can ORDER BY DateTime. It cannot ORDER BY DateTimeOffset.
    public DateTime CreatedAt { get; set; }

    public string SuggestedCategory { get; set; } = "other";
    public string SuggestedUrgency { get; set; } = "normal";
    public string SuggestedReply { get; set; } = "";
    public string SuggestedReason { get; set; } = "";

    // "rules", "model", or "fallback"
    public string SuggestionSource { get; set; } = "rules";
    public bool SuggestionConfident { get; set; }

    public string? FinalCategory { get; set; }
    public string? FinalUrgency { get; set; }
    public string? FinalReply { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Convenience for the pages. Not a column.
    [NotMapped]
    public bool IsReviewed => ReviewedAt is not null;

    public static WorkRequest FromIntake(string body, Suggestion suggestion) =>
        new()
        {
            Body = body,
            CreatedAt = DateTime.UtcNow,
            SuggestedCategory = suggestion.Category,
            SuggestedUrgency = suggestion.Urgency,
            SuggestedReply = suggestion.Reply,
            SuggestedReason = suggestion.Reason,
            SuggestionSource = suggestion.Source,
            SuggestionConfident = suggestion.Confident
        };

    public void ApplyDecision(string category, string urgency, string reply)
    {
        FinalCategory = category;
        FinalUrgency = urgency;
        FinalReply = reply;
        ReviewedAt = DateTime.UtcNow;
    }
}
