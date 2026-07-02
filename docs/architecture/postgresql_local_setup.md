# PostgreSQL Local Setup

## Purpose

This document explains the safe local PostgreSQL setup direction for PropCare Cloud. Sprint 5 created EF Core migrations and helper scripts. Sprint 6 adds local readiness checks and demo seed data support, while still allowing normal restore, build, and test validation without PostgreSQL installed.

## Recommended Local Database Name

`propcarecloud_db`

## PostgreSQL Installation

PostgreSQL and `psql` may be installed later if direct local database testing is needed. The current project can still build and test without PostgreSQL installed.

Sprint 6 environment check result:

- `psql`: missing or not available on PATH.
- Actual local database update and seed endpoint execution are deferred until PostgreSQL is installed and configured.

## Local Setup Checklist

1. Install PostgreSQL locally.
2. Confirm `psql --version` works in PowerShell.
3. Create a local database named `propcarecloud_db`.
4. Store the application connection string using user-secrets or an environment variable.
5. Set `PROPCLOUD_CONNECTION_STRING` only for EF Core migration update scripts.
6. Run the migration update script.
7. Start the backend and verify database readiness.
8. Run the local demo seed endpoint.

## Manual Database Creation

After PostgreSQL is installed, create the database manually using a local admin tool or `psql`.

```sql
CREATE DATABASE propcarecloud_db;
```

## Safe Connection Configuration

Do not store real passwords or private connection strings in source control.

### User Secrets Option

```powershell
dotnet user-secrets init --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>" --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj
```

### Environment Variable Option

```powershell
$env:PROPCLOUD_CONNECTION_STRING="Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>"
```

The design-time DbContext factory reads `PROPCLOUD_CONNECTION_STRING` for EF Core tooling. If it is missing, the factory uses a password-free local placeholder only for migration generation.

For the running API, use user-secrets or the standard .NET environment variable form:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=propcarecloud_db;Username=postgres;Password=<your-local-password>"
```

Do not commit either value to source control.

## Optional Local Database Update

Run this only when `PROPCLOUD_CONNECTION_STRING` is set to a safe local PostgreSQL connection string:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
```

This script does not print the connection string and does not use AWS.

## Readiness and Seed Verification

Run the backend:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

Verify readiness:

```text
GET http://localhost:5015/api/database/readiness
```

If the local database is configured and migrations are applied, seed demo data:

```text
POST http://localhost:5015/api/seed/demo-data
```

Run the seed endpoint a second time to confirm it skips duplicate records.

If no database connection is configured, the seed endpoint should return a safe `400 Bad Request` response instead of crashing.

## Credential Warning

Never commit real PostgreSQL passwords, private connection strings, AWS credentials, or production secrets. Keep `appsettings.json` with an empty `DefaultConnection` placeholder only.

## Cloud Note

Amazon RDS setup, secure cloud credentials, and production database configuration will be handled later in a cloud sprint.
