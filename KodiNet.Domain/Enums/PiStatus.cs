namespace KodiNet.Domain.Enums;

public enum PiStatus
{
    /// <summary>Kodi connection responded — no media is currently playing.</summary>
    Idle,
    /// <summary>Kodi is currently playing media.</summary>
    Playing,
    /// <summary>Kodi is not responding on the configured IP.</summary>
    Offline,
    /// <summary>The IP responds but is not a Kodi host (LibreELEC absent).</summary>
    Incompatible
}