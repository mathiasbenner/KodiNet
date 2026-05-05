using KodiNet.Application.DTOs;
using MudBlazor;

namespace KodiNet.Web.Layout;

/// <summary>
/// Définition complète d'un thème applicatif.
/// Pour ajouter un thème : instancier un ThemeDefinition et l'ajouter à KodiNetTheme.All.
/// Aucun autre fichier n'a besoin d'être modifié.
/// </summary>
public sealed record ThemeDefinition(
    int Id,         // identifiant
    string Name,     // libellé affiché dans l'UI
    string Swatch,   // couleur hex du mode clair, pour la pastille de sélection
    MudTheme Theme);   // un seul MudTheme portant PaletteLight + PaletteDark

/// <summary>
/// Catalogue des thèmes de l'application.<br />
/// Chaque thème expose une paire Light/Dark construite sur une palette harmonieuse.<br />
///
/// Thème Ruby    : bordeaux #9D344B / rose dorée #E8A0A8<br />
/// Thème Ocean   : bleu pétrole #28546C / cyan doux #6BAFC0<br />
/// Thème Amber   : bois #AA6C39 / ambre clair #D4A96A<br />
/// </summary>
public static partial class KodiNetTheme
{
    public static MudTheme BuildFromDto(AppThemeDto dto)
    {
        return new MudTheme
        {
            PaletteLight = BuildPalette<PaletteLight>(dto.Light),
            PaletteDark = BuildPalette<PaletteDark>(dto.Dark),
            Typography = AppTypography,
            LayoutProperties = AppLayout
        };
    }

    public static MudTheme BuildDefault() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary             = "#9D344B",
            PrimaryDarken       = "#7a2539",
            Secondary           = "#4A7C5E",
            SecondaryDarken     = "#3a6149",
            Background          = "#fdf5f6",
            Surface             = "#ffffff",
            AppbarBackground    = "#9D344B",
            AppbarText          = "#ffffff",
            TextPrimary         = "#3a1520",
            TextSecondary       = "#9a7580",
            Divider             = "#f0d5da",
            Error               = "#b71c1c",
            Success             = "#4A7C5E",
            Warning             = "#e65100",
            Info                = "#7a2539"
        },
        PaletteDark = new PaletteDark
        {
            Primary             = "#e08090",
            PrimaryDarken       = "#f0a8b4",
            Secondary           = "#7ab89a",
            SecondaryDarken     = "#9acdb5",
            Background          = "#1a0d10",
            Surface             = "#2b1419",
            AppbarBackground    = "#220e13",
            AppbarText          = "#fce8eb",
            TextPrimary         = "#fce8eb",
            TextSecondary       = "#c09090",
            Divider             = "#4a2530",
            Error               = "#ef9a9a",
            Success             = "#a5d6a7",
            Warning             = "#ffcc80",
            Info                = "#e08090"
        },
        Typography = AppTypography,
        LayoutProperties = AppLayout
    };


    // ── Private helpers ───────────────────────────────────────────────────────

    private static Palette BuildPalette<T>(ThemePaletteDto theme)
    {
        switch (typeof(T))
        {
            case Type t when t == typeof(PaletteLight):
                return new PaletteLight
                {
                    Primary = theme.Primary,
                    PrimaryDarken = theme.PrimaryDarken,
                    Secondary = theme.Secondary,
                    SecondaryDarken = theme.SecondaryDarken,
                    Background = theme.Background,
                    Surface = theme.Surface,
                    AppbarBackground = theme.AppbarBackground,
                    AppbarText = theme.AppbarText,
                    TextPrimary = theme.TextPrimary,
                    TextSecondary = theme.TextSecondary,
                    Divider = theme.Divider,
                    Error = theme.Error,
                    Success = theme.Success,
                    Warning = theme.Warning,
                    Info = theme.Info
                };
            case Type t when t == typeof(PaletteDark):
                return new PaletteDark
                {
                    Primary = theme.Primary,
                    PrimaryDarken = theme.PrimaryDarken,
                    Secondary = theme.Secondary,
                    SecondaryDarken = theme.SecondaryDarken,
                    Background = theme.Background,
                    Surface = theme.Surface,
                    AppbarBackground = theme.AppbarBackground,
                    AppbarText = theme.AppbarText,
                    TextPrimary = theme.TextPrimary,
                    TextSecondary = theme.TextSecondary,
                    Divider = theme.Divider,
                    Error = theme.Error,
                    Success = theme.Success,
                    Warning = theme.Warning,
                    Info = theme.Info
                };
            default:
                throw new ArgumentException($"Type de palette non supporté : {typeof(T).Name}");
        }
    }

    private static Typography AppTypography => new()
    {
        Default = new DefaultTypography { FontFamily = ["Noto Sans", "sans-serif"] },
        H1 = new H1Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "700" },
        H2 = new H2Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "700" },
        H3 = new H3Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "700" },
        H4 = new H4Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "700" },
        H5 = new H5Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "600" },
        H6 = new H6Typography { FontFamily = ["Rajdhani", "sans-serif"], FontWeight = "600" },
        Button = new ButtonTypography { FontFamily = ["Noto Sans", "sans-serif"], FontWeight = "600" }
    };

    private static LayoutProperties AppLayout => new() { AppbarHeight = "54px" };
}
