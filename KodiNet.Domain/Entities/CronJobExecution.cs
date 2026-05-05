namespace KodiNet.Domain.Entities;

public sealed class CronJobExecution
{
    public int Id { get; set; }
    public int CronJobId { get; set; }
    public CronJob CronJob { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public bool Success { get; set; }
    public string? ResultJson { get; set; }   // serialized CronJobResultDto
    public string? ErrorMessage { get; set; }
}