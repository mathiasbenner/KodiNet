using FluentAssertions;
using KodiNet.Domain.Entities;

namespace Kodinet.Domain.Tests;

public class CronJobTests
{
    [Fact]
    public void CronJob_ShouldBeDisabledAtCreation()
    {
        CronJob job = new();
        job.IsEnabled.Should().BeFalse("at creation, cron job should be disabled");
    }

    [Fact]
    public void Disable_ShouldThrowIfAlreadyDisabled()
    {
        CronJob job = new();
        job.Invoking(j => j.Disable()).Should().Throw<InvalidOperationException>("disabling a job while already disabled should throw");
    }

    [Fact]
    public void Enable_ShouldThrowIfAlreadyEnabled()
    {
        CronJob job = new();
        job.Enable();
        job.Invoking(j => j.Enable()).Should().Throw<InvalidOperationException>("enabling a job while already enabled should throw");
    }
}