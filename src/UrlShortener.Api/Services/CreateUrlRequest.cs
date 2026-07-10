namespace UrlShortener.Api.Services;

public record CreateUrlRequest(string Url, string? Alias = null);
