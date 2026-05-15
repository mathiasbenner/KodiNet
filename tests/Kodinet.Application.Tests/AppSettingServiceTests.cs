using FluentAssertions;
using KodiNet.Application.DTOs;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KodiNet.Application.Tests;

public sealed class AppSettingServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly Mock<ICredentialEncryption> _encryption;
    private readonly AppSettingService _sut;
    private readonly string _dbName;

    public AppSettingServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // Factory creates fresh contexts
        var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .Returns(() =>
                   {
                       var newContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                           .UseInMemoryDatabase(_dbName)
                           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                           .Options);
                       return Task.FromResult<AppDbContext>(newContext);
                   });
        _dbFactory = mockFactory.Object;

        // Mock encryption
        _encryption = new Mock<ICredentialEncryption>();
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => "ENCRYPTED_" + s);
        _encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s.Replace("ENCRYPTED_", ""));

        _sut = new AppSettingService(_dbFactory, _encryption.Object);
    }

    [Fact]
    public async Task GetMailSettingsAsync_ShouldReturnAllMailSettings()
    {
        // Act - Database is seeded with 7 mail settings
        var result = await _sut.GetMailSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(7);
        result.Should().Contain(s => s.Key == "Mail:From");
        result.Should().Contain(s => s.Key == "Mail:FromName");
        result.Should().Contain(s => s.Key == "Mail:SmtpHost");
        result.Should().Contain(s => s.Key == "Mail:SmtpPort");
        result.Should().Contain(s => s.Key == "Mail:EnableSsl");
        result.Should().Contain(s => s.Key == "Mail:Username");
        result.Should().Contain(s => s.Key == "Mail:Password");
    }

    [Fact]
    public async Task GetMailSettingsAsync_ShouldMarkPasswordAsSensitive()
    {
        // Act
        var result = await _sut.GetMailSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        var passwordSetting = result.FirstOrDefault(s => s.Key == "Mail:Password");
        passwordSetting.Should().NotBeNull();
        passwordSetting!.IsSensitive.Should().BeTrue();
    }

    [Fact]
    public async Task GetMailSettingsAsync_ShouldMaskSensitiveValues()
    {
        // Act
        var result = await _sut.GetMailSettingsAsync(TestContext.Current.CancellationToken);

        // Assert - Sensitive settings should have empty value
        var passwordSetting = result.FirstOrDefault(s => s.Key == "Mail:Password");
        passwordSetting.Should().NotBeNull();
        passwordSetting!.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMailSettingsAsync_ShouldNotMaskNonSensitiveValues()
    {
        // Act
        var result = await _sut.GetMailSettingsAsync(TestContext.Current.CancellationToken);

        // Assert - Non-sensitive settings should show their values
        var smtpPortSetting = result.FirstOrDefault(s => s.Key == "Mail:SmtpPort");
        smtpPortSetting.Should().NotBeNull();
        smtpPortSetting!.Value.Should().Be("587");
    }

    [Fact]
    public async Task UpdateSettingAsync_ShouldUpdateSetting()
    {
        // Arrange
        var request = new UpdateAppSettingRequest("Mail:From", "newemail@test.com");

        // Act
        await _sut.UpdateSettingAsync(request, TestContext.Current.CancellationToken);

        // Assert - Verify in database using fresh context
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                   .Options);
        var setting = await assertDb.AppSettings.FirstAsync(s => s.Key == "Mail:From", TestContext.Current.CancellationToken);
        setting.Value.Should().Be("newemail@test.com");
    }

    [Fact]
    public async Task UpdateSettingAsync_ShouldEncryptSensitiveSettings()
    {
        // Arrange
        var request = new UpdateAppSettingRequest("Mail:Password", "mysecretpass");

        // Act
        await _sut.UpdateSettingAsync(request, TestContext.Current.CancellationToken);

        // Assert
        _encryption.Verify(e => e.Encrypt("mysecretpass"), Times.Once);

        // Verify encrypted value in database
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                   .Options);
        var setting = await assertDb.AppSettings.FirstAsync(s => s.Key == "Mail:Password", TestContext.Current.CancellationToken);
        setting.Value.Should().StartWith("ENCRYPTED_");
    }

    [Fact]
    public async Task UpdateSettingAsync_ShouldThrowKeyNotFoundException_WhenSettingNotFound()
    {
        // Arrange
        var request = new UpdateAppSettingRequest("NonExistent:Key", "value");

        // Act & Assert
        await _sut.Invoking(s => s.UpdateSettingAsync(request, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NonExistent:Key*");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDecryptedValue_ForSensitiveSettings()
    {
        // Arrange
        _db.AppSettings.Add(new AppSetting { Key = "Test:Sensitive", Value = "ENCRYPTED_secret", IsSensitive = true });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetAsync("Test:Sensitive", TestContext.Current.CancellationToken);

        // Assert
        _encryption.Verify(e => e.Decrypt("ENCRYPTED_secret"), Times.Once);
        result.Should().Be("secret");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnPlainValue_ForNonSensitiveSettings()
    {
        // Arrange
        _db.AppSettings.Add(new AppSetting { Key = "Test:NotSensitive", Value = "plaintext", IsSensitive = false });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetAsync("Test:NotSensitive", TestContext.Current.CancellationToken);

        // Assert
        _encryption.Verify(e => e.Decrypt(It.IsAny<string>()), Times.Never);
        result.Should().Be("plaintext");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenSettingNotFound()
    {
        // Act
        var result = await _sut.GetAsync("NonExistent:Key", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
