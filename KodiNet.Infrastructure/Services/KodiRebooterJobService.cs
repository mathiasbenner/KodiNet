using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// Reboots the LibreELEC system of all Pis.
/// </summary>
public sealed class KodiRebooterJobService(
    IDbContextFactory<AppDbContext> dbFactory,
    IKodiClient kodiClient,
    ICredentialEncryption encryption,
    ILogger<KodiRebooterJobService> logger) : IKodiRebooterJobService
{
    public async Task<CronJobResultDto> ExecuteAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pis = await db.RaspberryPis.AsNoTracking().ToListAsync(ct);
        var details   = new List<CronPiResultDto>();
        var restarted = 0;
        var failed    = 0;

        foreach (var pi in pis)
        {
            var kodiUser = encryption.Decrypt(pi.KodiUserEncrypted);
            var kodiPass = encryption.Decrypt(pi.KodiPasswordEncrypted);
            try
            {
                var alive = await kodiClient.PingAsync(pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);
                if (!alive) { details.Add(new(pi.Id, pi.Name, "Offline", false, "Pi unreachable")); continue; }

                await kodiClient.RebootAsync(pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);
                details.Add(new(pi.Id, pi.Name, "Online", true, null));
                restarted++;
                logger.LogInformation("KodiRebooter — Pi {Name} rebooted", pi.Name);
            }
            catch (Exception ex)
            {
                details.Add(new(pi.Id, pi.Name, "Unknown", false, ex.Message));
                failed++;
                logger.LogWarning(ex, "KodiRebooter — error on Pi {Name}", pi.Name);
            }
        }

        return new CronJobResultDto(DateTime.UtcNow, pis.Count, restarted, failed, details);
    }
}