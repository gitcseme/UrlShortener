namespace UrlShortener.Api.Models;

public class ShortenedUrl
{
    public long Id { get; set; }
    public string LongUrl { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int ClickCount { get; set; }
}
