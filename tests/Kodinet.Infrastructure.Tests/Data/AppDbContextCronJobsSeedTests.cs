using FluentAssertions;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Tests.Data;

public sealed class AppDbContextCronJobsSeedTests : IDisposable
{
    private readonly AppDbContext _db;

    public AppDbContextCronJobsSeedTests()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        var options = optionsBuilder.Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Database_ShouldBeSeedWith2CronJobs()
    {
        // Act
        var jobs = _db.CronJobs.ToList();

        // Assert
        jobs.Should().HaveCount(2, "exactly 2 cron jobs must be seeded");
    }

    [Fact]
    public void Database_ShouldContainKodiRestarterJob()
    {
        // Act
        var job = _db.CronJobs.FirstOrDefault(j => j.Name == "KodiRestarter");

        // Assert
        job.Should().NotBeNull("KodiRestarter job must be seeded");
        job!.Id.Should().Be(1);
        job.Description.Should().Be("Restart any inactive Kodi players and play the video folder on a loop.");
        job.Schedule.Should().Be("0 6-16 * * *");
        job.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainKodiRebooterJob()
    {
        // Act
        var job = _db.CronJobs.FirstOrDefault(j => j.Name == "KodiRebooter");

        // Assert
        job.Should().NotBeNull("KodiRebooter job must be seeded");
        job!.Id.Should().Be(2);
        job.Description.Should().Be("Reboot the LibreELEC system on all the Pi devices in the list.");
        job.Schedule.Should().Be("50 5 * * *");
        job.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldSeedAllCronJobsAsDisabledByDefault()
    {
        // Act
        var jobs = _db.CronJobs.ToList();

        // Assert
        jobs.Should().AllSatisfy(j => j.IsEnabled.Should().BeFalse("all seeded cron jobs should be disabled by default"));
    }

    [Fact]
    public void Database_ShouldSeedCronJobsWithValidCronExpressions()
    {
        // Act
        var jobs = _db.CronJobs.OrderBy(j => j.Id).ToList();

        // Assert
        jobs.Should().HaveCount(2);
        jobs[0].Schedule.Should().Be("0 6-16 * * *", "KodiRestarter schedule should be valid");
        jobs[1].Schedule.Should().Be("50 5 * * *", "KodiRebooter schedule should be valid");
    }

    [Fact]
    public void Database_ShouldSeedAllCronJobsWithCorrectProperties()
    {
        // Act
        var jobs = _db.CronJobs.OrderBy(j => j.Id).ToList();

        // Assert
        jobs.Should().HaveCount(2);

        var expectedJobs = new[]
        {
            new
            {
                Id = 1,
                Name = "KodiRestarter",
                Description = "Restart any inactive Kodi players and play the video folder on a loop.",
                Schedule = "0 6-16 * * *",
                IsEnabled = false
            },
            new
            {
                Id = 2,
                Name = "KodiRebooter",
                Description = "Reboot the LibreELEC system on all the Pi devices in the list.",
                Schedule = "50 5 * * *",
                IsEnabled = false
            }
        };

        for (int i = 0; i < jobs.Count; i++)
        {
            jobs[i].Id.Should().Be(expectedJobs[i].Id);
            jobs[i].Name.Should().Be(expectedJobs[i].Name);
            jobs[i].Description.Should().Be(expectedJobs[i].Description);
            jobs[i].Schedule.Should().Be(expectedJobs[i].Schedule);
            jobs[i].IsEnabled.Should().Be(expectedJobs[i].IsEnabled);
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
