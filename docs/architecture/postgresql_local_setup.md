# PostgreSQL Local Setup

## Purpose

This document explains the safe local PostgreSQL setup direction for PropCare Cloud. Sprint 5 creates EF Core migrations and helper scripts, but it does not require a real PostgreSQL database connection for normal restore, build, or test validation.

## Recommended Local Database Name

`propcarecloud_db`

## PostgreSQL Installation

PostgreSQL and `psql` may be installed later if direct local database testing is needed. The current project can still build and test without PostgreSQL installed.

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

## Optional Local Database Update

Run this only when `PROPCLOUD_CONNECTION_STRING` is set to a safe local PostgreSQL connection string:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
```

This script does not print the connection string and does not use AWS.

## Cloud Note

Amazon RDS setup, secure cloud credentials, and production database configuration will be handled later in a cloud sprint.
