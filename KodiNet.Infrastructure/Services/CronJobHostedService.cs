using Cronos;
using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// Moteur d'exécution des jobs cron.
///
/// Architecture : tick every 30 seconds.
/// At each tick, all enabled jobs are reloaded from the database.
/// A job is triggered if the next occurrence calculated from its
/// last execution (or from the epoch if never run) is in the past.
///
/// Advantages vs Task.Delay(nextOccurrence - now) :
///   - Schedule changes are taken into account within 30s
///   - Immediate activation/deactivation
///   - Resistant to restarts (LastRunAt persisted in the database)
/// </summary>
public sealed class CronJobHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CronJobHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("CronJobHostedService started (tick every {Interval}s)",
            TickInterval.TotalSeconds);

        using var timer = new PeriodicTimer(TickInterval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await ProcessDueJobsAsync(ct);
        }
    }

    private async Task ProcessDueJobsAsync(CancellationToken ct)
    {
        // Each tick creates its own scope to access Scoped services
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<CronJob> jobs;
        try
        {
            jobs = await db.CronJobs.Where(j => j.IsEnabled).ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CronJobHostedService — unable to read jobs from the database");
            return;
        }

        foreach (var job in jobs)
        {
            if (ct.IsCancellationRequested) break;
            if (IsDue(job))
                await RunJobAsync(job, scope, ct);
        }
    }

    /// <summary>
    /// A job is due if the next occurrence calculated from <see cref="CronJob.LastRunAt"/>
    /// is in the past or now.
    /// If the job has never run (LastRunAt == null), we use (now - 1 tick) as
    /// the reference to avoid triggering it immediately at startup.
    /// </summary>
    private static bool IsDue(CronJob job)
    {
        CronExpression expr;
        try { expr = CronExpression.Parse(job.Schedule); }
        catch { return false; } // invalid expression — ignore

        var from = job.LastRunAt ?? DateTime.UtcNow.Subtract(TickInterval);
        var next = expr.GetNextOccurrence(new DateTime(from.Ticks, DateTimeKind.Utc), TimeZoneInfo.Utc);
        return next.HasValue && next.Value <= DateTime.UtcNow;
    }

    private async Task RunJobAsync(CronJob job, IServiceScope scope, CancellationToken ct)
    {
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mailService = scope.ServiceProvider.GetRequiredService<IMailNotificationService>();

        logger.LogInformation("CronJob — starting « {JobName} »", job.Name);

        // Mark LastRunAt immediately to avoid double triggering
        // if the execution takes more than 30s (next tick)
        job.LastRunAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var execution = new CronJobExecution
        {
            CronJobId = job.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CronJobExecutions.Add(execution);
        await db.SaveChangesAsync(ct);

        try
        {
            CronJobResultDto result = job.Name switch
            {
                "KodiRebooter" => await scope.ServiceProvider
                          .GetRequiredService<IKodiRebooterJobService>()
                          .ExecuteAsync(ct),
                _ => await scope.ServiceProvider            // KodiRestarter and any future jobs
             .GetRequiredService<IKodiRestarterJobService>()
             .ExecuteAsync(ct),
            };

            execution.FinishedAt = DateTime.UtcNow;
            execution.Success = result.PisFailed == 0;
            execution.ResultJson = JsonSerializer.Serialize(result);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "CronJob « {JobName} » completed — {Restarted} restarted, {Failed} failed",
                job.Name, result.PisRestarted, result.PisFailed);

            await mailService.SendCronReportAsync(result, ct);
        }
        catch (Exception ex)
        {
            execution.FinishedAt = DateTime.UtcNow;
            execution.Success = false;
            execution.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
            logger.LogError(ex, "CronJob « {JobName} » — unexpected error", job.Name);
        }
    }
}