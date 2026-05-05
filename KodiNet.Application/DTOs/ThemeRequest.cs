namespace KodiNet.Application.DTOs;

public record CreateThemeRequest(string Name, string SwatchColor, ThemePaletteDto Light, ThemePaletteDto Dark);
public record UpdateThemeRequest(int Id, string Name, string SwatchColor, ThemePaletteDto Light, ThemePaletteDto Dark);