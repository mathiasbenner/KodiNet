using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

/// <summary>
/// Orchestration of the cron job: checking each Pi,
/// restarting if necessary, returning a report.
/// </summary>
public interface IKodiRestarterJobService
{
    Task<CronJobResultDto> ExecuteAsync(CancellationToken ct = default);
}