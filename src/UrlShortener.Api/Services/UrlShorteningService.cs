using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class AliasAlreadyExistsException(string alias) : Exception($"Alias '{alias}' is already taken.");

public class UrlShorteningService : IUrlShorteningService
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly int Base = Alphabet.Length;

    private readonly AppDbContext _db;

    public UrlShorteningService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShortenResult> ShortenUrlAsync(string longUrl, string? alias, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            var conflict = await _db.ShortenedUrls.AnyAsync(u => u.ShortCode == alias);
            if (conflict)
            {
                throw new AliasAlreadyExistsException(alias);
            }

            var entity = new ShortenedUrl
            {
                LongUrl = longUrl,
                ShortCode = alias,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ShortenedUrls.Add(entity);
            await _db.SaveChangesAsync();

            return ToResult(entity, baseUrl);
        }

        var existing = await _db.ShortenedUrls.FirstOrDefaultAsync(u => u.LongUrl == longUrl);
        if (existing != null)
        {
            return ToResult(existing, baseUrl);
        }

        var autoEntity = new ShortenedUrl
        {
            LongUrl = longUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ShortenedUrls.Add(autoEntity);
        await _db.SaveChangesAsync();

        autoEntity.ShortCode = Encode(autoEntity.Id);
        await _db.SaveChangesAsync();

        return ToResult(autoEntity, baseUrl);
    }

    public async Task<string?> GetLongUrlAsync(string shortCode)
    {
        var entity = await _db.ShortenedUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
        if (entity == null) return null;

        entity.ClickCount++;
        await _db.SaveChangesAsync();

        return entity.LongUrl;
    }

    public async Task<StatsResult?> GetStatsAsync(string shortCode)
    {
        var entity = await _db.ShortenedUrls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
        if (entity == null) return null;

        return new StatsResult(entity.ShortCode, entity.LongUrl, entity.CreatedAt, entity.ClickCount);
    }

    private static string Encode(long id)
    {
        if (id == 0) return Alphabet[0].ToString();

        var chars = new char[11];
        var pos = chars.Length;

        while (id > 0)
        {
            pos--;
            chars[pos] = Alphabet[(int)(id % Base)];
            id /= Base;
        }

        return new string(chars, pos, chars.Length - pos);
    }

    private static ShortenResult ToResult(ShortenedUrl entity, string baseUrl) =>
        new(entity.ShortCode, $"{baseUrl.TrimEnd('/')}/{entity.ShortCode}", entity.LongUrl);
}
