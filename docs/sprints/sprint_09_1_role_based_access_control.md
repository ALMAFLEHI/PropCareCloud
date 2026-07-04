# Sprint 9.1 - Real Role-Based Access Control & Data Filtering

## Sprint Goal

Fix the Sprint 9 partial role implementation by enforcing role-based access control in the backend API and aligning frontend pages/actions with the same permission rules.

## Date

2026-07-04

## Problem Found After Sprint 9

Sprint 9 added login, JWT demo accounts, protected routes, and role-specific dashboard/sidebar content. It was still partial because the backend did not yet filter maintenance request data or block role-inappropriate actions. Tenants and maintenance staff could still call APIs that should have been restricted.

## Role Permission Matrix

| Role | Allowed | Blocked |
| --- | --- | --- |
| Admin / Owner | Full portfolio, properties, users/roles, all requests, status updates, staff assignment | Production-only features deferred |
| Property Manager | Properties/units, all requests, request triage, status updates, staff assignment | Admin-only users/roles page |
| Tenant | Own requests only, assigned units, create request for assigned active unit | Other tenants' requests, properties, users/roles, status updates, staff assignment |
| Maintenance Staff | Assigned requests only, update assigned work to InProgress or Completed | Unassigned/other staff requests, create requests, properties, users/roles, staff assignment |

## Backend Enforcement Summary

- Added `ICurrentUserService` / `CurrentUserService` to read authenticated user profile, email, and role claims.
- Added authorization policies: `AdminOnly`, `AdminOrManager`, `AdminManagerOrStaff`, `AllRoles`.
- Added `TenantUnitAssignment` entity and `AddTenantUnitAssignments` EF migration.
- Added `GET /api/user-profiles/maintenance-staff` for admin/manager assignment UI.
- Added `GET /api/user-profiles/tenants` for admin/manager request helper UI.
- Added `GET /api/user-profiles/me/assigned-units` for tenant request creation.
- Protected property/unit endpoints with `AdminOrManager`.
- Filtered `GET /api/maintenance-requests` by role.
- Enforced request view access by role for individual records.
- Enforced tenant request creation only for assigned active occupied units.
- Enforced staff status updates only for assigned requests and only to `InProgress` or `Completed`.
- Enforced staff assignment for Admin / Owner and Property Manager only.
- Comments now respect request visibility and tenant internal-comment restrictions.

## Frontend Role Behavior Summary

- Added reusable `AccessDenied` component.
- Added role helper functions for admin, manager, tenant, staff, request creation, assignment, property management, and status updates.
- Updated API client with user profile helper endpoints and staff assignment endpoint.
- Dashboard no longer calls property APIs for tenant/staff roles.
- Tenant dashboard shows active assigned units.
- Requests page now changes behavior by role:
  - Admin / Owner and Property Manager see all requests, full status options, and assignment controls.
  - Tenant sees only own requests, can create only from assigned unit dropdown, and has no status or assignment controls.
  - Maintenance Staff sees only assigned jobs, cannot create requests, and only sees In progress / Completed status options.

## Files Changed

- `backend/src/PropCareCloud.Api/Services/CurrentUserService.cs`
- `backend/src/PropCareCloud.Api/Services/MaintenanceRequestService.cs`
- `backend/src/PropCareCloud.Api/Services/AuthService.cs`
- `backend/src/PropCareCloud.Api/Services/SeedDataService.cs`
- `backend/src/PropCareCloud.Api/Controllers/MaintenanceRequestsController.cs`
- `backend/src/PropCareCloud.Api/Controllers/PropertiesController.cs`
- `backend/src/PropCareCloud.Api/Controllers/UserProfilesController.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/TenantUnitAssignment.cs`
- `backend/src/PropCareCloud.Api/Data/AppDbContext.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260704152545_AddTenantUnitAssignments.cs`
- `backend/tests/PropCareCloud.Api.Tests/AuthServiceTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/MaintenanceRequestServiceTests.cs`
- `backend/tests/PropCareCloud.Api.Tests/SeedDataServiceTests.cs`
- `frontend/src/api/propCareApi.ts`
- `frontend/src/types/api.ts`
- `frontend/src/utils/roles.ts`
- `frontend/src/components/AccessDenied.tsx`
- `frontend/src/components/ProtectedRoute.tsx`
- `frontend/src/pages/RoleDashboardPage.tsx`
- `frontend/src/pages/RequestsPage.tsx`
- `README.md`
- `backend/README.md`
- `frontend/README.md`

## Tests and Validation

Automated validation performed:

```powershell
dotnet build .\backend\PropCareCloud.sln
dotnet test .\backend\PropCareCloud.sln
npm run build
```

Final validation commands performed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

Backend tests cover:

- Tenant request filtering returns only tenant-owned requests.
- Maintenance staff filtering returns only assigned requests.
- Tenant cannot update request status.
- Maintenance staff can update assigned work to `InProgress`.
- Maintenance staff cannot update assigned work to disallowed statuses.
- Tenant can create requests only for assigned active units.
- Property controller requires the `AdminOrManager` policy.
- Demo tenant unit assignment is created without duplicates.

## Manual Validation Plan

1. Apply migration:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
```

2. Run backend:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

3. In Swagger, run `POST /api/auth/ensure-demo-accounts`.
4. Start frontend:

```powershell
cd frontend
npm run dev -- --port 5173
```

5. Validate roles:

- Admin / Owner: full navigation, all requests, properties, users/roles.
- Property Manager: dashboard, requests, properties, assignment/status controls, no users/roles page.
- Tenant: dashboard and requests only, own requests only, assigned-unit create form, no status dropdown.
- Maintenance Staff: dashboard and requests only, assigned jobs only, no create button, status options limited to In progress and Completed.

Unauthorized API checks:

- Tenant `PATCH /api/maintenance-requests/{id}/status` returns 403.
- Tenant `GET /api/properties` returns 403.
- Staff `POST /api/maintenance-requests` returns 403.
- Staff `PATCH /api/maintenance-requests/{id}/assign` returns 403.

## Security Notes

- No AWS services, Cognito, RDS cloud connection, or deployment were configured.
- No real database passwords, AWS access keys, production JWT secrets, or committed `.env` files are required.
- Demo credentials remain documented for assignment testing only.
- Demo passwords are stored as BCrypt hashes in the database.
- Local database credentials must remain in user-secrets or environment variables.

## Evidence Captured

- Sprint 9 role evidence screenshots were captured in the final Sprint 9 evidence closure:
  - `docs/sprints/screenshots/sprint_09_tenant_sara_restricted_view.png`
  - `docs/sprints/screenshots/sprint_09_tenant_imran_restricted_view.png`
  - `docs/sprints/screenshots/sprint_09_admin_full_access.png`
  - `docs/sprints/screenshots/sprint_09_manager_request_controls.png`
  - `docs/sprints/screenshots/sprint_09_staff_assigned_queue.png`

## Final Status

COMPLETE for Sprint 9.1 code and automated validation.

- `check-fullstack-local`: PASS
- `check-backend`: PASS, 41 backend tests passed
- `check-frontend`: PASS

Manual role screenshots were captured in the final Sprint 9 evidence closure.

## Sprint 9.2 Follow-Up

Sprint 9.2 further validates tenant-unit isolation and multiple tenant account behavior. It adds a second tenant demo account, hardens active rental-unit assignment uniqueness, and proves Sara/Imran request visibility remains isolated through the same generic tenant rules.
