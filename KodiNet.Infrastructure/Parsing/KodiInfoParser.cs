using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using KodiNet.Domain.Enums;

namespace KodiNet.Infrastructure.Parsing;

/// <summary>
/// Parsing values returned by Kodi's JSON-RPC API.<br />
/// System labels (CpuUsage, FreeMemory, etc.) are unstructured strings<br />
/// whose format may vary depending on the Kodi version and platform.
/// </summary>
public static class KodiInfoParser
{
    /// <summary>
    /// Parse the label System.CpuUsage.<br />
    /// Kodi may return:<br/>
    ///   - "#0: 12.5%, #1: 8.3%, ..." (multi-core LibreELEC)<br />
    ///   - "12%" (global value)<br />
    ///   - "12.5 %" (with space)<br />
    /// Returns the average of the cores if multi-core.
    /// </summary>
    public static double ParseCpuUsage(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;

        // multi-core : "#0: 12.5%, #1: 8.3%"
        var multiCore = Regex.Matches(s, @"#\d+:\s*([\d.]+)\s*%");
        if (multiCore.Count > 0)
        {
            var sum = 0.0;
            foreach (Match m in multiCore)
                if (double.TryParse(m.Groups[1].Value,
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    sum += v;
            return sum / multiCore.Count;
        }

        // simple : "12%" or "12.5 %"
        var simple = Regex.Match(s, @"([\d.]+)\s*%");
        if (simple.Success &&
            double.TryParse(simple.Groups[1].Value,
                NumberStyles.Any, CultureInfo.InvariantCulture, out var single))
            return single;

        return 0;
    }

    /// <summary>
    /// Parse a JSON node {hours, minutes, seconds} into total seconds.
    /// </summary>
    public static double ParseSeconds(JsonNode? node)
    {
        if (node is null) return 0;
        return (node["hours"]?.GetValue<int>() ?? 0) * 3600
             + (node["minutes"]?.GetValue<int>() ?? 0) * 60
             + (node["seconds"]?.GetValue<int>() ?? 0);
    }

    /// <summary>
    /// Parse a memory/disk string into bytes.<br />
    /// Supported formats: "1.5 GB", "512 MB", "2048 KB", "1024 B",
    ///                    "1.5 GiB", "512 MiB" (LibreELEC).
    /// </summary>
    public static long ParseMemory(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;

        var match = Regex.Match(s.Trim(),
            @"([\d.,]+)\s*(GiB|MiB|KiB|GB|MB|KB|B)?",
            RegexOptions.IgnoreCase);

        if (!match.Success) return 0;

        var num = match.Groups[1].Value.Replace(",", ".");
        if (!double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return 0;

        return match.Groups[2].Value.ToUpperInvariant() switch
        {
            "GB" or "GIB" => (long)(value * 1_073_741_824),
            "MB" or "MIB" => (long)(value * 1_048_576),
            "KB" or "KIB" => (long)(value * 1_024),
            "B" => (long)value,
            _ => (long)value
        };
    }

    /// <summary>
    /// Parse the CPU temperature.<br />
    /// Formats : "45°C", "45 °C", "45.2°C", "45 C".
    /// </summary>
    public static double ParseTemperature(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        var match = Regex.Match(s.Trim(), @"([\d.]+)\s*°?\s*C", RegexOptions.IgnoreCase);
        if (!match.Success) return 0;
        return double.TryParse(match.Groups[1].Value,
            NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : 0;
    }

    public static RepeatMode ParseRepeatMode(string? s) => s?.ToLowerInvariant() switch
    {
        "one" => RepeatMode.One,
        "all" => RepeatMode.All,
        _ => RepeatMode.Off
    };

    public static string FormatRepeatMode(RepeatMode mode) => mode switch
    {
        RepeatMode.One => "one",
        RepeatMode.All => "all",
        _ => "off"
    };
}