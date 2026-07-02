# Sprint 06 Local PostgreSQL Database Setup and Seed Data

## Sprint Name

Sprint 6: Local PostgreSQL Database Setup & Seed Data

## Sprint Goal

Prepare and validate the local PostgreSQL database setup workflow for PropCare Cloud, add safe seed data support, add database readiness endpoints, and document how the application can be connected to a local PostgreSQL database before moving to Amazon RDS in a later sprint.

## Date/Time

Sprint validation completed on 2026-07-02 at 18:20 +03:00.

## Files Created/Changed

Backend code:

- `backend/src/PropCareCloud.Api/Models/SeedDataResult.cs`
- `backend/src/PropCareCloud.Api/Services/SeedDataService.cs`
- `backend/src/PropCareCloud.Api/Services/DatabaseReadinessService.cs`
- `backend/src/PropCareCloud.Api/Controllers/SeedDataController.cs`
- `backend/src/PropCareCloud.Api/Controllers/DatabaseReadinessController.cs`
- `backend/src/PropCareCloud.Api/Program.cs`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.http`

Tests:

- `backend/tests/PropCareCloud.Api.Tests/SeedDataServiceTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/DatabaseReadinessServiceTests.cs`

Scripts:

- `scripts/setup-local-postgresql.ps1`
- `scripts/check-seed-data.ps1`
- `scripts/update-local-database.ps1`

Documentation:

- `README.md`
- `backend/README.md`
- `docs/architecture/postgresql_local_setup.md`
- `docs/architecture/database_design.md`
- `docs/sprints/sprint_06_local_postgresql_seed_data.md`

## Seed Data Summary

`SeedDataService` creates safe local demo data only when a database connection is configured and `AppDbContext` is registered.

Seed data includes:

- 1 Admin / Owner
- 1 Property Manager
- 2 Tenants
- 2 Maintenance Staff
- 2 Properties
- 4 Rental Units
- 4 Maintenance Requests
- Comments for selected requests
- Attachment metadata for one request using a fake future S3-style `StorageKey`

The service checks existing `UserProfiles` first and skips duplicate records when seed data already exists.

## New Endpoints Created

- `GET /api/database/readiness`
  - Works without a database connection.
  - Reports safe local database readiness metadata.
  - Does not expose connection strings or credentials.
- `POST /api/seed/demo-data`
  - Local development seed endpoint.
  - Returns safe `400 Bad Request` when database connection is not configured.
  - Seeds demo data only when `AppDbContext` is registered.

## Scripts Created

- `scripts/setup-local-postgresql.ps1`
  - Checks `psql` availability.
  - Checks whether `PROPCLOUD_CONNECTION_STRING` exists.
  - Prints safe setup guidance without printing secrets.
- `scripts/check-seed-data.ps1`
  - Checks required seed/readiness files.
  - Checks migration files.
  - Runs restore/build/test.
  - Does not require PostgreSQL.
  - Does not call the seed endpoint.
- `scripts/update-local-database.ps1`
  - Confirms it is local PostgreSQL only.
  - Uses `PROPCLOUD_CONNECTION_STRING`.
  - Does not print the connection string.

## Tests Added

- `SeedDataServiceTests`
  - Confirms first seed creates users, properties, units, requests, comments, and attachments.
  - Confirms second seed skips duplicates.
  - Confirms fake future S3-style storage key is created.
- `DatabaseReadinessServiceTests`
  - Confirms missing connection string returns safe readiness metadata.
  - Confirms provider is PostgreSQL.
  - Confirms planned provider mentions Amazon RDS PostgreSQL.
  - Confirms no connection string values are exposed.

## PostgreSQL Availability Result

`psql --version` result:

```text
psql was not recognized as a command.
```

PostgreSQL is missing or not available on PATH in the current environment. Actual local database update and seed endpoint execution against a real PostgreSQL database are deferred.

## Commands/Checks Performed

Initial checks:

```powershell
git status --short
psql --version
dotnet test .\backend\PropCareCloud.sln
powershell -ExecutionPolicy Bypass -File .\scripts\setup-local-postgresql.ps1
```

Final validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

Manual API check:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

## Build/Test Results

- `check-seed-data.ps1`: PASS
  - Migration files: PASS
  - Seed/readiness files: PASS
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 12 tests passed
- `check-backend.ps1`: PASS
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 12 tests passed
- Direct `dotnet test .\backend\PropCareCloud.sln`: PASS, 12 tests passed

## Manual API Check Result

Backend started locally at `http://localhost:5015` without a configured database connection.

- Swagger document returned HTTP 200.
- Swagger includes `/api/database/readiness`.
- Swagger includes `/api/seed/demo-data`.
- `GET /api/database/readiness` returned HTTP 200.
- Readiness response confirmed:
  - `connectionStringConfigured`: `false`
  - `appDbContextRegistered`: `false`
  - `provider`: `PostgreSQL`
  - `plannedCloudProvider`: `Amazon RDS PostgreSQL`
  - `canConnect`: `null`
- `POST /api/seed/demo-data` returned HTTP 400 safely.
- Seed response message: `Database connection is not configured. Configure local PostgreSQL before seeding.`
- No connection string, host, username, or password was exposed.

## Issues Found

- PostgreSQL `psql` is not installed or not available on PATH.
- `PROPCLOUD_CONNECTION_STRING` is not set in the current shell.
- Actual seed execution against a real local PostgreSQL database is deferred.
- NuGet restore/test commands may require approved access to the user NuGet configuration/cache.

## What Was Intentionally Not Done Yet

- No Amazon RDS connection was configured.
- No real production credentials were added.
- No authentication was implemented.
- No full CRUD features were implemented.
- No AWS deployment was performed.

## Evidence Screenshots Needed Later

- `sprint_06_database_readiness_swagger.png`
- `sprint_06_seed_data_swagger.png` only if local PostgreSQL is configured and seed endpoint runs successfully

## Final Status

PARTIAL

Sprint 6 is PARTIAL because all code, tests, scripts, documentation, and no-database API checks passed, but PostgreSQL/psql is missing and actual seed execution against a real local PostgreSQL database could not be tested yet.
