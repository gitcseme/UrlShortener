# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A minimal ASP.NET Core URL shortener API (.NET 10, minimal APIs). Database-generated `Id` (long) is base-62 encoded to produce the short code.

## Commands

```
# Build
dotnet build

# Run the API (from src/UrlShortener.Api)
dotnet run --project src/UrlShortener.Api

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~UrlShortener.Tests.ClassName.MethodName"

# Start SQL Server dependency (required before running the API)
docker-compose up -d

# EF Core migrations (run from src/UrlShortener.Api, or add --project/--startup-project)
dotnet ef migrations add <Name> --project src/UrlShortener.Api
dotnet ef database update --project src/UrlShortener.Api
```

The API expects SQL Server reachable via the `SqlServer` connection string in `appsettings.json` (defaults to `localhost,1433`, sa/`YourStrong!Passw0rd`, matching `docker-compose.yml`).

## Architecture

- **Minimal API, no controllers**: all HTTP endpoints are mapped directly in `Program.cs` (`POST /shorten`, `GET /{shortCode}`, `GET /api/{shortCode}/stats`). Add new endpoints there rather than introducing a controller layer.
- **Service layer**: `IUrlShorteningService` / `UrlShorteningService` (`Services/`) holds all business logic — shortening, alias handling, redirect lookup (increments click count), and stats. Endpoints stay thin and delegate to this service.
- **Short code generation**: an entity is first inserted with no `ShortCode` to obtain its DB-generated `Id`, then `Id` is base-62 encoded (custom alphabet in `UrlShorteningService`) and saved back as `ShortCode` in a second `SaveChangesAsync`. Custom aliases skip this and are checked for uniqueness up front (`AliasAlreadyExistsException` → HTTP 409).
- **Idempotent shortening**: when no alias is given, an existing row with the same `LongUrl` is reused instead of creating a duplicate.
- **Validation**: request validation uses FluentValidation (`Validators/CreateUrlRequestValidator.cs`), registered via `AddValidatorsFromAssemblyContaining`. Endpoints call the validator manually and return `Results.ValidationProblem` on failure — validation is not wired through automatic ASP.NET filters.
- **Error handling**: `GlobalExceptionHandler` (`Exceptions/`) is the single place mapping exceptions to `ProblemDetails` responses. Add new domain exceptions there as new switch cases rather than handling errors ad hoc in endpoints.
- **Data layer**: EF Core with SQL Server (`Data/AppDbContext.cs`), single entity `Models/ShortenedUrl.cs` (`ShortCode` has a unique index, max length 10). Migrations live in `src/UrlShortener.Api/Migrations/`.
- **Projects**: `src/UrlShortener.Api` (web API) and `tests/UrlShortener.Tests` (xUnit, references the API project). Solution file is `UrlShortener.slnx`.
