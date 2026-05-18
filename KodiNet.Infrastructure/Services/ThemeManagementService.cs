using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

    public async Task<IReadOnlyList<AppThemeDto>> ImportFromCsvAsync(CsvThemeImportRequest request, CancellationToken ct = default)
    {
        var results = new List<AppThemeDto>();
        foreach(var row in request.Rows.Where(r => r.ParseError is null))
        {
            var created = await CreateAsync(
                new CreateThemeRequest(
                    row.Name, 
                    row.LightPalette.Primary, 
                    row.LightPalette, 
                    row.DarkPalette), ct);
            results.Add(created);
        }
        return results;
    }

    public async Task<byte[]> ExportIntoCsvBytesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var themes = await db.AppThemes
            .AsNoTracking()
            .Where(e => !e.IsSystem) // Exclude system themes from export
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

        var sb = new StringBuilder();
        sb.Append("name;");
        sb.Append("light_primary;light_primaryDarken;light_secondary;light_secondaryDarken;light_background;light_surface;");
        sb.Append("light_appbarBackground;light_appbarText;light_textPrimary;light_textSecondary;light_divider;");
        sb.Append("light_error;light_success;light_warning;light_info;");
        sb.Append("dark_primary;dark_primaryDarken;dark_secondary;dark_secondaryDarken;dark_background;dark_surface;");
        sb.Append("dark_appbarBackground;dark_appbarText;dark_textPrimary;dark_textSecondary;dark_divider;");
        sb.AppendLine("dark_error;dark_success;dark_warning;dark_info");
        foreach (var theme in themes)
        {
            sb.Append($"{theme.Name};");
            sb.Append($"{theme.Light.Primary};{theme.Light.PrimaryDarken};{theme.Light.Secondary};{theme.Light.SecondaryDarken};{theme.Light.Background};{theme.Light.Surface};");
            sb.Append($"{theme.Light.AppbarBackground};{theme.Light.AppbarText};{theme.Light.TextPrimary};{theme.Light.TextSecondary};{theme.Light.Divider};");
            sb.Append($"{theme.Light.Error};{theme.Light.Success};{theme.Light.Warning};{theme.Light.Info};");
            sb.Append($"{theme.Dark.Primary};{theme.Dark.PrimaryDarken};{theme.Dark.Secondary};{theme.Dark.SecondaryDarken};{theme.Dark.Background};{theme.Dark.Surface};");
            sb.Append($"{theme.Dark.AppbarBackground};{theme.Dark.AppbarText};{theme.Dark.TextPrimary};{theme.Dark.TextSecondary};{theme.Dark.Divider};");
            sb.AppendLine($"{theme.Dark.Error};{theme.Dark.Success};{theme.Dark.Warning};{theme.Dark.Info}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
