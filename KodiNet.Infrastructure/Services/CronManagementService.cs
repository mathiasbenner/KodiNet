using System.Text.Json;
using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

public sealed class CronManagementService(
    IDbContextFactory<AppDbContext> dbFactory) : ICronManagementService
{
    public async Task<IReadOnlyList<CronJobDto>> GetAllJobsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CronJobs.AsNoTracking()
            .Select(j => new CronJobDto(j.Id, j.Name, j.Description, j.Schedule, j.IsEnabled, j.LastRunAt))
            .ToListAsync(ct);
    }

    public async Task<CronJobDto> UpdateJobAsync(UpdateCronJobRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var job = await db.CronJobs.FindAsync([request.Id], ct)
                  ?? throw new KeyNotFoundException($"Job {request.Id} not found.");

        // Validate the cron expression before saving
        try { Cronos.CronExpression.Parse(request.Schedule); }
        catch { throw new ArgumentException($"Invalid cron expression: {request.Schedule}"); }

        job.Schedule = request.Schedule;
        if (request.IsEnabled)
            job.Enable();
        else
            job.Disable();

        await db.SaveChangesAsync(ct);
        return new CronJobDto(job.Id, job.Name, job.Description, job.Schedule, job.IsEnabled, job.LastRunAt);
    }

    public async Task<IReadOnlyList<CronJobExecutionDto>> GetHistoryAsync(int jobId, int limit = 50, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var executions = await db.CronJobExecutions.AsNoTracking()
            .Where(x => x.CronJobId == jobId)
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync(ct);

        return executions.Select(x =>
        {
            CronJobResultDto? result = null;
            if (x.ResultJson is not null)
                try { result = JsonSerializer.Deserialize<CronJobResultDto>(x.ResultJson); } catch { }

            return new CronJobExecutionDto(
                x.Id, x.CronJobId, x.StartedAt, x.FinishedAt,
                x.Success, x.ErrorMessage, result);
        }).ToList();
    }
}