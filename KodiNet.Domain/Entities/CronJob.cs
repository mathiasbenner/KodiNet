namespace KodiNet.Domain.Entities;

public sealed class CronJob
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // "KodiRestarter"
    public string Description { get; set; } = string.Empty;
    public string Schedule { get; set; } = "0 8 * * *";    // cron expression
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CronJobExecution> Executions { get; set; } = [];
}