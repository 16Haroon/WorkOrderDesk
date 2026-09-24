namespace WorkOrderDesk.Intake;

// Bound from the "Llm" section in appsettings, user-secrets, or Llm__* env vars.
public class LlmOptions
{
    public const string SectionName = "Llm";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";

    // Empty means skip the model. Never commit a real value.
    public string? ApiKey { get; set; }
}
