namespace WorkOrderDesk.Intake;

public class Suggestion
{
    public string Category { get; init; } = "other";
    public string Urgency { get; init; } = "normal";
    public string Reply { get; init; } = "";
    public string Reason { get; init; } = "";

    // "rules" = first pass was enough.
    // "model" = rules were unsure and the JSON checked out.
    // "fallback" = we asked the model and then discarded the answer.
    public string Source { get; init; } = "rules";

    public bool Confident { get; init; }
}
