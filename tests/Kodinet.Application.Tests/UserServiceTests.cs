using FluentAssertions;
using KodiNet.Application.DTOs;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using KodiNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KodiNet.Application.Tests;

public sealed class UserServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly UserService _sut;  // System Under Test
    private readonly string _dbName;

    public UserServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // Factory creates fresh contexts (to avoid ObjectDisposedException)
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

        _sut = new UserService(_dbFactory);
    }


    [Fact]
    public async Task UpsertAsync_ShouldAssignOwnerRole_WhenFirstUser()
    {
        // Arrange — empty DB, no role assigned yet
        var user = new NewUserRequest
        (    
            MicrosoftOid: "aaa-bbb",
            DisplayName: "Alice",
            Email: "alice@company.com",
            LastLoginAt: DateTime.UtcNow
        );

        // Act
        var result = await _sut.UpsertAsync(user, TestContext.Current.CancellationToken);

        // Assert — use fresh context to avoid ObjectDisposedException
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .Options);
        var roles = await assertDb.UserRoles.Where(ur => ur.AppUserId == result.Id).ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        roles.Should().ContainSingle()
            .Which.AppRoleId.Should().Be(4);  // Owner role
    }

    [Fact]
    public async Task UpsertAsync_ShouldNotAssignOwner_WhenUsersAlreadyExist()
    {
        // Arrange — Owner already exists
        var existing  = new AppUser
        {
            MicrosoftOid = "existing",
            DisplayName = "Bob",
            Email = "bob@co.com",
            LastLoginAt = DateTime.UtcNow
        };
        _db.Users.Add(existing);
        _db.UserRoles.Add(new UserRole { AppUserId = existing.Id, AppRoleId = 3 });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var newUser = new NewUserRequest
        (
            MicrosoftOid: "new-user",
            DisplayName: "Carol",
            Email: "carol@co.com",
            LastLoginAt: DateTime.UtcNow
        );

        // Act
        await _sut.UpsertAsync(newUser, TestContext.Current.CancellationToken);

        // Assert — use fresh context to avoid ObjectDisposedException
        using var assertDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                   .UseInMemoryDatabase(_dbName)
                   .Options);
        var carolRoles = await assertDb.UserRoles
                .Where(ur => ur.AppUser.MicrosoftOid == "new-user").ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        carolRoles.Should().BeEmpty();
    }

    public void Dispose() => _db.Dispose();
}