namespace WorkOrderDesk.Intake;

// Closed lists. The validator and the review dropdowns share these
// so a model cannot invent a fifth urgency.
public static class Labels
{
    public static readonly string[] Categories =
    [
        "plumbing",
        "electrical",
        "hvac",
        "appliance",
        "other"
    ];

    public static readonly string[] Urgencies =
    [
        "emergency",
        "urgent",
        "normal",
        "low"
    ];

    public static bool IsCategory(string? value) =>
        value is not null && Categories.Contains(value, StringComparer.OrdinalIgnoreCase);

    public static bool IsUrgency(string? value) =>
        value is not null && Urgencies.Contains(value, StringComparer.OrdinalIgnoreCase);
}
