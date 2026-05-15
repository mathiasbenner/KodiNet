using FluentAssertions;
using KodiNet.Domain.Entities;
using KodiNet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KodiNet.Infrastructure.Tests.Data;

public sealed class AppDbContextAppThemesSeedTests : IDisposable
{
    private readonly AppDbContext _db;

    public AppDbContextAppThemesSeedTests()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));

        var options = optionsBuilder.Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public void Database_ShouldBeSeedWith3Themes()
    {
        // Act
        var themes = _db.AppThemes.ToList();

        // Assert
        themes.Should().HaveCount(3, "exactly 3 default themes must be seeded");
    }

    [Fact]
    public void Database_ShouldContainRubyTheme()
    {
        // Act
        var theme = _db.AppThemes.FirstOrDefault(t => t.Name == "Ruby");

        // Assert
        theme.Should().NotBeNull("Ruby theme must be seeded");
        theme!.Id.Should().Be(1);
        theme.SwatchColor.Should().Be("#9D344B");
        theme.IsSystem.Should().BeTrue();
        theme.LightPaletteJson.Should().NotBeNullOrEmpty();
        theme.DarkPaletteJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Database_ShouldContainOceanTheme()
    {
        // Act
        var theme = _db.AppThemes.FirstOrDefault(t => t.Name == "Ocean");

        // Assert
        theme.Should().NotBeNull("Ocean theme must be seeded");
        theme!.Id.Should().Be(2);
        theme.SwatchColor.Should().Be("#28546C");
        theme.IsSystem.Should().BeTrue();
        theme.LightPaletteJson.Should().NotBeNullOrEmpty();
        theme.DarkPaletteJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Database_ShouldContainAmberTheme()
    {
        // Act
        var theme = _db.AppThemes.FirstOrDefault(t => t.Name == "Amber");

        // Assert
        theme.Should().NotBeNull("Amber theme must be seeded");
        theme!.Id.Should().Be(3);
        theme.SwatchColor.Should().Be("#AA6C39");
        theme.IsSystem.Should().BeTrue();
        theme.LightPaletteJson.Should().NotBeNullOrEmpty();
        theme.DarkPaletteJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Database_ShouldSeedAllThemesWithCorrectNames()
    {
        // Act
        var themes = _db.AppThemes.OrderBy(t => t.Id).ToList();

        // Assert
        themes.Should().HaveCount(3);
        themes[0].Name.Should().Be("Ruby");
        themes[1].Name.Should().Be("Ocean");
        themes[2].Name.Should().Be("Amber");
    }

    [Fact]
    public void Database_ShouldMarkAllThemesAsSystemThemes()
    {
        // Act
        var themes = _db.AppThemes.ToList();

        // Assert
        themes.Should().AllSatisfy(t => t.IsSystem.Should().BeTrue("all seeded themes should be system themes"));
    }

    [Fact]
    public void Database_ShouldSeedThemesWithValidColorSwatches()
    {
        // Act
        var themes = _db.AppThemes.OrderBy(t => t.Id).ToList();

        // Assert
        var expectedSwatchColors = new[] { "#9D344B", "#28546C", "#AA6C39" };

        for (int i = 0; i < themes.Count; i++)
        {
            themes[i].SwatchColor.Should().Be(expectedSwatchColors[i]);
        }
    }

    [Fact]
    public void Database_RubyThemePalettes_ShouldContainRequiredColors()
    {
        // Act
        var theme = _db.AppThemes.First(t => t.Name == "Ruby");

        // Assert
        theme.LightPaletteJson.Should().Contain("\"primary\"");
        theme.LightPaletteJson.Should().Contain("\"background\"");
        theme.LightPaletteJson.Should().Contain("\"textPrimary\"");
        theme.DarkPaletteJson.Should().Contain("\"primary\"");
        theme.DarkPaletteJson.Should().Contain("\"background\"");
        theme.DarkPaletteJson.Should().Contain("\"textPrimary\"");
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
