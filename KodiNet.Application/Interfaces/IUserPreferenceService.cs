using KodiNet.Application.DTOs;

namespace KodiNet.Application.Interfaces;

/// <summary>
/// Service for user preferences
/// </summary>
public interface IUserPreferenceService
{
    Task<AppThemeDto?>  GetSelectedThemeAsync(string userOid, CancellationToken ct = default);
    Task                SetSelectedThemeAsync(string userOid, int? themeId, CancellationToken ct = default);
    Task<string?>       GetLanguageAsync(string userOid, CancellationToken ct = default);
    Task                SetLanguageAsync(string userOid, string languageCode, CancellationToken ct = default);
}