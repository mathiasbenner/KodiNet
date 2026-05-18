using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Enums;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

public sealed class KodiService(
    IDbContextFactory<AppDbContext> dbFactory,
    ICredentialEncryption           encryption,
    IKodiClient                     kodiClient,
    ISftpFileClient                 sftpClient) : IKodiService
{
    public async Task<PiStatusDto> GetStatusAsync(int piId, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (kodiUser, kodiPass) = DecryptKodi(pi);

        var reachable = await kodiClient.PingAsync(pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);
        if (!reachable)
            return new PiStatusDto(piId, PiStatus.Offline, null, 0, 0, 0, 0);

        try
        {
            var info   = await kodiClient.GetSystemInfoAsync(pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);
            var player = await kodiClient.GetPlayerStateAsync(pi.IpAddress, pi.KodiPort, kodiUser, kodiPass, ct);
            var status = player.IsPlaying ? PiStatus.Playing : PiStatus.Idle;

            return new PiStatusDto(piId, status,
                info.KodiVersion, info.CpuPercent,
                info.FreeMemoryBytes, info.TemperatureCelsius, info.FreeDiskBytes);
        }
        catch
        {
            // L'IP répond mais ce n'est pas Kodi
            return new PiStatusDto(piId, PiStatus.Incompatible, null, 0, 0, 0, 0);
        }
    }

    public async Task<PlayerStateDto> GetPlayerStateAsync(int piId, CancellationToken ct = default)
    {
        var pi          = await GetPiOrThrowAsync(piId, ct);
        var (u, p)      = DecryptKodi(pi);
        var state       = await kodiClient.GetPlayerStateAsync(pi.IpAddress, pi.KodiPort, u, p, ct);
        return new PlayerStateDto(
            state.IsPlaying, state.CurrentFile,
            state.PositionSeconds, state.DurationSeconds,
            state.Volume, state.Repeat);
    }

    public async Task PlayFileAsync(int piId, string filePath, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.PlayFileAsync(pi.IpAddress, pi.KodiPort, u, p, filePath, ct);
    }

    public async Task StopAsync(int piId, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.StopAsync(pi.IpAddress, pi.KodiPort, u, p, ct);
    }

    public async Task SetVolumeAsync(int piId, int volume, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.SetVolumeAsync(pi.IpAddress, pi.KodiPort, u, p, volume, ct);
    }

    public async Task SetRepeatAsync(int piId, RepeatMode repeat, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.SetRepeatAsync(pi.IpAddress, pi.KodiPort, u, p, repeat, ct);
    }

    public async Task<IReadOnlyList<FileItemDto>> ListPiFilesAsync(int piId, string? subPath = null, CancellationToken ct = default)
    {
        var pi         = await GetPiOrThrowAsync(piId, ct);
        var (u, p)     = DecryptSsh(pi);
        var remotePath = subPath ?? pi.VideoFolderPath;
        var entries    = await sftpClient.ListAsync(pi.IpAddress, pi.SshPort, u, p, remotePath, ct);

        return entries
            .Select(e => new FileItemDto(e.FullPath, e.Name, e.IsDirectory, e.SizeBytes, e.LastModified, null))
            .ToList();
    }

    public async Task DeletePiFileAsync(int piId, string remotePath, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptSsh(pi);
        await sftpClient.DeleteAsync(pi.IpAddress, pi.SshPort, u, p, remotePath, ct);
    }

    public async Task RenamePiFileAsync(int piId, string remotePath, string newName, CancellationToken ct = default)
    {
        var pi      = await GetPiOrThrowAsync(piId, ct);
        var (u, p)  = DecryptSsh(pi);
        var dir     = Path.GetDirectoryName(remotePath) ?? "/";
        var newPath = $"{dir}/{newName}";
        await sftpClient.RenameAsync(pi.IpAddress, pi.SshPort, u, p, remotePath, newPath, ct);
    }

    public async Task UploadToPiAsync(int piId, Stream content, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var pi     = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptSsh(pi);
        await sftpClient.UploadStreamAsync(pi.IpAddress, pi.SshPort, u, p, content, remotePath, progress, ct);
    }

    public async Task RebootAsync(int piId, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        try { await kodiClient.RebootAsync(pi.IpAddress, pi.KodiPort, u, p, ct); }
        catch { /* connexion coupée par le reboot — normal */ }
    }
    public async Task PlayVideoFolderAsync(int piId, string folderPath, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.PlayFolderAsync(pi.IpAddress, pi.KodiPort, u, p, folderPath, ct);
    }

    public async Task TogglePlayPauseAsync(int piId, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.TogglePlayPauseAsync(pi.IpAddress, pi.KodiPort, u, p, ct);
    }

    public async Task SkipNextAsync(int piId, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.SkipNextAsync(pi.IpAddress, pi.KodiPort, u, p, ct);
    }

    public async Task SkipPreviousAsync(int piId, CancellationToken ct = default)
    {
        var pi = await GetPiOrThrowAsync(piId, ct);
        var (u, p) = DecryptKodi(pi);
        await kodiClient.SkipPreviousAsync(pi.IpAddress, pi.KodiPort, u, p, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<RaspberryPi> GetPiOrThrowAsync(int id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.RaspberryPis.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
               ?? throw new KeyNotFoundException($"Pi {id} not found.");
    }

    private (string user, string password) DecryptKodi(RaspberryPi pi) =>
        (encryption.Decrypt(pi.KodiUserEncrypted), encryption.Decrypt(pi.KodiPasswordEncrypted));

    private (string user, string password) DecryptSsh(RaspberryPi pi) =>
        (encryption.Decrypt(pi.SshUserEncrypted), encryption.Decrypt(pi.SshPasswordEncrypted));
}
