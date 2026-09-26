using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using UrlShortener.Api.Data;
using UrlShortener.Api.Exceptions;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Tests;

public class UrlShorteningServiceTests
{
    private const string BaseUrl = "https://short.test";

    private static UrlShorteningService CreateService(List<ShortenedUrl> data)
    {
        var dbSetMock = data.AsQueryable().BuildMockDbSet();

        dbSetMock.Setup(m => m.Add(It.IsAny<ShortenedUrl>()))
            .Callback<ShortenedUrl>(entity =>
            {
                entity.Id = data.Count == 0 ? 1 : data.Max(u => u.Id) + 1;
                data.Add(entity);
            });

        var contextMock = new Mock<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().Options);
        contextMock.Setup(c => c.Set<ShortenedUrl>()).Returns(dbSetMock.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new UrlShorteningService(contextMock.Object);
    }

    [Fact]
    public async Task ShortenUrlAsync_NoAlias_GeneratesBase62ShortCodeAndPersists()
    {
        var data = new List<ShortenedUrl>();
        var service = CreateService(data);

        var result = await service.ShortenUrlAsync("https://example.com", null, BaseUrl);

        Assert.Equal("1", result.ShortCode);
        Assert.Equal("https://short.test/1", result.ShortUrl);
        Assert.Equal("https://example.com", result.LongUrl);
        Assert.Single(data);
    }

    [Fact]
    public async Task ShortenUrlAsync_NoAlias_ReturnsExistingEntity_WhenLongUrlAlreadyShortened()
    {
        var data = new List<ShortenedUrl>
        {
            new() { Id = 1, LongUrl = "https://example.com", ShortCode = "abc", CreatedAt = DateTimeOffset.UtcNow }
        };
        var service = CreateService(data);

        var result = await service.ShortenUrlAsync("https://example.com", null, BaseUrl);

        Assert.Equal("abc", result.ShortCode);
        Assert.Single(data);
    }

    [Fact]
    public async Task ShortenUrlAsync_WithAlias_UsesProvidedAlias()
    {
        var data = new List<ShortenedUrl>();
        var service = CreateService(data);

        var result = await service.ShortenUrlAsync("https://example.com", "my-alias", BaseUrl);

        Assert.Equal("my-alias", result.ShortCode);
        Assert.Equal("https://short.test/my-alias", result.ShortUrl);
    }

    [Fact]
    public async Task ShortenUrlAsync_WithAlias_ThrowsWhenAliasAlreadyExists()
    {
        var data = new List<ShortenedUrl>
        {
            new() { Id = 1, LongUrl = "https://taken.com", ShortCode = "my-alias", CreatedAt = DateTimeOffset.UtcNow }
        };
        var service = CreateService(data);

        await Assert.ThrowsAsync<AliasAlreadyExistsException>(
            () => service.ShortenUrlAsync("https://example.com", "my-alias", BaseUrl));
    }

    [Fact]
    public async Task GetLongUrlAsync_ReturnsLongUrlAndIncrementsClickCount()
    {
        var entity = new ShortenedUrl { Id = 1, LongUrl = "https://example.com", ShortCode = "abc", ClickCount = 0 };
        var data = new List<ShortenedUrl> { entity };
        var service = CreateService(data);

        var longUrl = await service.GetLongUrlAsync("abc");

        Assert.Equal("https://example.com", longUrl);
        Assert.Equal(1, entity.ClickCount);
    }

    [Fact]
    public async Task GetLongUrlAsync_ReturnsNull_WhenShortCodeNotFound()
    {
        var service = CreateService(new List<ShortenedUrl>());

        var longUrl = await service.GetLongUrlAsync("missing");

        Assert.Null(longUrl);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsStats_WhenFound()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var data = new List<ShortenedUrl>
        {
            new() { Id = 1, LongUrl = "https://example.com", ShortCode = "abc", CreatedAt = createdAt, ClickCount = 3 }
        };
        var service = CreateService(data);

        var stats = await service.GetStatsAsync("abc");

        Assert.NotNull(stats);
        Assert.Equal("abc", stats!.ShortCode);
        Assert.Equal("https://example.com", stats.LongUrl);
        Assert.Equal(createdAt, stats.CreatedAt);
        Assert.Equal(3, stats.ClickCount);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsNull_WhenNotFound()
    {
        var service = CreateService(new List<ShortenedUrl>());

        var stats = await service.GetStatsAsync("missing");

        Assert.Null(stats);
    }
}
