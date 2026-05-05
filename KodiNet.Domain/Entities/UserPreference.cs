namespace KodiNet.Domain.Entities;

public sealed class UserPreference
{
    public int          Id                  { get; set; }
    public AppUser      AppUser             { get; set; } = null!;
    public string?      Language            { get; set; }
    public int?         SelectedThemeId     { get; set; }   // null = default theme
    public AppTheme?    SelectedTheme       { get; set; }
    public DateTime     UpdatedAt           { get; set; } = DateTime.UtcNow;
}