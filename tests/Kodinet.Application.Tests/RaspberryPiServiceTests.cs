using FluentAssertions;
using KodiNet.Application.DTOs;
using KodiNet.Domain.Entities;
using KodiNet.Domain.Interfaces.Services;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KodiNet.Application.Tests;

public sealed class RaspberryPiServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly Mock<ICredentialEncryption> _encryption;
    private readonly RaspberryPiService _sut;   // System Under Test
    private readonly string _dbName;

    public RaspberryPiServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // Factory qui retourne toujours le même contexte
        var mockFactory = new Mock<IDbContextFactory<AppDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                   .Returns(() =>
                   {
                       var newContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                           .UseInMemoryDatabase(_dbName)
                           .Options);
                       return Task.FromResult<AppDbContext>(newContext);
                   });
        _dbFactory = mockFactory.Object;

        // Encryption simulée — retourne simplement la valeur telle quelle
        _encryption = new Mock<ICredentialEncryption>();
        _encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _encryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);

        _sut = new RaspberryPiService(_dbFactory, _encryption.Object);
    }

    // ── Tests IP en double ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenIpAlreadyExists()
    {
        // Arrange — un Pi existe déjà avec cette IP
        var establishment = new Establishment { Id = 1, Name = "Test" };
        _db.Establishments.Add(establishment);
        _db.RaspberryPis.Add(new RaspberryPi
        {
            Name = "Pi existant",
            IpAddress = "192.168.1.10",
            EstablishmentId = 1,
            Location = "A",
            KodiUserEncrypted = "",
            KodiPasswordEncrypted = "",
            SshUserEncrypted = "",
            SshPasswordEncrypted = ""
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreatePiRequest(
            "Nouveau Pi", "192.168.1.10", 1, "B", "Pi 4B",
            "kodi", "pass", 8080, "pi", "pass", 22, "/storage/videos");

        // Act
        var act = async () => await _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*192.168.1.10*");
    }

    [Fact]
    public async Task CreateAsync_ShouldSucceed_WhenIpIsUnique()
    {
        // Arrange
        _db.Establishments.Add(new Establishment { Id = 1, Name = "Test" });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new CreatePiRequest(
            "Pi A", "192.168.1.20", 1, "Salle", "Pi 4B",
            "kodi", "pass", 8080, "pi", "pass", 22, "/storage/videos");

        // Act
        var result = await _sut.CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IpAddress.Should().Be("192.168.1.20");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenIpConflictsWithAnotherPi()
    {
        // Arrange — deux Pi existants
        _db.Establishments.Add(new Establishment { Id = 1, Name = "Test" });
        _db.RaspberryPis.AddRange(
            new RaspberryPi
            {
                Id = 1,
                Name = "Pi A",
                IpAddress = "192.168.1.10",
                EstablishmentId = 1,
                Location = "A",
                KodiUserEncrypted = "",
                KodiPasswordEncrypted = "",
                SshUserEncrypted = "",
                SshPasswordEncrypted = ""
            },
            new RaspberryPi
            {
                Id = 2,
                Name = "Pi B",
                IpAddress = "192.168.1.11",
                EstablishmentId = 1,
                Location = "B",
                KodiUserEncrypted = "",
                KodiPasswordEncrypted = "",
                SshUserEncrypted = "",
                SshPasswordEncrypted = ""
            });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Tenter de donner l'IP du Pi A au Pi B
        var request = new UpdatePiRequest(
            2, "Pi B", "192.168.1.10", 1, "B", "Pi 4B",
            null, null, 8080, null, null, 22, "/storage/videos");

        // Act & Assert
        await FluentActions.Awaiting(() => _sut.UpdateAsync(request))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    public void Dispose() => _db.Dispose();
}