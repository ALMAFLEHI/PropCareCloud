# PropCareCloud.Api Backend

PropCareCloud.Api is the ASP.NET Core Web API backend foundation for PropCare Cloud.

## Technology

- ASP.NET Core Web API
- .NET 8
- Controllers
- Swagger/OpenAPI in Development
- xUnit test project
- Entity Framework Core 8
- Npgsql Entity Framework Core PostgreSQL provider

## Restore

```powershell
dotnet restore .\backend\PropCareCloud.sln
```

## Build

```powershell
dotnet build .\backend\PropCareCloud.sln
```

## Run

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

## Test

```powershell
dotnet test .\backend\PropCareCloud.sln
```

## Current Endpoints

- `GET /api/health`
- `GET /api/system-info`
- `GET /api/domain-summary`
- `GET /api/database/status`
- `GET /api/database/readiness`
- `POST /api/seed/demo-data`

## Sprint 4 Database Domain Foundation

Sprint 4 adds the backend domain model and EF Core database foundation.

Domain entities added:

- `UserProfile`
- `Property`
- `RentalUnit`
- `MaintenanceRequest`
- `MaintenanceRequestComment`
- `MaintenanceRequestAttachment`

EF Core additions:

- `AppDbContext`
- Fluent API table and relationship configuration
- Enum-to-string storage configuration
- Planned database provider: Amazon RDS PostgreSQL

The application only registers `AppDbContext` when a `DefaultConnection` connection string exists. The current `appsettings.json` contains an empty placeholder only.

## Sprint 5 Migration and PostgreSQL Setup

Sprint 5 adds the EF Core migration and safe PostgreSQL setup foundation.

- EF Core Design package added for migration tooling.
- Local `dotnet-ef` tool configured through `.config/dotnet-tools.json`.
- Initial migration created: `InitialCreate`.
- Migration files: `backend/src/PropCareCloud.Api/Data/Migrations`.
- Local PostgreSQL setup documentation: `docs/architecture/postgresql_local_setup.md`.
- Database status endpoint added: `GET /api/database/status`.

Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
```

Optional local database update command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
```

The optional update script requires `PROPCLOUD_CONNECTION_STRING` to be set and does not print the connection string.

## Sprint 6 Local PostgreSQL and Seed Data Foundation

Sprint 6 adds safe local PostgreSQL readiness checks and demo seed data support.

- Database readiness endpoint added: `GET /api/database/readiness`.
- Local demo seed endpoint added: `POST /api/seed/demo-data`.
- Seed data creates sample owners, managers, tenants, maintenance staff, properties, units, requests, comments, and fake future S3-style attachment metadata.
- The seed endpoint returns a safe `400 Bad Request` if no database connection is configured.
- No real credentials are stored in committed configuration files.

Local setup and validation scripts:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-local-postgresql.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
```

Use user-secrets or environment variables for local database credentials. Do not commit real connection strings or passwords.

## Notes

Real RDS connectivity, migrations, authentication, authorization rules, and AWS deployment will be added in later sprints. No production secrets, AWS credentials, or real database connection strings are configured in this sprint.
