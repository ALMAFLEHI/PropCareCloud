# Sprint 04 Database Domain Models

## Sprint Name

Sprint 4: Database Design & Backend Domain Models

## Sprint Goal

Create the backend database/domain foundation for PropCare Cloud by designing the core data model, adding EF Core domain entities, enums, DbContext configuration, database design documentation, and tests. This sprint prepares the project for future PostgreSQL/RDS integration without connecting to a real database yet.

## Date/Time

Backend validation completed on 2026-07-02 16:33:55 +03:00.

## Files Created/Changed

Created backend domain model files:

- `backend/src/PropCareCloud.Api/Domain/Entities/UserProfile.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/Property.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/RentalUnit.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/MaintenanceRequest.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/MaintenanceRequestComment.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/MaintenanceRequestAttachment.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/UserRole.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/PropertyStatus.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/UnitStatus.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/MaintenanceCategory.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/MaintenancePriority.cs`
- `backend/src/PropCareCloud.Api/Domain/Enums/MaintenanceStatus.cs`

Created or updated backend EF/API files:

- `backend/src/PropCareCloud.Api/Data/AppDbContext.cs`
- `backend/src/PropCareCloud.Api/Services/DomainSummaryService.cs`
- `backend/src/PropCareCloud.Api/Controllers/DomainSummaryController.cs`
- `backend/src/PropCareCloud.Api/Program.cs`
- `backend/src/PropCareCloud.Api/appsettings.json`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.http`

Created or updated tests and documentation:

- `backend/tests/PropCareCloud.Api.Tests/DomainEntityTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/AppDbContextModelTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/DomainSummaryServiceTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/PropCareCloud.Api.Tests.csproj`
- `backend/README.md`
- `README.md`
- `docs/architecture/database_design.md`
- `docs/sprints/sprint_04_database_domain_models.md`

## Domain Entities Created

- `UserProfile`
- `Property`
- `RentalUnit`
- `MaintenanceRequest`
- `MaintenanceRequestComment`
- `MaintenanceRequestAttachment`

## Enums Created

- `UserRole`
- `PropertyStatus`
- `UnitStatus`
- `MaintenanceCategory`
- `MaintenancePriority`
- `MaintenanceStatus`

## DbContext Summary

`AppDbContext` was added with DbSets for the six domain entities. Fluent API configuration defines table names, required fields, max lengths, relationships, enum-to-string conversion, indexes for user identity/email fields, and restrictive delete behavior to avoid future PostgreSQL cascade path issues.

The planned database provider is Amazon RDS PostgreSQL through `Npgsql.EntityFrameworkCore.PostgreSQL`. `Program.cs` only registers the DbContext when `DefaultConnection` is configured, so the local API still runs with no real database connection.

## New Endpoint Created

- `GET /api/domain-summary`

This endpoint returns the Sprint 4 domain summary, planned database provider, entity names, enum names, and a message confirming that this sprint defines the model only and does not connect to RDS yet.

## Tests Added

- Domain entity default/basic validity tests
- EF Core InMemory model save/read test
- Domain summary service test

Existing `ApplicationInfoService` tests remain in place.

## Commands/Checks Performed

Package setup:

```powershell
dotnet add backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj package Microsoft.EntityFrameworkCore --version 8.0.22
dotnet add backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add backend\tests\PropCareCloud.Api.Tests\PropCareCloud.Api.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory --version 8.0.22
```

Backend validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

Manual API run:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

Manual endpoint verification:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5015/api/domain-summary
Invoke-WebRequest -UseBasicParsing http://localhost:5015/swagger/v1/swagger.json
```

## Build/Test Results

- `dotnet restore`: PASS
- `dotnet build`: PASS
- `dotnet test`: PASS
- Build warnings: 0
- Build errors: 0
- Unit tests: 8 passed, 0 failed, 0 skipped
- Manual `GET /api/domain-summary`: PASS, HTTP 200
- Swagger endpoint listing for `/api/domain-summary`: PASS

## Issues Found

- NuGet package operations required access to the user NuGet configuration/cache and were run with approved access.
- The first validation attempt with approved NuGet access was blocked by a previously running local `PropCareCloud.Api` process locking the build output. The process was stopped, validation was rerun, and restore/build/test passed.
- A manual API run produced the existing HTTPS redirection warning because no HTTPS port is configured for this local profile. The HTTP endpoint still responded successfully at `http://localhost:5015/api/domain-summary`.

## What Was Intentionally Not Done Yet

- No RDS connection was configured.
- No migrations were created.
- No authentication was implemented.
- No frontend database screens were created.
- No AWS deployment was performed.
- No real connection strings or credentials were added.

## Evidence Screenshots Needed Later

- `sprint_04_domain_summary_swagger.png`

## Final Status

COMPLETE

Sprint 4 is complete because EF Core 8.0.x packages are added, domain entities and enums exist, `AppDbContext` exists, `/api/domain-summary` exists, database design documentation exists, Sprint 4 documentation exists, backend and root README files are updated, restore/build/test pass, and the backend endpoint was manually verified.
