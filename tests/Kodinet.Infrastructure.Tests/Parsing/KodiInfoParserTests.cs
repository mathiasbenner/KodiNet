using FluentAssertions;
using KodiNet.Domain.Enums;
using KodiNet.Infrastructure.Parsing;
using System.Text.Json.Nodes;

namespace KodiNet.Infrastructure.Tests.Parsing;

public sealed class KodiInfoParserTests
{
    // ── ParseCpuUsage ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("#0: 12.5%, #1: 8.3%", 10.4)]       // average 2 cores
    [InlineData("#0: 100%, #1: 0%, #2: 50%", 50.0)] // average 3 cores
    [InlineData("25%", 25.0)]                       // simple format
    [InlineData("25.7 %", 25.7)]                    // with space
    [InlineData("0%", 0.0)]                         // zero
    [InlineData("", 0.0)]                           // empty string
    [InlineData(null, 0.0)]                         // null
    public void ParseCpuUsage_ShouldReturnExpected(string? input, double expected)
    {
        var result = KodiInfoParser.ParseCpuUsage(input);
        result.Should().BeApproximately(expected, precision: 0.01);
    }

    [Fact]
    public void ParseCpuUsage_MultiCore_ShouldAverageAllCores()
    {
        // 4 cores at 0%, 25%, 50%, 75% → average = 37.5%
        var input  = "#0: 0%, #1: 25%, #2: 50%, #3: 75%";
        var result = KodiInfoParser.ParseCpuUsage(input);
        result.Should().BeApproximately(37.5, 0.01);
    }

    // ── ParseMemory ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1 GB", 1_073_741_824L)]
    [InlineData("1.5 GB", 1_610_612_736L)]
    [InlineData("512 MB", 536_870_912L)]
    [InlineData("2048 KB", 2_097_152L)]
    [InlineData("1024 B", 1_024L)]
    [InlineData("1 GiB", 1_073_741_824L)]  // LibreELEC
    [InlineData("512 MiB", 536_870_912L)]  // LibreELEC
    [InlineData("0 MB", 0L)]
    [InlineData("", 0L)]
    [InlineData(null, 0L)]
    [InlineData("valeur invalide", 0L)]
    public void ParseMemory_ShouldReturnBytes(string? input, long expectedBytes)
    {
        KodiInfoParser.ParseMemory(input).Should().Be(expectedBytes);
    }

    [Fact]
    public void ParseMemory_WithDecimalComma_ShouldHandleLocale()
    {
        // Some versions could use the comma as separator
        var result = KodiInfoParser.ParseMemory("1,5 GB");
        result.Should().Be((long)(1.5 * 1_073_741_824));
    }

    // ── ParseTemperature ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("45°C", 45.0)]
    [InlineData("45 °C", 45.0)]
    [InlineData("45.2°C", 45.2)]
    [InlineData("45 C", 45.0)]
    [InlineData("0°C", 0.0)]
    [InlineData("", 0.0)]
    [InlineData(null, 0.0)]
    public void ParseTemperature_ShouldReturnCelsius(string? input, double expected)
    {
        KodiInfoParser.ParseTemperature(input).Should().BeApproximately(expected, 0.01);
    }

    // ── ParseSeconds ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseSeconds_ShouldConvertHoursMinutesSeconds()
    {
        var node = JsonNode.Parse("""{"hours":1,"minutes":30,"seconds":45}""");
        KodiInfoParser.ParseSeconds(node).Should().Be(5445); // 3600+1800+45
    }

    [Fact]
    public void ParseSeconds_ShouldReturnZero_WhenNull()
    {
        KodiInfoParser.ParseSeconds(null).Should().Be(0);
    }

    [Fact]
    public void ParseSeconds_ShouldHandleZeroValues()
    {
        var node = JsonNode.Parse("""{"hours":0,"minutes":0,"seconds":0}""");
        KodiInfoParser.ParseSeconds(node).Should().Be(0);
    }

    [Fact]
    public void ParseSeconds_ShouldHandleMissingFields()
    {
        // Kodi peut omettre certains champs
        var node = JsonNode.Parse("""{"minutes":5}""");
        KodiInfoParser.ParseSeconds(node).Should().Be(300);
    }

    // ── RepeatMode ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("off", RepeatMode.Off)]
    [InlineData("one", RepeatMode.One)]
    [InlineData("all", RepeatMode.All)]
    [InlineData("OFF", RepeatMode.Off)] // case insensitive
    [InlineData("ALL", RepeatMode.All)]
    [InlineData(null, RepeatMode.Off)]  // null → Off
    [InlineData("", RepeatMode.Off)]    // empty → Off
    [InlineData("unknown", RepeatMode.Off)]
    public void ParseRepeatMode_ShouldReturnCorrectEnum(string? input, RepeatMode expected)
    {
        KodiInfoParser.ParseRepeatMode(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(RepeatMode.Off, "off")]
    [InlineData(RepeatMode.One, "one")]
    [InlineData(RepeatMode.All, "all")]
    public void FormatRepeatMode_ShouldReturnKodiString(RepeatMode mode, string expected)
    {
        KodiInfoParser.FormatRepeatMode(mode).Should().Be(expected);
    }

    [Fact]
    public void RepeatMode_RoundTrip_ShouldBeIdempotent()
    {
        // Parse → Format → Parse must return the original value
        foreach (var mode in Enum.GetValues<RepeatMode>())
        {
            var formatted = KodiInfoParser.FormatRepeatMode(mode);
            var parsed    = KodiInfoParser.ParseRepeatMode(formatted);
            parsed.Should().Be(mode, $"round-trip failed for {mode}");
        }
    }
}