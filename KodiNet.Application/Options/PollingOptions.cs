namespace KodiNet.Application.Options;

/// <summary>
/// Polling intervals on Dashboard.
/// Configurable in appsettings.json → "Polling" section.
/// </summary>
public sealed class PollingOptions
{
    public const string Section = "Polling";

    /// <summary>Refresh frequency for Raspberry Pi status on the dashboard (default: 20s).</summary>
    public int StatusIntervalSeconds { get; init; } = 20;

    /// <summary>Refresh frequency for the list of Raspberry Pi devices on the dashboard (default: 30s).</summary>
    public int ListRefreshIntervalSeconds { get; init; } = 30;

    /// <summary>Refresh frequency for the player status in the media tab (default: 5s).</summary>
    public int PlayerMediaTabIntervalSeconds { get; init; } = 5;

    /// <summary>Concurrent interrogated Kodis (default: 10).</summary>
    public int MaxConcurrency { get; init; } = 10;

    // Propriétés calculées pour consommation directe
    public TimeSpan StatusInterval
        => TimeSpan.FromSeconds(StatusIntervalSeconds);
    public TimeSpan ListRefreshInterval
        => TimeSpan.FromSeconds(ListRefreshIntervalSeconds);

    public TimeSpan PlayerMediaTabInterval
        => TimeSpan.FromSeconds(PlayerMediaTabIntervalSeconds);
}