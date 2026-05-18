using FluentAssertions;
using KodiNet.Infrastructure.Parsing;

namespace KodiNet.Infrastructure.Tests.Parsing;

public sealed class StoragePathResolverTests
{
    // ── RootPath = /videos ────────────────────────────────────

    private readonly StoragePathResolver _sut = new("/videos");

    [Theory]
    [InlineData("/", "/videos")]    // virtual root → RootPath
    [InlineData(null, "/videos")]   // null
    [InlineData("", "/videos")]     // empty
    public void Resolve_ShouldReturnRootPath_ForRootInputs(string? input, string expected)
    {
        _sut.Resolve(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("/videos/films", "/videos/films")]      // already absolute under root
    [InlineData("/videos/films/sci-fi", "/videos/films/sci-fi")]
    public void Resolve_ShouldReturnAsIs_WhenAlreadyUnderRoot(string input, string expected)
    {
        _sut.Resolve(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("films", "/videos/films")]                  // simple relative
    [InlineData("films/sci-fi", "/videos/films/sci-fi")]    // relative with subfolder
    public void Resolve_ShouldPrefixWithRoot_ForRelativePaths(string input, string expected)
    {
        _sut.Resolve(input).Should().Be(expected);
    }

    // ── RootPath = / (no prefixes) ─────────────────────────────────────────

    [Fact]
    public void Resolve_WithEmptyRoot_ShouldReturnSlash_ForRootInput()
    {
        var sut = new StoragePathResolver("/");
        sut.Resolve("/").Should().Be("/");
    }

    [Fact]
    public void Resolve_WithEmptyRoot_ShouldReturnPath_ForAbsoluteInput()
    {
        var sut = new StoragePathResolver("/");
        sut.Resolve("/films").Should().Be("/films");
    }

    // ── Robustness ────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_ShouldTrimTrailingSlash_FromRootPath()
    {
        // RootPath with trailing slash — must be normalized
        var sut = new StoragePathResolver("/videos/");
        sut.Resolve("/").Should().Be("/videos");
    }

    [Fact]
    public void Resolve_ShouldNotDoubleSlash_WhenConcatenating()
    {
        // Avoid /videos//films
        _sut.Resolve("/films").Should().NotContain("//");
    }
}