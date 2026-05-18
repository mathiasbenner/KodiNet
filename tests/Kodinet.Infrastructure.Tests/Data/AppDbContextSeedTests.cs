using FluentAssertions;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Tests.Data;

public sealed class AppDbContextSeedTests : IDisposable
{
    private readonly AppDbContext _db;

    public AppDbContextSeedTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())  // DB unique per test
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();  // Triggers OnModelCreating and SeedInitialData
    }

    [Fact]
    public void Database_ShouldBeSeedWith4Roles()
    {
        // Act
        var roles = _db.Roles.ToList();

        // Assert
        roles.Should().HaveCount(4, "exactly 4 default roles must be seeded");
    }

    [Fact]
    public void Database_ShouldContainAdminRole()
    {
        // Act
        var adminRole = _db.Roles.FirstOrDefault(r => r.Name == "Admin");

        // Assert
        adminRole.Should().NotBeNull("Admin role must be seeded");
        adminRole!.Id.Should().Be(1);
        adminRole.Description.Should().Be("Full access (Pi management, transfers, deletion)");
    }

    [Fact]
    public void Database_ShouldContainOperatorRole()
    {
        // Act
        var operatorRole = _db.Roles.FirstOrDefault(r => r.Name == "Operator");

        // Assert
        operatorRole.Should().NotBeNull("Operator role must be seeded");
        operatorRole!.Id.Should().Be(2);
        operatorRole.Description.Should().Be("Pi monitoring, file transfers");
    }

    [Fact]
    public void Database_ShouldContainViewerRole()
    {
        // Act
        var viewerRole = _db.Roles.FirstOrDefault(r => r.Name == "Viewer");

        // Assert
        viewerRole.Should().NotBeNull("Viewer role must be seeded");
        viewerRole!.Id.Should().Be(3);
        viewerRole.Description.Should().Be("Read-only — view the status of the Pi");
    }

    [Fact]
    public void Database_ShouldContainOwnerRole()
    {
        // Act
        var ownerRole = _db.Roles.FirstOrDefault(r => r.Name == "Owner");

        // Assert
        ownerRole.Should().NotBeNull("Owner role must be seeded");
        ownerRole!.Id.Should().Be(4);
        ownerRole.Description.Should().Be("Sole owner — non-revocable admin rights, cannot be configured via the UI");
    }

    [Fact]
    public void Database_ShouldSeedAllRolesWithCorrectProperties()
    {
        // Act
        var roles = _db.Roles.OrderBy(r => r.Id).ToList();

        // Assert
        roles.Should().HaveCount(4);

        var expectedRoles = new[]
        {
            new { Id = 1, Name = "Admin", Description = "Full access (Pi management, transfers, deletion)" },
            new { Id = 2, Name = "Operator", Description = "Pi monitoring, file transfers" },
            new { Id = 3, Name = "Viewer", Description = "Read-only — view the status of the Pi" },
            new { Id = 4, Name = "Owner", Description = "Sole owner — non-revocable admin rights, cannot be configured via the UI" }
        };

        for (int i = 0; i < roles.Count; i++)
        {
            roles[i].Id.Should().Be(expectedRoles[i].Id);
            roles[i].Name.Should().Be(expectedRoles[i].Name);
            roles[i].Description.Should().Be(expectedRoles[i].Description);
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
