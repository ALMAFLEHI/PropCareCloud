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

## Notes

Real RDS connectivity, migrations, authentication, authorization rules, and AWS deployment will be added in later sprints. No production secrets, AWS credentials, or real database connection strings are configured in this sprint.
