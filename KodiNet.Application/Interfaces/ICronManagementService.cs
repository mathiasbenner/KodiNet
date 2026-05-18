using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface ICronManagementService
{
    Task<IReadOnlyList<CronJobDto>> GetAllJobsAsync(CancellationToken ct = default);
    Task<CronJobDto> UpdateJobAsync(UpdateCronJobRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<CronJobExecutionDto>> GetHistoryAsync(int jobId, int limit = 50, CancellationToken ct = default);
}