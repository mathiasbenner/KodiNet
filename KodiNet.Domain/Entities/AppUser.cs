namespace KodiNet.Domain.Entities;

/// <summary>An application user (linked to the Microsoft AD account).</summary>
public sealed class AppUser
{
    public int    Id                        { get; set; }
    public string MicrosoftOid              { get; set; } = ""; // Object ID Azure AD
    public string Email                     { get; set; } = "";
    public string DisplayName               { get; set; } = "";
    public DateTime LastLoginAt             { get; set; }
    public bool CronNotificationsEnabled    { get; set; } = false;
    public string? NotificationEmail        { get; set; } // null = use this.Email
    public DateTime UpdatedAt               { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}