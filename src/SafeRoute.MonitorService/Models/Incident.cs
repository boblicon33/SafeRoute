namespace SafeRoute.MonitorService.Models;

public class Incident
{
    public Guid Id { get; set; }
    public DateTime DetectedAt { get; set; }
    public int FailureCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AlertSent { get; set; }
}