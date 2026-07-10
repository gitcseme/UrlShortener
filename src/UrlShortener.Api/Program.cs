using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IUrlShorteningService, UrlShorteningService>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/shorten", async (CreateUrlRequest request, IUrlShorteningService service, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(request.Url) ||
        !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "http" && uri.Scheme != "https"))
    {
        return Results.BadRequest(new { error = "A valid http or https URL is required." });
    }

    if (!string.IsNullOrWhiteSpace(request.Alias))
    {
        if (request.Alias.Length is < 3 or > 20)
        {
            return Results.BadRequest(new { error = "Alias must be between 3 and 20 characters." });
        }
        if (!request.Alias.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'))
        {
            return Results.BadRequest(new { error = "Alias may only contain letters, digits, hyphens, and underscores." });
        }
    }

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";

    try
    {
        var result = await service.ShortenUrlAsync(request.Url, request.Alias, baseUrl);
        return Results.Created(result.ShortUrl, result);
    }
    catch (AliasAlreadyExistsException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapGet("/{shortCode}", async (string shortCode, IUrlShorteningService service) =>
{
    var longUrl = await service.GetLongUrlAsync(shortCode);
    if (longUrl == null) return Results.NotFound();

    return Results.Redirect(longUrl);
});

app.MapGet("/api/{shortCode}/stats", async (string shortCode, IUrlShorteningService service) =>
{
    var stats = await service.GetStatsAsync(shortCode);
    if (stats == null) return Results.NotFound();

    return Results.Ok(stats);
});

app.Run();

public record CreateUrlRequest(string Url, string? Alias = null);
