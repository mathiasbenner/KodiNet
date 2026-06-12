namespace KodiNet.Domain.Entities;

/// <summary>Cron jobs are disabled by default</summary>
public sealed class CronJob
{
    public int Id { get; set; }
    public string Name { get; set; } = "";   // "KodiRestarter"
    public string Description { get; set; } = "";
    public string Schedule { get; set; } = "0 8 * * *";    // cron expression
    public bool IsEnabled { get; private set; } = false;
    public DateTime? LastRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CronJobExecution> Executions { get; set; } = [];

    public void Enable()
    {
        if (IsEnabled)
            throw new InvalidOperationException("Already enabled");
        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled)
            throw new InvalidOperationException("Already disabled");
        IsEnabled = false;
    }
}