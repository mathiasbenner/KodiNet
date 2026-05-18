using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

public sealed class UserPreferenceService(
    IDbContextFactory<AppDbContext> dbFactory,
    IThemePaletteSerializer<ThemePaletteDto> themePaletteSerializer) : IUserPreferenceService
{
    public async Task<AppThemeDto?> GetSelectedThemeAsync(string userOid, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MicrosoftOid == userOid, ct);
        if (user == null) return null;

        // If the user doesn't have preferences, we can return null and let the client handle it (e.g. use system theme)
        var pref = await db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        if (pref == null) return null;

        var theme = await db.AppThemes
            .AsNoTracking()
            .FirstAsync(t => t.Id == pref.SelectedThemeId, ct);

        return theme is null ? 
            null : 
            new AppThemeDto(
                theme.Id,
                theme.Name,
                theme.SwatchColor,
                themePaletteSerializer.ToObject(theme.LightPaletteJson),
                themePaletteSerializer.ToObject(theme.DarkPaletteJson),
                theme.IsSystem);
    }

    public async Task SetSelectedThemeAsync(string userOid, int? themeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.MicrosoftOid == userOid, ct);
        if (user == null) return;

        // If the user doesn't have preferences, we create them. Otherwise, we update the existing preferences.
        var pref = await db.UserPreferences
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        if (pref == null)
            pref = AddUserPreferences(db, user, themeId, null);
        else
        {
            pref.SelectedThemeId = themeId;
            pref.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetLanguageAsync(string userOid, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MicrosoftOid == userOid, ct);
        if (user == null) return null;

        // If the user doesn't have preferences, we can return null and let the client handle it (e.g. use system theme)
        var pref = await db.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        return pref?.Language;
    }

    public async Task SetLanguageAsync(string userOid, string languageCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.MicrosoftOid == userOid, ct);
        if (user == null) return;

        // If the user doesn't have preferences, we create them. Otherwise, we update the existing preferences.
        var pref = await db.UserPreferences
            .FirstOrDefaultAsync(u => u.Id == user.Id, ct);
        if (pref == null)
            pref = AddUserPreferences(db, user, null, languageCode);
        else
        {
            pref.Language = languageCode;
            pref.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static UserPreference AddUserPreferences(AppDbContext db, AppUser user, int? themeId, string? languageCode)
    {
        var pref = new UserPreference
        {
            Id = user.Id,
            AppUser = user,
            Language = languageCode,
            SelectedThemeId = themeId,
            UpdatedAt = DateTime.UtcNow
        };
        db.UserPreferences.Add(pref);
        return pref;
    }
}