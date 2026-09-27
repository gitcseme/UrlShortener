using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Exceptions;
using UrlShortener.Api.Services;
using UrlShortener.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddScoped<IUrlShorteningService, UrlShorteningService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUrlRequestValidator>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options?
        .WithTitle("My .NET 10 API")
        .WithTheme(ScalarTheme.Mars);
});

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.MapPost("/shorten",
    async (CreateUrlRequest request,
    IValidator<CreateUrlRequest> validator,
    IUrlShorteningService service,
    HttpContext http) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var result = await service.ShortenUrlAsync(request.Url, request.Alias, baseUrl);

    return Results.Created(result.ShortUrl, result);
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

app.MapGet("/api/env", () =>
{
    var variables = Environment.GetEnvironmentVariables()
        .Cast<System.Collections.DictionaryEntry>()
        .ToDictionary(kv => kv.Key, kv => kv.Value);

    return Results.Ok(variables);
});

using var scope = app.Services.CreateScope();
using var db = scope.ServiceProvider.GetService<AppDbContext>();
db?.Database.Migrate();


app.Run();
