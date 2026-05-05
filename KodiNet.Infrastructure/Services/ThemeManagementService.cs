using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

public sealed class ThemeManagementService(
    IDbContextFactory<AppDbContext> dbFactory,
    IThemePaletteSerializer<ThemePaletteDto> themePaletteSerializer) : IThemeManagementService
{
    public async Task<IReadOnlyList<AppThemeDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.AppThemes
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .Select(e => 
                new AppThemeDto(
                    e.Id, 
                    e.Name, 
                    e.SwatchColor,
                    themePaletteSerializer.ToObject(e.LightPaletteJson),
                    themePaletteSerializer.ToObject(e.DarkPaletteJson),
                    e.IsSystem))
            .ToListAsync(ct);
    }

    public async Task<AppThemeDto> CreateAsync(CreateThemeRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = new AppTheme { 
            Name = request.Name.Trim(), 
            SwatchColor = request.SwatchColor, 
            LightPaletteJson = themePaletteSerializer.ToJson(request.Light), 
            DarkPaletteJson = themePaletteSerializer.ToJson(request.Dark)};
        db.AppThemes.Add(entity);
        await db.SaveChangesAsync(ct);
        return new AppThemeDto(
            entity.Id, 
            entity.Name, 
            entity.SwatchColor, 
            themePaletteSerializer.ToObject(entity.LightPaletteJson),
            themePaletteSerializer.ToObject(entity.DarkPaletteJson), 
            entity.IsSystem);
    }

    public async Task<AppThemeDto> UpdateAsync(UpdateThemeRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.AppThemes.FindAsync([request.Id], ct)
            ?? throw new KeyNotFoundException($"Theme {request.Id} not found.");
        entity.Name = request.Name.Trim();
        entity.SwatchColor = request.SwatchColor;
        entity.LightPaletteJson = themePaletteSerializer.ToJson(request.Light);
        entity.DarkPaletteJson = themePaletteSerializer.ToJson(request.Dark);
        await db.SaveChangesAsync(ct);
        return new AppThemeDto(
            entity.Id, 
            entity.Name, 
            entity.SwatchColor,
            themePaletteSerializer.ToObject(entity.LightPaletteJson),
            themePaletteSerializer.ToObject(entity.LightPaletteJson), 
            entity.IsSystem);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // Prevent deletion of system themes
        if (db.AppThemes.Any(e => e.Id == id && e.IsSystem))
            throw new InvalidOperationException("Cannot delete a system theme.");

        // If any user has this theme selected, switch them to the default theme before deleting
        var hasUsers = await db.UserPreferences.AnyAsync(p => p.SelectedThemeId == id, ct);
        if (hasUsers)
        {
            var defaultTheme = await db.AppThemes.FirstAsync(t => t.IsSystem, ct) 
                ?? throw new InvalidOperationException("No default theme found. Cannot delete this theme.");
            await db.UserPreferences.Where(p => p.SelectedThemeId == id)
                .ExecuteUpdateAsync(p => p.SetProperty(up => up.SelectedThemeId, defaultTheme.Id), ct);
        }
        await db.AppThemes.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
    }
}
