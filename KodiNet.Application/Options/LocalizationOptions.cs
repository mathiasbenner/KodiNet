namespace KodiNet.Application.Options;

/// <summary>
/// Localization settings.
/// Configurable in appsettings.json → "Localization" section.
/// </summary>
public sealed class LocalizationOptions
{
    public const string Section = "Localization";

    /// <summary>Default culture of the app (default: fr).</summary>
    public string DefaultCulture { get; init; } = "fr";
}