using FluentAssertions;
using KodiNet.Application.DTOs;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KodiNet.Application.Tests;

public sealed class EstablishmentServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly EstablishmentService _sut;
    private readonly string _dbName;

    public EstablishmentServiceTests()
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

        _sut = new EstablishmentService(_dbFactory);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoEstablishmentsExist()
    {
        // Act
        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEstablishments_OrderedByName()
    {
        // Arrange
        _db.Establishments.AddRange(
            new Establishment { Name = "Zeta House", Address = "123 Main St" },
            new Establishment { Name = "Alpha House", Address = "456 Oak Ave" },
            new Establishment { Name = "Beta House", Address = "789 Pine Rd" }
        );
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha House");
        result[1].Name.Should().Be("Beta House");
        result[2].Name.Should().Be("Zeta House");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateEstablishment_WithNameAndAddress()
    {
        // Act
        var result = await _sut.CreateAsync("New House", "999 New St", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New House");
        result.Address.Should().Be("999 New St");
        result.Id.Should().BeGreaterThan(0);

        // Verify in database
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                   .Options);
        var stored = await assertDb.Establishments.FirstOrDefaultAsync(e => e.Name == "New House", TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.Address.Should().Be("999 New St");
    }

    [Fact]
    public async Task CreateAsync_ShouldTrimWhitespace()
    {
        // Act
        var result = await _sut.CreateAsync("  Trimmed House  ", "  123 Test St  ", TestContext.Current.CancellationToken);

        // Assert
        result.Name.Should().Be("Trimmed House");
        result.Address.Should().Be("123 Test St");
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateWithNullAddress()
    {
        // Act
        var result = await _sut.CreateAsync("No Address House", null, TestContext.Current.CancellationToken);

        // Assert
        result.Address.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEstablishment()
    {
        // Arrange
        var establishment = new Establishment { Name = "Original", Address = "Old Address" };
        _db.Establishments.Add(establishment);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.UpdateAsync(establishment.Id, "Updated", "New Address", TestContext.Current.CancellationToken);

        // Assert
        result.Name.Should().Be("Updated");
        result.Address.Should().Be("New Address");

        // Verify in database
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
                   .Options);
        var stored = await assertDb.Establishments.FirstOrDefaultAsync(e => e.Id == establishment.Id, TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Updated");
        stored.Address.Should().Be("New Address");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenEstablishmentNotFound()
    {
        // Act & Assert
        await _sut.Invoking(s => s.UpdateAsync(999, "Name", "Address"))
            .Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowInvalidOperation_WhenEstablishmentHasPis()
    {
        // Arrange
        var establishment = new Establishment { Name = "Has Pis", Address = "Delete Me" };
        _db.Establishments.Add(establishment);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pi = new RaspberryPi
        {
            Name = "Pi 1",
            IpAddress = "192.168.1.1",
            EstablishmentId = establishment.Id,
            Location = "Room",
            KodiUserEncrypted = "",
            KodiPasswordEncrypted = "",
            SshUserEncrypted = "",
            SshPasswordEncrypted = ""
        };
        _db.RaspberryPis.Add(pi);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await _sut.Invoking(s => s.DeleteAsync(establishment.Id, TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot delete an establishment that contains Pis*");
    }

    public void Dispose() => _db.Dispose();
}
