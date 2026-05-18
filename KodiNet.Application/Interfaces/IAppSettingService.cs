using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

public interface IAppSettingService
{
    Task<IReadOnlyList<AppSettingDto>> GetMailSettingsAsync(CancellationToken ct = default);
    Task UpdateSettingAsync(UpdateAppSettingRequest request, CancellationToken ct = default);
    Task<string?> GetAsync(string key, CancellationToken ct = default);
}