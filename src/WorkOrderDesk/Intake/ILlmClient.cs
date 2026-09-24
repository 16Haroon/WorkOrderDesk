namespace WorkOrderDesk.Intake;

// Returns the raw assistant text, or null when the call should be ignored.
public interface ILlmClient
{
    Task<string?> CompleteJsonAsync(string body, CancellationToken cancellationToken);
}
