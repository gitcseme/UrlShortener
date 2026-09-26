# UrlShortener

A simple URL shortener service. Database generated Id is converted to base 62, using as short-code.

## Endpoints
- `POST /shorten` — shorten a URL (optional custom alias)
- `GET /{shortCode}` — redirect to the original URL
- `GET /api/{shortCode}/stats` — get click stats for a short code

## Run locally
```
docker-compose up -d
dotnet run --project src/UrlShortener.Api
```
