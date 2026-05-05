using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Enums;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KodiNet.Infrastructure.Services;

/// <summary>
/// For each reachable Pi:<br />
///   - Idle (no playback) → plays the video folder in repeat mode<br />
///   - Playing → does nothing<br />
///   - Offline / Incompatible → reports in the result
/// </summary>
public sealed class KodiRestarterJobService(
    IDbContextFactory<AppDbContext> dbFactory,
    IKodiClient kodiClient,
    ICredentialEncryption encryption,
    ILogger<KodiRestarterJobService> logger) : IKodiRestarterJobService
{
    public async Task<CronJobResultDto> ExecuteAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var pis = await db.RaspberryPis.AsNoTracking().ToListAsync(ct);

        var details  = new List<CronPiResultDto>();
        var restarted = 0;
        var failed    = 0;

        foreach (var pi in pis)
        {
            var kodiUser = encryption.Decrypt(pi.KodiUserEncrypted);
            var kodiPass = encryption.Decrypt(pi.KodiPasswordEncrypted);

            try
            {
                // Vérifier la disponibilité
                var alive = await kodiClient.PingAsync(
                    pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);

                if (!alive)
                {
                    details.Add(new(pi.Id, pi.Name, "Offline", false, "Pi unreachable"));
                    continue;
                }

                // Récupérer l'état du lecteur
                var state = await kodiClient.GetPlayerStateAsync(
                    pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);

                if (state.IsPlaying)
                {
                    details.Add(new(pi.Id, pi.Name, "Playing", false, null));
                    continue;
                }

                // Idle ou arrêté → relancer le dossier en répétition All
                var previousStatus = state.CurrentFile is null ? "Stopped" : "Idle";

                await kodiClient.PlayFolderAsync(
                    pi.IpAddress, pi.KodiPort, kodiUser, kodiPass,
                    pi.VideoFolderPath, ct);

                await kodiClient.SetRepeatAsync(
                    pi.IpAddress, pi.KodiPort, kodiUser, kodiPass,
                    RepeatMode.All, ct);

                details.Add(new(pi.Id, pi.Name, previousStatus, true, null));
                restarted++;

                logger.LogInformation(
                    "CronJob — Pi {Name} ({Ip}) restarted from state {Status}",
                    pi.Name, pi.IpAddress, previousStatus);
            }
            catch (Exception ex)
            {
                details.Add(new(pi.Id, pi.Name, "Unknown", false, ex.Message));
                failed++;
                logger.LogWarning(ex, "CronJob — error on Pi {Name}", pi.Name);
            }
        }

        return new CronJobResultDto(
            DateTime.UtcNow, pis.Count, restarted, failed, details);
    }
}