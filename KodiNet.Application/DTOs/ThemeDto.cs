namespace KodiNet.Application.DTOs;

// Serialized palette — only configurable colors
public record ThemePaletteDto(
    string Primary,
    string PrimaryDarken,
    string Secondary,
    string SecondaryDarken,
    string Background,
    string Surface,
    string AppbarBackground,
    string AppbarText,
    string TextPrimary,
    string TextSecondary,
    string Divider,
    string Error,
    string Success,
    string Warning,
    string Info);

public record AppThemeDto(
    int              Id,
    string           Name,
    string           SwatchColor,
    ThemePaletteDto  Light,
    ThemePaletteDto  Dark,
    bool             IsSystem);