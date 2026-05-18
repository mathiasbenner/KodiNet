namespace KodiNet.Web;

/// <summary>
/// Source de vérité unique pour toutes les routes de l'application.
/// Utiliser ces constantes dans @attribute [Routes()], les NavigateTo(), et les Href.
/// </summary>
public static class AppRoutes
{
    public const string Dashboard = "/";
    public const string Storage = "/storage";
    public const string Settings = "/settings";
    public const string SettingsWParameters = Settings + "/{section}";
}