namespace KodiNet.Infrastructure;

/// <summary>
/// Centralized application constants.
/// Any significant numeric value must be defined here.
/// </summary>
public static class AppConstants
{
    public static class Localization
    {
        /// <summary>Default culture.</summary>
        public const string DefaultCulture = "fr";
    }

    public static class Kodi
    {
        /// <summary>Timeout for a standard JSON-RPC request.</summary>
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

        /// <summary>Timeout for the availability ping (should be short).</summary>
        public static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);
    }

    public static class Polling
    {
        /// <summary>Refresh frequency for Raspberry Pi status on the dashboard.</summary>
        public static readonly TimeSpan RefreshPiStatusInterval = TimeSpan.FromSeconds(20);

        /// <summary>Refresh frequency for the list of Raspberry Pi devices on the dashboard.</summary>
        public static readonly TimeSpan RefreshPiListInterval = TimeSpan.FromSeconds(30);

        /// <summary>Refresh frequency for the player status in the media tab.</summary>
        public static readonly TimeSpan PlayerInterval = TimeSpan.FromSeconds(5);

        /// <summary>Number of Raspberry Pis processed concurrently during polling.</summary>
        public const int StatusPollChunkSize = 5;
    }

    public static class Transfer
    {
        /// <summary>Maximum number of concurrent SFTP transfers.</summary>
        public const int MaxConcurrentTransfers = 3;
    }
}