namespace MOTORFISCALSAPB1.Shared.Contracts;

public class StructureProgressMessage
{
    public string Stage { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public string Status { get; set; } = "running";
    public string? Message { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
