using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface IMailNotificationService
{
    Task SendCronReportAsync(CronJobResultDto result, CancellationToken ct = default);
}