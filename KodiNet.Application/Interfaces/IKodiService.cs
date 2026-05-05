using KodiNet.Application.DTOs;
using KodiNet.Domain.Enums;

namespace KodiNet.Application.Interfaces;

public interface IKodiService
{
    Task<PiStatusDto>                GetStatusAsync(int piId, CancellationToken ct = default);
    Task<PlayerStateDto>             GetPlayerStateAsync(int piId, CancellationToken ct = default);
    Task                             PlayFileAsync(int piId, string filePath, CancellationToken ct = default);
    Task                             StopAsync(int piId, CancellationToken ct = default);
    Task                             SetVolumeAsync(int piId, int volume, CancellationToken ct = default);
    Task                             SetRepeatAsync(int piId, RepeatMode repeat, CancellationToken ct = default);
    Task<IReadOnlyList<FileItemDto>> ListPiFilesAsync(int piId, string? subPath = null, CancellationToken ct = default);
    Task                             DeletePiFileAsync(int piId, string remotePath, CancellationToken ct = default);
    Task                             RenamePiFileAsync(int piId, string remotePath, string newName, CancellationToken ct = default);
    Task                             UploadToPiAsync(int piId, Stream content, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default);
    Task PlayVideoFolderAsync(int piId, string folderPath, CancellationToken ct = default);
    Task TogglePlayPauseAsync(int piId, CancellationToken ct = default);
    Task SkipNextAsync(int piId, CancellationToken ct = default);
    Task SkipPreviousAsync(int piId, CancellationToken ct = default);
    Task RebootAsync(int piId, CancellationToken ct = default);
}