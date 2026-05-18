using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

/// <summary>Restarts the LibreELEC system on all Pis.</summary>
public interface IKodiRebooterJobService
{
    Task<CronJobResultDto> ExecuteAsync(CancellationToken ct = default);
}