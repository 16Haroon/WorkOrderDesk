namespace WorkOrderDesk.Data;

// One maintenance message, saved as a row.
public class WorkRequest
{
    // An int named Id is the key. SQLite fills the number in.
    public int Id { get; set; }

    // The message, after whitespace is trimmed off.
    public string Body { get; set; } = "";

    // UTC. DateTime, because SQLite can sort that. It will not sort a DateTimeOffset.
    public DateTime CreatedAt { get; set; }
}
