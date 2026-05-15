namespace KodiNet.Domain.Entities;

public sealed class AppTheme
{
    public int      Id                  { get; set; }
    public string   Name                { get; set; } = "";
    public string   SwatchColor         { get; set; } = "#000000"; // UI swatch
    public string   LightPaletteJson    { get; set; } = "{}";    // JSON of light mode colors
    public string   DarkPaletteJson     { get; set; } = "{}";    // JSON of dark mode colors
    public bool     IsSystem            { get; set; }             // true = protected, non-deletable
    public DateTime CreatedAt           { get; set; } = DateTime.UtcNow;
}