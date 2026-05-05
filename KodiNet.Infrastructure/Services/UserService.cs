using KodiNet.Application.DTOs;
using KodiNet.Application.Interfaces;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Services;

// ─── UserService ──────────────────────────────────────────────────────────────

/// <summary>
/// Manage users and their application roles.
/// Used by ClaimsTransformation and settings page (admin).
/// </summary>
public sealed class UserService(IDbContextFactory<AppDbContext> dbFactory) : IUserService
{
    public async Task<UserDto?> GetByOidAsync(string oid, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.AppRole)
            .FirstOrDefaultAsync(u => u.MicrosoftOid == oid, ct);
        return entity is null ? 
            null : 
            new UserDto(
                entity.Id,
                entity.DisplayName,
                entity.Email,
                entity.LastLoginAt,
                entity.UserRoles,
                entity.CronNotificationsEnabled,
                entity.NotificationEmail);
    }

    public async Task<UserDto> UpsertAsync(NewUserRequest user, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var existing = await db.Users.FirstOrDefaultAsync(u => u.MicrosoftOid == user.MicrosoftOid, ct);
        if (existing is null)
        {
            var entityEntry = db.Users.Add(new AppUser
            {
                MicrosoftOid = user.MicrosoftOid,
                DisplayName = user.DisplayName,
                Email = user.Email,
                LastLoginAt = user.LastLoginAt
            });
            await db.SaveChangesAsync(ct);
            existing = entityEntry.Entity;

            // First user → automatic Owner
            var isFirstUser = !await db.UserRoles.AnyAsync(ct);
            if (isFirstUser)
            {
                var ownerRole = await db.Roles.FirstAsync(r => r.Name == "Owner", ct);
                db.UserRoles.Add(new UserRole { AppUserId = entityEntry.Entity.Id, AppRoleId = ownerRole.Id });
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            existing.DisplayName = user.DisplayName;
            existing.Email = user.Email;
            existing.LastLoginAt = user.LastLoginAt;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new UserDto(
            Id: existing.Id,
            DisplayName: existing.DisplayName,
            Email: existing.Email,
            LastLoginAt: existing.LastLoginAt,
            UserRoles: existing.UserRoles,
            CronNotificationsEnabled: existing.CronNotificationsEnabled,
            NotificationEmail: existing.NotificationEmail);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string oid, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.AppUser.MicrosoftOid == oid)
            .Select(ur => ur.AppRole.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.AppRole)
            .OrderBy(u => u.DisplayName)
            .Select(u => new UserDto(
                u.Id,
                u.DisplayName,
                u.Email,
                u.LastLoginAt,
                u.UserRoles,
                u.CronNotificationsEnabled,
                u.NotificationEmail))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AppRole>> GetAllRolesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Roles.AsNoTracking().ToListAsync(ct);
    }

    public async Task AssignRoleAsync(int userId, int roleId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Cannot assign Owner via this method — use TransferOwnershipAsync
        var role = await db.Roles.FindAsync([roleId], ct);
        if (role?.Name == "Owner")
            throw new InvalidOperationException("The Owner role cannot be assigned directly.");

        // An Owner cannot receive another role
        var isOwner = await db.UserRoles
        .AnyAsync(ur => ur.AppUserId == userId && ur.AppRole.Name == "Owner", ct);
        if (isOwner)
            throw new InvalidOperationException("The Owner cannot have other roles.");

        var exists = await db.UserRoles.AnyAsync(ur => ur.AppUserId == userId && ur.AppRoleId == roleId, ct);
        if (!exists)
        {
            db.UserRoles.Add(new UserRole { AppUserId = userId, AppRoleId = roleId });
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Cannot revoke Owner via this method
        var role = await db.Roles.FindAsync([roleId], ct);
        if (role?.Name == "Owner")
            throw new InvalidOperationException("The Owner role cannot be revoked.");

        await db.UserRoles
            .Where(ur => ur.AppUserId == userId && ur.AppRoleId == roleId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<UserDto?> GetOwnerAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.AppRole)
            .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.AppRole.Name == "Owner"), ct)
            .ContinueWith(t => t.Result == null ? null : new UserDto(
                t.Result.Id,
                t.Result.DisplayName,
                t.Result.Email,
                t.Result.LastLoginAt,
                t.Result.UserRoles,
                t.Result.CronNotificationsEnabled,
                t.Result.NotificationEmail), ct);
    }

    public async Task TransferOwnershipAsync(int newOwnerUserId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var ownerRole = await db.Roles.FirstAsync(r => r.Name == "Owner", ct);
        var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin", ct);

        // Change Owner to Admin for the current owner
        await db.UserRoles
            .Where(ur => ur.AppRoleId == ownerRole.Id)
            .ExecuteUpdateAsync(ur => ur.SetProperty(p => p.AppRoleId, adminRole.Id), ct);

        // The new Owner loses all existing roles and has only Owner
        await db.UserRoles
            .Where(ur => ur.AppUserId == newOwnerUserId)
            .ExecuteDeleteAsync(ct);

        db.UserRoles.Add(new UserRole { AppUserId = newOwnerUserId, AppRoleId = ownerRole.Id });
        await db.SaveChangesAsync(ct);
    }

    public async Task ToggleNotificationsAsync(int userId, bool enable, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.FindAsync([userId], ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        user.CronNotificationsEnabled = enable;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task ModifyNotificationEmailAsync(int userId, string? email, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var user = await db.Users.FindAsync([userId], ct);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        user.NotificationEmail = email;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
