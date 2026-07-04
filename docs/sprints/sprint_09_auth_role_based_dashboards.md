# Sprint 9 - Authentication, Role-Based Dashboards & Demo Credentials

## Sprint Goal

Add demo authentication and role-based user experience for PropCare Cloud so the application supports assignment-ready login credentials, protected frontend routes, and role-specific dashboard behavior.

## Date

2026-07-04

## Files Created or Changed

- `backend/src/PropCareCloud.Api/Domain/Entities/AuthUserAccount.cs`
- `backend/src/PropCareCloud.Api/Domain/Entities/UserProfile.cs`
- `backend/src/PropCareCloud.Api/Data/AppDbContext.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260704143452_AddAuthUserAccounts.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260704143452_AddAuthUserAccounts.Designer.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/PropCareCloud.Api/DTOs/Auth/LoginRequest.cs`
- `backend/src/PropCareCloud.Api/DTOs/Auth/AuthUserResponse.cs`
- `backend/src/PropCareCloud.Api/DTOs/Auth/LoginResponse.cs`
- `backend/src/PropCareCloud.Api/DTOs/Auth/DemoCredentialResponse.cs`
- `backend/src/PropCareCloud.Api/Services/AuthService.cs`
- `backend/src/PropCareCloud.Api/Controllers/AuthController.cs`
- `backend/src/PropCareCloud.Api/Program.cs`
- `backend/src/PropCareCloud.Api/appsettings.json`
- `backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj`
- `backend/tests/PropCareCloud.Api.Tests/AuthServiceTests.cs`
- `frontend/src/types/api.ts`
- `frontend/src/api/propCareApi.ts`
- `frontend/src/utils/authStorage.ts`
- `frontend/src/utils/roles.ts`
- `frontend/src/context/AuthContext.tsx`
- `frontend/src/components/ProtectedRoute.tsx`
- `frontend/src/components/Sidebar.tsx`
- `frontend/src/components/Topbar.tsx`
- `frontend/src/pages/LoginPage.tsx`
- `frontend/src/pages/RoleDashboardPage.tsx`
- `frontend/src/pages/RequestsPage.tsx`
- `frontend/src/pages/UsersPage.tsx`
- `README.md`
- `backend/README.md`
- `frontend/README.md`

## Backend Auth Summary

- Added `AuthUserAccount` linked one-to-one with `UserProfile`.
- Added unique email index for authentication accounts.
- Added BCrypt password hashing through `BCrypt.Net-Next`.
- Added JWT bearer authentication using .NET 8 package `Microsoft.AspNetCore.Authentication.JwtBearer` version `8.0.22`.
- Added local development JWT signing fallback only for assignment/demo use.
- Added Swagger bearer token support.
- Added auth endpoints:
  - `POST /api/auth/login`
  - `GET /api/auth/me`
  - `GET /api/auth/demo-credentials`
  - `POST /api/auth/ensure-demo-accounts`

## Frontend Auth Summary

- Added `/login` page.
- Added auth context and local token/user storage.
- Added Axios bearer token attachment.
- Added protected routes.
- Added role-based sidebar navigation.
- Added topbar user/role display and logout button.
- Added role-specific dashboard content for Admin / Owner, Property Manager, Tenant, and Maintenance Staff.

## Demo Credentials

| Role | Email | Password |
| --- | --- | --- |
| Admin / Owner | `admin@propcare.demo` | `PropCare@Admin123` |
| Property Manager | `manager@propcare.demo` | `PropCare@Manager123` |
| Tenant | `tenant@propcare.demo` | `PropCare@Tenant123` |
| Maintenance Staff | `staff@propcare.demo` | `PropCare@Staff123` |

These are demo assignment accounts only. Backend storage uses BCrypt password hashes and does not store plaintext passwords.

## Role-Based Dashboard Behavior

- Admin / Owner: portfolio overview, property and request monitoring, users/roles access.
- Property Manager: request triage, property access, maintenance workflow focus.
- Tenant: request submission and tracking view.
- Maintenance Staff: assigned work queue and status update focus.

The initial Sprint 9 UI was role-specific, but strict backend authorization and request filtering were completed in Sprint 9.1.

## Sprint 9.1 Completion Update

Sprint 9.1 fixes the Sprint 9 partial status by adding real backend role-based access control and data filtering.

- Tenant request lists now return only the signed-in tenant's own requests.
- Maintenance staff request lists now return only jobs assigned to the signed-in staff profile.
- Admin / Owner and Property Manager can view all demo maintenance requests.
- Tenants can create maintenance requests only for active assigned units.
- Tenants cannot update request status or access property management endpoints.
- Maintenance staff cannot create tenant requests or assign jobs.
- Maintenance staff can update only assigned jobs to `InProgress` or `Completed`.
- Property and rental unit management endpoints require Admin / Owner or Property Manager.
- Frontend dashboard and request page controls now match the backend role rules.

See `docs/sprints/sprint_09_1_role_based_access_control.md` for the detailed Sprint 9.1 closure record.

## Commands and Checks Performed

```powershell
dotnet add backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.22
dotnet add backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj package BCrypt.Net-Next
dotnet tool run dotnet-ef migrations add AddAuthUserAccounts --project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj --startup-project backend/src/PropCareCloud.Api/PropCareCloud.Api.csproj --output-dir Data/Migrations
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

## Build and Test Results

- Backend restore/build/test: PASS
- Backend tests: 31 passed
- Frontend install/build: PASS
- Full-stack local validation: PASS
- Manual login validation: BLOCKED because this Codex process does not have the local PostgreSQL password/connection string configured

## Manual UI Testing Plan

- Start backend at `http://localhost:5015`.
- Open Swagger at `http://localhost:5015/swagger`.
- Run `POST /api/auth/ensure-demo-accounts`.
- Run `POST /api/auth/login` for all four demo accounts.
- Start frontend at `http://localhost:5173`.
- Open `http://localhost:5173/login`.
- Confirm demo credential cards appear.
- Confirm clicking a demo credential fills the login form.
- Confirm Admin, Tenant, and Maintenance Staff login works.
- Confirm dashboard and sidebar change by role.
- Confirm logout works.
- Confirm protected pages redirect to `/login` when logged out.

## Issues Found

- Local live auth validation requires a configured local PostgreSQL `DefaultConnection`.
- A safe no-password local PostgreSQL attempt was rejected because the database requires a password.
- No production identity provider is configured in this sprint.

## Security Notes

- Demo passwords are assignment credentials and may appear in documentation/UI.
- Database storage uses BCrypt hashes, not plaintext passwords.
- `Jwt:SigningKey` is empty in committed configuration.
- No real database passwords, AWS access keys, or production secrets are committed.
- Real local database credentials must continue to use user-secrets or environment variables.

## Intentionally Not Done Yet

- No AWS Cognito.
- No production password reset.
- No email invitation flow.
- No AWS RDS cloud connection.
- No cloud deployment.
- No production-grade authorization policy matrix.

## Evidence Screenshots Needed Later

- `docs/sprints/screenshots/sprint_09_login_demo_credentials.png`
- `docs/sprints/screenshots/sprint_09_admin_dashboard.png`
- `docs/sprints/screenshots/sprint_09_tenant_dashboard.png`
- `docs/sprints/screenshots/sprint_09_staff_dashboard.png`

## Final Status

Sprint 9 initial authentication and role dashboard work was PARTIAL until Sprint 9.1. Sprint 9.1 is COMPLETE for backend RBAC/data filtering code and automated validation; final Sprint 9 evidence closure still requires manual role screenshots.
