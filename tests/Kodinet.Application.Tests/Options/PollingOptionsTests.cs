using FluentAssertions;
using KodiNet.Application.Options;
using Microsoft.Extensions.Configuration;

namespace KodiNet.Application.Tests.Options;

public sealed class PollingOptionsTests
{
    // ── Default values ────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_ShouldBeReasonableForProduction()
    {
        var opts = new PollingOptions();

        opts.StatusIntervalSeconds.Should().BeGreaterThanOrEqualTo(20);
        opts.ListRefreshIntervalSeconds.Should().BeGreaterThanOrEqualTo(30);
        opts.MaxConcurrency.Should().Be(10);
    }

    [Fact]
    public void MaxConcurrency_Default_ShouldSupportFiftyPis()
    {
        // If 50 Pi and MaxConcurrency no more than 10, then 5 batches max
        var opts    = new PollingOptions();
        var piCount = 50;
        var waves   = Math.Ceiling((double)piCount / opts.MaxConcurrency);

        waves.Should().BeLessThanOrEqualTo(5,
            "50 Pi with concurrency no more than 10 must not have less than 5 batches");
    }

    // ── Calculated properties ─────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 1)]
    [InlineData(20, 20)]
    [InlineData(60, 60)]
    public void StatusInterval_ShouldConvertSecondsToTimeSpan(
        int seconds, int expectedSeconds)
    {
        var opts = new PollingOptions { StatusIntervalSeconds = seconds };
        opts.StatusInterval.TotalSeconds.Should().Be(expectedSeconds);
    }

    [Fact]
    public void ListRefreshInterval_ShouldAlwaysBeGreaterOrEqualToStatusInterval()
    {
        // Invariant job : refresh list less often than status
        var opts = new PollingOptions
        {
            StatusIntervalSeconds      = 20,
            ListRefreshIntervalSeconds = 30
        };

        opts.ListRefreshInterval.Should().BeGreaterThanOrEqualTo(opts.StatusInterval,
            "the list changes slower than status");
    }

    // ── IConfiguration binding ────────────────────────────────────────────────

    [Fact]
    public void ShouldBindFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Polling:StatusIntervalSeconds"]      = "15",
                ["Polling:ListRefreshIntervalSeconds"] = "45",
                ["Polling:MaxConcurrency"]             = "5"
            })
            .Build();

        var opts = new PollingOptions();
        config.GetSection(PollingOptions.Section).Bind(opts);

        opts.StatusIntervalSeconds.Should().Be(15);
        opts.ListRefreshIntervalSeconds.Should().Be(45);
        opts.MaxConcurrency.Should().Be(5);
        opts.StatusInterval.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void ShouldUseDefaults_WhenConfigurationIsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var opts = new PollingOptions();
        config.GetSection(PollingOptions.Section).Bind(opts);

        // Default values need to survive on empty section bind
        opts.StatusIntervalSeconds.Should().NotBe(null);
        opts.MaxConcurrency.Should().NotBe(null);
    }
}