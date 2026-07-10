namespace UrlShortener.Api.Services;

public record ShortenResult(string ShortCode, string ShortUrl, string LongUrl);
public record StatsResult(string ShortCode, string LongUrl, DateTimeOffset CreatedAt, int ClickCount);

public interface IUrlShorteningService
{
    Task<ShortenResult> ShortenUrlAsync(string longUrl, string? alias, string baseUrl);
    Task<string?> GetLongUrlAsync(string shortCode);
    Task<StatsResult?> GetStatsAsync(string shortCode);
}
