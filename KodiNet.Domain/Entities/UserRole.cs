namespace KodiNet.Domain.Entities;

/// <summary>Join table for user ↔ role.</summary>
public sealed class UserRole
{
    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int AppRoleId { get; set; }
    public AppRole AppRole { get; set; } = null!;
}