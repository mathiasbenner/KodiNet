namespace KodiNet.Infrastructure;

/// <summary>
/// Centralized application constants.
/// Any significant numeric value must be defined here.
/// </summary>
public static class InfraConstants
{
    public static class Kodi
    {
        /// <summary>Timeout for a standard JSON-RPC request.</summary>
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

        /// <summary>Timeout for the availability ping (should be short).</summary>
        public static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);
    }

    public static class Transfer
    {
        /// <summary>Maximum number of concurrent SFTP transfers.</summary>
        public const int MaxConcurrentTransfers = 3;
    }
}