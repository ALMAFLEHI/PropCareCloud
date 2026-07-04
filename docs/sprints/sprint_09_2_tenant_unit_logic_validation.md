# Sprint 9.2 - Tenant Unit Account Isolation & Multi-Tenant Logic Validation

## Sprint Goal

Validate and harden tenant account, tenant profile, rental unit assignment, and request isolation logic so PropCare Cloud behaves like a real-world multi-tenant property management SaaS.

## Date

2026-07-04

## Why This Sprint Was Needed

Sprint 9 added authentication and role dashboards. Sprint 9.1 added backend RBAC and role-based filtering. Sprint 9.2 verifies that tenant/unit/account logic is not only correct for one demo tenant, but works for any current or future tenant account.

Sara Tenant and Imran Tenant are demo users only. The isolation logic uses authenticated `userProfileId` values and tenant-unit assignments, so it applies to all future tenants instead of being hardcoded for Sara or Imran.

## Final Account Uniqueness Rules

- Every login account has a unique email.
- Every login account maps to exactly one `UserProfile`.
- `AuthUserAccount.UserProfileId` is unique.
- A `UserProfile` cannot have multiple active login accounts.
- Demo account passwords are stored as BCrypt hashes only.
- Plaintext demo passwords are allowed only in assignment documentation and the demo login panel.

## Final Tenant-Unit Assignment Rules

- `TenantUnitAssignment.TenantProfileId` is required.
- `TenantUnitAssignment.RentalUnitId` is required.
- `TenantUnitAssignment.IsActive` is required.
- One tenant can have multiple active assigned rental units.
- One rental unit can have only one active assignment at a time.
- Inactive historical assignments are allowed.
- PostgreSQL filtered unique index:
  - `RentalUnitId` is unique where `"IsActive" = TRUE AND "LeaseEndDateUtc" IS NULL`.

## Final Request Visibility Rules

- Admin / Owner can view all tenant requests.
- Property Manager can view all tenant requests.
- Tenant can view only requests where `TenantProfileId` matches the authenticated tenant profile.
- Tenant cannot see another tenant's request even in the same property.
- Maintenance Staff can view only requests where `AssignedStaffProfileId` matches the authenticated staff profile.

## Final Role Behavior Rules

- Tenant can create requests only for active assigned units.
- Tenant request creation ignores `tenantProfileId` from the request body and uses the JWT user profile.
- Tenant receives `403 Forbidden` when attempting to create for another tenant's unit.
- Tenant receives `400 Bad Request` when no active assigned units exist.
- Tenant cannot update maintenance status.
- Maintenance Staff can update only assigned jobs and only to `InProgress` or `Completed`.
- Admin / Owner and Property Manager retain portfolio-level management access.

## Sara vs Imran Validation Summary

- Sara Tenant login: `tenant@propcare.demo`
- Imran Tenant login: `imran@propcare.demo`
- Sara and Imran have separate `AuthUserAccount` rows.
- Sara and Imran have separate `UserProfile` IDs.
- Sara and Imran receive different active assigned units.
- Sara request filtering returns only Sara requests.
- Imran request filtering returns only Imran requests.
- Sara cannot create requests for Imran's unit.
- Imran cannot create requests for Sara's unit.

## Backend Changes

- Added Imran Tenant demo account to `AuthService`.
- Updated demo credential endpoint to return five accounts.
- Hardened tenant assignment selection to avoid already actively assigned units.
- Made `AuthUserAccount.UserProfileId` unique explicit in EF model configuration.
- Replaced tenant/unit/active unique index with filtered active rental-unit uniqueness.
- Added `HardenTenantUnitAssignmentIndexes` EF migration.
- Added tenant no-active-unit handling for request creation.
- Added multi-tenant isolation tests for Sara and Imran.

## Frontend Changes

- Updated login fallback credential cards to show five demo accounts.
- Login panel now identifies Sara and Imran tenant demo accounts separately.
- Existing tenant dashboard and requests page continue to use backend-filtered data and assigned-unit API responses.

## Tests Added or Updated

- Demo auth creates five hashed demo accounts.
- Sara and Imran are separate tenant profiles and accounts.
- Sara and Imran receive different active units.
- Demo account setup is idempotent.
- Account email and account user profile indexes are unique.
- Active rental unit assignment index is filtered and unique.
- One tenant can have multiple active units.
- Sara and Imran request lists are isolated.
- Sara cannot view Imran's request.
- Imran cannot view Sara's request.
- Sara cannot create for Imran's unit.
- Imran cannot create for Sara's unit.
- Tenant can create for own active assigned unit.
- Admin/Manager and Staff Sprint 9.1 RBAC tests still pass.

## Validation

Validation commands performed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Manual Validation Checklist

Backend:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

Swagger:

- `POST /api/auth/ensure-demo-accounts`
- `GET /api/auth/demo-credentials`

Frontend:

```powershell
cd frontend
npm run dev -- --port 5173
```

Manual role checks:

- Sara Tenant logs in with `tenant@propcare.demo` / `PropCare@Tenant123`.
- Sara sees only Dashboard and Maintenance Requests.
- Sara sees only Sara requests and Sara assigned units.
- Sara cannot update status.
- Imran Tenant logs in with `imran@propcare.demo` / `PropCare@Imran123`.
- Imran sees only Dashboard and Maintenance Requests.
- Imran sees only Imran requests and Imran assigned units.
- Imran cannot update status.
- Admin sees all tenant requests.
- Property Manager sees all tenant requests and assignment/status controls.
- Maintenance Staff sees only assigned jobs.

## Evidence Captured

- Sprint 9 tenant isolation evidence screenshots were captured in the final Sprint 9 evidence closure:
  - `docs/sprints/screenshots/sprint_09_tenant_sara_restricted_view.png`
  - `docs/sprints/screenshots/sprint_09_tenant_imran_restricted_view.png`
  - `docs/sprints/screenshots/sprint_09_admin_full_access.png`

## Sprint 9.3 Follow-Up

Sprint 9.3 corrected the remaining manual validation mismatch between tenant assigned units and seeded request units.

- Sara Tenant active units: `B-1102` and `A-0101`.
- Imran Tenant active units: `A-0205` and `B-1208`.
- Sara and Imran seeded request unit IDs are now subsets of their own active assigned unit IDs.
- The existing Sprint 9.2 schema and filtered unique active-unit assignment index were sufficient, so no migration was needed.

## Security Notes

- No AWS services, Cognito, production invitations, RDS cloud connection, or deployment were configured.
- No real database passwords, AWS access keys, production JWT secrets, or committed `.env` files are required.
- Demo credentials remain documented for assignment testing only.
- Demo passwords are stored as BCrypt hashes in database records.
- Local database credentials must remain in user-secrets or environment variables.

## Final Status

COMPLETE for Sprint 9.2 code and automated validation.

- `check-fullstack-local`: PASS
- `check-backend`: PASS, 47 backend tests passed
- `check-frontend`: PASS

Manual evidence screenshots were captured in the final Sprint 9 evidence closure.
