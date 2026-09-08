# AssetsManagement

.NET 8 ASP.NET Core API for the TEAIP Asset Data administration area. The solution uses Clean
Architecture, EF Core 8 with SQL Server, ASP.NET Core Identity, JWT bearer authentication,
FluentValidation, Swagger, soft deletion, audit fields, safe local image storage, and relational tests.

## Projects

- `src/AssetsManagement.Domain` — entities, enums, exceptions, and domain rules.
- `src/AssetsManagement.Application` — API contracts, DTOs, validators, and abstractions.
- `src/AssetsManagement.Infrastructure` — EF Core, Identity, JWT, SQL Server, seeding, and file storage.
- `src/AssetsManagement.Api` — HTTP endpoints, security, Swagger, rate limiting, and error handling.
- `tests/AssetsManagement.UnitTests` and `tests/AssetsManagement.IntegrationTests`.

## Prerequisites

- .NET 8 SDK
- SQL Server 2019+, SQL Server Express, or LocalDB

## Configuration

Set production secrets with environment variables or a secret provider:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=AssetsManagement;..."
$env:Jwt__Issuer = "AssetsManagement"
$env:Jwt__Audience = "AssetsManagement.Client"
$env:Jwt__SigningKey = "a-random-secret-of-at-least-32-bytes"
```

`appsettings.Development.json` contains a clearly marked development-only signing key and a LocalDB
connection. Never use that key in production. CORS origins are configured under `Cors:AllowedOrigins`.

## Build, migrate, and run

```powershell
dotnet restore AssetsManagement.sln
dotnet build AssetsManagement.sln
dotnet ef database update --project src/AssetsManagement.Infrastructure --startup-project src/AssetsManagement.Api -- --environment Development
dotnet run --project src/AssetsManagement.Api --launch-profile http
dotnet test AssetsManagement.sln
```

Swagger is available at the URL printed by `dotnet run`, followed by `/swagger` (the development
HTTP profile uses `http://localhost:5288/swagger`). Health is available at `/health`.

## Authentication

Startup migration and seeding are idempotent. The development administrator is:

- Username: `admin`
- Password: `Admin@123`

Call `POST /api/auth/login`, copy `data.accessToken`, select **Authorize** in Swagger, and enter the
token. Swagger's HTTP bearer scheme adds the `Bearer` prefix. Change the seeded credential for any
shared environment.

## Image storage

Image metadata is stored in SQL Server. File content is stored under
`src/AssetsManagement.Api/App_Data/asset-images` with generated names and is served only through
`GET /api/asset-images/{id}/content`. JPEG, PNG, and WebP files up to 10 MB are accepted. Storage is
behind `IFileStorageService` so object storage can replace the local implementation.

## Source-document limitation

Only `TEAIP_Prototype.html` was present in the supplied workspace. The eleven requested TEAIP PDS
DOCX files were absent. See `IMPLEMENTATION_REPORT.md` for affected assumptions and unresolved PDS
rules. The prototype was not modified.
