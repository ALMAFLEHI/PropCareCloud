# Sprint 05 Database Migration PostgreSQL Setup

## Sprint Name

Sprint 5: Database Migration & PostgreSQL Setup

## Sprint Goal

Prepare the EF Core migration and PostgreSQL setup foundation for PropCare Cloud. This sprint creates the initial database migration, adds safe local PostgreSQL setup documentation/scripts, adds database status checking, and keeps the system buildable without requiring a real database connection.

## Date/Time

Sprint closure completed on 2026-07-02 at 17:58 +03:00.

## Files Created/Changed

Created or updated EF Core migration tooling:

- `.config/dotnet-tools.json`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj`
- `backend/src/PropCareCloud.Api/Data/AppDbContextFactory.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260702144954_InitialCreate.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260702144954_InitialCreate.Designer.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/AppDbContextModelSnapshot.cs`

Created or updated database status API files:

- `backend/src/PropCareCloud.Api/Services/DatabaseStatusService.cs`
- `backend/src/PropCareCloud.Api/Controllers/DatabaseStatusController.cs`
- `backend/src/PropCareCloud.Api/Program.cs`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.http`

Created or updated scripts:

- `scripts/check-database-migration.ps1`
- `scripts/update-local-database.ps1`

Created or updated tests and documentation:

- `backend/tests/PropCareCloud.Api.Tests/DatabaseStatusServiceTests.cs`
- `backend/README.md`
- `README.md`
- `docs/architecture/database_design.md`
- `docs/architecture/postgresql_local_setup.md`
- `docs/sprints/sprint_05_database_migration_postgresql_setup.md`

## EF Core Migration Summary

- EF Core Design package added: `Microsoft.EntityFrameworkCore.Design` 8.0.22.
- EF Core relational runtime aligned with `Microsoft.EntityFrameworkCore.Relational` 8.0.22.
- Local EF tool configured: `dotnet-ef` 8.0.x.
- Migration created: `InitialCreate`.
- Migration files are committed for schema tracking.
- No database update is required for normal validation.

## Migration Files Location

`backend/src/PropCareCloud.Api/Data/Migrations`

The migration covers:

- `user_profiles`
- `properties`
- `rental_units`
- `maintenance_requests`
- `maintenance_request_comments`
- `maintenance_request_attachments`

## Database Status Endpoint Summary

- Endpoint: `GET /api/database/status`
- Reports provider and setup status safely.
- Does not expose host, username, password, or full connection string.
- Works even when no real database connection exists.

## Local PostgreSQL Setup Summary

Local setup guidance is documented in `docs/architecture/postgresql_local_setup.md`.

Recommended database name:

```text
propcarecloud_db
```

Secrets should be supplied through user secrets or environment variables. No real database credentials are stored in source control.

## Scripts Created

- `scripts/check-database-migration.ps1`
  - Restores local tools.
  - Checks `dotnet-ef` version.
  - Confirms migration files exist.
  - Runs restore/build/test.
  - Does not run database update.
  - Does not require PostgreSQL.
- `scripts/update-local-database.ps1`
  - Optional local-only database update helper.
  - Requires `PROPCLOUD_CONNECTION_STRING`.
  - Does not print the connection string.
  - Does not use AWS.

## Tests Added

- `DatabaseStatusServiceTests`
  - Confirms provider is PostgreSQL.
  - Confirms planned cloud provider mentions Amazon RDS PostgreSQL.
  - Confirms database name suggestion is `propcarecloud_db`.
  - Confirms the response does not expose a connection string.
  - Confirms current sprint mentions Sprint 5.
  - Confirms migrations have been created.

Previous tests remain in place.

## Commands/Checks Performed

Package and tool setup:

```powershell
dotnet add backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 8.0.22
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.0.22
dotnet tool restore
dotnet tool run dotnet-ef --version
```

Migration creation:

```powershell
dotnet tool run dotnet-ef migrations add InitialCreate --project backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj --startup-project backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj --output-dir Data\Migrations
```

Validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

Manual API check:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

## Build/Test Results

- `check-database-migration.ps1`: PASS
  - `dotnet tool restore`: PASS
  - `dotnet-ef --version`: PASS, 8.0.22
  - Migration files: PASS, 3 C# migration files found
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 9 tests passed
- `check-backend.ps1`: PASS
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 9 tests passed

## Manual API Check

- Backend started locally at `http://localhost:5015`.
- Swagger document returned HTTP 200.
- Swagger includes `/api/database/status`.
- `GET /api/database/status` returned HTTP 200.
- Response confirmed:
  - `provider`: `PostgreSQL`
  - `plannedCloudProvider`: `Amazon RDS PostgreSQL`
  - `databaseNameSuggestion`: `propcarecloud_db`
  - `connectionStringConfigured`: `false`
  - `migrationsCreated`: `true`
- Response did not expose host, username, password, or full connection string.

## Issues Found

- Initial sandboxed validation could not read the user NuGet configuration at `C:\Users\devls\AppData\Roaming\NuGet\NuGet.Config`.
- Validation passed after rerunning with approved access.
- No project build or test failures remained.

## What Was Intentionally Not Done Yet

- No Amazon RDS connection was configured.
- No real database credentials were added.
- No database update is required.
- No CRUD features were implemented.
- No authentication was implemented.
- No AWS deployment was performed.

## Evidence Screenshot Needed Later

- `sprint_05_database_status_swagger.png`

## Final Status

COMPLETE

Sprint 5 is complete because the migration foundation exists, the database status endpoint works, restore/build/test pass, and `check-database-migration.ps1` passes.
