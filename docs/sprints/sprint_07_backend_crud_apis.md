# Sprint 07 Backend CRUD APIs

## Sprint Name

Sprint 7: Backend CRUD APIs for Properties and Maintenance Requests

## Sprint Goal

Build real backend CRUD API features for Task 1 system implementation. This sprint adds database-backed APIs for property/unit management and tenant maintenance request workflows using the existing EF Core PostgreSQL domain model.

## Date/Time

Sprint implementation and validation completed on 2026-07-03 at 23:01 +03:00.

## Files Created/Changed

Backend code:

- `backend/src/PropCareCloud.Api/DTOs/Properties/*`
- `backend/src/PropCareCloud.Api/DTOs/MaintenanceRequests/*`
- `backend/src/PropCareCloud.Api/Services/PropertyService.cs`
- `backend/src/PropCareCloud.Api/Services/MaintenanceRequestService.cs`
- `backend/src/PropCareCloud.Api/Controllers/PropertiesController.cs`
- `backend/src/PropCareCloud.Api/Controllers/MaintenanceRequestsController.cs`
- `backend/src/PropCareCloud.Api/Program.cs`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.http`

Tests:

- `backend/tests/PropCareCloud.Api.Tests/PropertyServiceTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/MaintenanceRequestServiceTests.cs`

Scripts and documentation:

- `scripts/check-crud-api.ps1`
- `README.md`
- `backend/README.md`
- `docs/architecture/database_design.md`
- `docs/sprints/sprint_07_backend_crud_apis.md`

## DTOs Created

Property DTOs:

- `PropertyCreateRequest`
- `PropertyUpdateRequest`
- `PropertyResponse`
- `RentalUnitCreateRequest`
- `RentalUnitUpdateRequest`
- `RentalUnitResponse`

Maintenance request DTOs:

- `MaintenanceRequestCreateRequest`
- `MaintenanceRequestUpdateRequest`
- `MaintenanceRequestResponse`
- `MaintenanceRequestAssignRequest`
- `MaintenanceRequestStatusUpdateRequest`
- `MaintenanceRequestCommentCreateRequest`
- `MaintenanceRequestCommentResponse`

## Services Created

- `PropertyService`
  - Property CRUD workflow.
  - Rental unit CRUD workflow.
  - Blocks property deletion when units exist.
  - Blocks unit deletion when maintenance requests exist.
- `MaintenanceRequestService`
  - Maintenance request list/create/update/delete workflow.
  - Tenant role validation for request creation.
  - Maintenance staff role validation for assignment.
  - Status updates and completion timestamp behavior.
  - Comment add/list workflow.

## Controllers and Endpoints Created

Properties:

- `GET /api/properties`
- `GET /api/properties/{id}`
- `POST /api/properties`
- `PUT /api/properties/{id}`
- `DELETE /api/properties/{id}`

Rental units:

- `GET /api/properties/{propertyId}/units`
- `GET /api/properties/{propertyId}/units/{unitId}`
- `POST /api/properties/{propertyId}/units`
- `PUT /api/properties/{propertyId}/units/{unitId}`
- `DELETE /api/properties/{propertyId}/units/{unitId}`

Maintenance requests:

- `GET /api/maintenance-requests`
- `GET /api/maintenance-requests/{id}`
- `POST /api/maintenance-requests`
- `PUT /api/maintenance-requests/{id}`
- `PATCH /api/maintenance-requests/{id}/assign`
- `PATCH /api/maintenance-requests/{id}/status`
- `DELETE /api/maintenance-requests/{id}`

Comments:

- `GET /api/maintenance-requests/{id}/comments`
- `POST /api/maintenance-requests/{id}/comments`

## Tests Added

- `PropertyServiceTests`
  - Gets seeded properties.
  - Creates properties.
  - Updates properties.
  - Blocks property deletion when units exist.
  - Creates rental units under properties.
  - Blocks unit deletion when maintenance requests exist.
- `MaintenanceRequestServiceTests`
  - Gets seeded maintenance requests.
  - Creates requests when tenant and rental unit exist.
  - Rejects request creation when the user is not a tenant.
  - Assigns only maintenance staff users.
  - Sets `CompletedAtUtc` when status becomes completed.
  - Adds comments.
  - Lists comments.

## Commands/Checks Performed

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-crud-api.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

Manual API check:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

Swagger URL:

```text
http://localhost:5015/swagger
```

## Build/Test Results

- `check-crud-api.ps1`: PASS
  - Required CRUD files: PASS
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 25 tests passed
- `check-backend.ps1`: PASS
  - `dotnet restore`: PASS
  - `dotnet build`: PASS, 0 warnings, 0 errors
  - `dotnet test`: PASS, 25 tests passed

## Local API Manual Testing Plan

- Confirm Swagger includes `Properties`.
- Confirm Swagger includes `MaintenanceRequests`.
- Confirm `GET /api/properties` returns HTTP 200 and seeded properties.
- Confirm `GET /api/maintenance-requests` returns HTTP 200 and seeded maintenance requests.
- Optionally create one test property using `POST /api/properties`.

## Manual API Validation

- Backend opened at `http://localhost:5015`.
- Swagger opened at `http://localhost:5015/swagger`.
- Swagger includes `/api/properties`.
- Swagger includes `/api/maintenance-requests`.
- `GET /api/properties` returned HTTP 200.
- `GET /api/properties` returned seeded property records.
- `GET /api/maintenance-requests` returned HTTP 200.
- `GET /api/maintenance-requests` returned seeded maintenance request records.
- No extra test property was created, keeping evidence/data minimal.
- Evidence screenshots saved:
  - `docs/sprints/screenshots/sprint_07_properties_api_swagger.png`
  - `docs/sprints/screenshots/sprint_07_maintenance_requests_api_swagger.png`

## Issues Found

- Initial manual `GET /api/maintenance-requests` exposed an EF Core PostgreSQL translation issue caused by ordering after projection.
- Fixed by applying `OrderByDescending` before projecting to `MaintenanceRequestResponse`.
- Validation and manual API checks passed after the fix.

## What Was Intentionally Not Done Yet

- No authentication was implemented.
- No authorization enforcement was implemented.
- No frontend CRUD integration was implemented.
- No Amazon RDS connection was configured.
- No AWS deployment was performed.

## Evidence Screenshots

- `docs/sprints/screenshots/sprint_07_properties_api_swagger.png`
- `docs/sprints/screenshots/sprint_07_maintenance_requests_api_swagger.png`

## Final Status

COMPLETE

Sprint 7 is COMPLETE because the CRUD DTOs, services, controllers, tests, validation script, Swagger endpoint groups, restore, build, test, and manual API checks all passed.
