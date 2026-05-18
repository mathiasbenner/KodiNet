using FluentAssertions;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Tests.Data;

public sealed class AppDbContextAppSettingsSeedTests : IDisposable
{
    private readonly AppDbContext _db;

    public AppDbContextAppSettingsSeedTests()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        var options = optionsBuilder.Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Database_ShouldBeSeedWith7MailSettings()
    {
        // Act
        var settings = _db.AppSettings.ToList();

        // Assert
        settings.Should().HaveCount(7, "exactly 7 mail settings must be seeded");
    }

    [Fact]
    public void Database_ShouldContainMailFromSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:From");

        // Assert
        setting.Should().NotBeNull("Mail:From setting must be seeded");
        setting!.Value.Should().Be("");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailFromNameSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:FromName");

        // Assert
        setting.Should().NotBeNull("Mail:FromName setting must be seeded");
        setting!.Value.Should().Be("Kodinet");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailSmtpHostSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:SmtpHost");

        // Assert
        setting.Should().NotBeNull("Mail:SmtpHost setting must be seeded");
        setting!.Value.Should().Be("");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailSmtpPortSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:SmtpPort");

        // Assert
        setting.Should().NotBeNull("Mail:SmtpPort setting must be seeded");
        setting!.Value.Should().Be("587");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailEnableSslSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:EnableSsl");

        // Assert
        setting.Should().NotBeNull("Mail:EnableSsl setting must be seeded");
        setting!.Value.Should().Be("true");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailUsernameSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:Username");

        // Assert
        setting.Should().NotBeNull("Mail:Username setting must be seeded");
        setting!.Value.Should().Be("");
        setting.IsSensitive.Should().BeFalse();
    }

    [Fact]
    public void Database_ShouldContainMailPasswordSetting()
    {
        // Act
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == "Mail:Password");

        // Assert
        setting.Should().NotBeNull("Mail:Password setting must be seeded");
        setting!.Value.Should().Be("");
        setting.IsSensitive.Should().BeTrue("password must be marked as sensitive");
    }

    [Fact]
    public void Database_ShouldSeedAllMailSettingsWithCorrectProperties()
    {
        // Act
        var settings = _db.AppSettings.OrderBy(s => s.Key).ToList();

        // Assert
        settings.Should().HaveCount(7);

        var expectedSettings = new[]
        {
            new { Key = "Mail:EnableSsl", Value = "true", IsSensitive = false },
            new { Key = "Mail:From", Value = "", IsSensitive = false },
            new { Key = "Mail:FromName", Value = "Kodinet", IsSensitive = false },
            new { Key = "Mail:Password", Value = "", IsSensitive = true },
            new { Key = "Mail:SmtpHost", Value = "", IsSensitive = false },
            new { Key = "Mail:SmtpPort", Value = "587", IsSensitive = false },
            new { Key = "Mail:Username", Value = "", IsSensitive = false }
        };

        for (int i = 0; i < settings.Count; i++)
        {
            settings[i].Key.Should().Be(expectedSettings[i].Key);
            settings[i].Value.Should().Be(expectedSettings[i].Value);
            settings[i].IsSensitive.Should().Be(expectedSettings[i].IsSensitive);
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
