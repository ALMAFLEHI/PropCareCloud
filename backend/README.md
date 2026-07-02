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

## Notes

Real RDS connectivity, migrations, authentication, authorization rules, and AWS deployment will be added in later sprints. No production secrets, AWS credentials, or real database connection strings are configured in this sprint.
