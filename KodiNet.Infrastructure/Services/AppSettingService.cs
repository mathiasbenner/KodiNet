using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

public sealed class AppSettingService(
    IDbContextFactory<AppDbContext> dbFactory,
    ICredentialEncryption encryption) : IAppSettingService
{
    private static readonly (string Key, string InputHint)[] MailSettingsMeta =
    [
        ("Mail:FromName",  "KodiNet"),
        ("Mail:From",      "kodinet@domaine.com"),
        ("Mail:SmtpHost",  "smtp.domaine.com"),
        ("Mail:SmtpPort",  "587"),
        ("Mail:EnableSsl", "true"),   // bool — UI managed
        ("Mail:Username",  ""),
        ("Mail:Password",  ""),
    ];

    public async Task<IReadOnlyList<AppSettingDto>> GetMailSettingsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var result = await db.AppSettings.AsNoTracking()
            .Where(s => MailSettingsMeta.Select(e => e.Key).Contains(s.Key))
            .ToListAsync(ct);
        return result.Select(s => new AppSettingDto(
            s.Key,
            s.IsSensitive ? string.Empty : s.Value ?? string.Empty,
            s.IsSensitive,
            MailSettingsMeta.First(m => m.Key == s.Key).InputHint))
            .ToList();
    }

    public async Task UpdateSettingAsync(UpdateAppSettingRequest request, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var setting = await db.AppSettings.FindAsync([request.Key], ct)
                      ?? throw new KeyNotFoundException($"Clé {request.Key} introuvable.");

        setting.Value = setting.IsSensitive
            ? encryption.Encrypt(request.Value)
            : request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var setting = await db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, ct);
        if (setting is null) return null;
        return setting.IsSensitive ? encryption.Decrypt(setting.Value) : setting.Value;
    }
}