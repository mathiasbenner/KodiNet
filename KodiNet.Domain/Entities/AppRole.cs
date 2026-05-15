namespace KodiNet.Domain.Entities;

/// <summary>An application role managed from the internal database.</summary>
public sealed class AppRole
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = ""; // ex: "Admin", "Operator", "Viewer"
    public string Description { get; set; } = "";

    public ICollection<UserRole> UserRoles { get; set; } = [];
}