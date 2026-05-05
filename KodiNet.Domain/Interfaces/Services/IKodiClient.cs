using KodiNet.Domain.Enums;

namespace KodiNet.Domain.Interfaces.Services;

public record KodiPlayerState(
    bool     IsPlaying,
    int?     PlayerId,
    string?  CurrentFile,
    double   PositionSeconds,
    double   DurationSeconds,
    int      Volume,
    RepeatMode Repeat);

public record KodiSystemInfo(
    string KodiVersion,
    string OsVersion,
    double CpuPercent,
    long   FreeMemoryBytes,
    double TemperatureCelsius,
    long   FreeDiskBytes);

public interface IKodiClient
{
    Task<bool>            PingAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task<KodiPlayerState> GetPlayerStateAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task<KodiSystemInfo>  GetSystemInfoAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task                  PlayFileAsync(string ip, int port, string user, string password, string filePath, CancellationToken ct = default);
    Task                  StopAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task                  SetVolumeAsync(string ip, int port, string user, string password, int volume, CancellationToken ct = default);
    Task                  SetRepeatAsync(string ip, int port, string user, string password, RepeatMode repeat, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListFilesAsync(string ip, int port, string user, string password, string directory, CancellationToken ct = default);
    Task                  DeleteFileAsync(string ip, int port, string user, string password, string filePath, CancellationToken ct = default);
    Task PlayFolderAsync(string ip, int port, string user, string password, string folderPath, CancellationToken ct = default);
    Task TogglePlayPauseAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task RebootAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task SkipNextAsync(string ip, int port, string user, string password, CancellationToken ct = default);
    Task SkipPreviousAsync(string ip, int port, string user, string password, CancellationToken ct = default);
}