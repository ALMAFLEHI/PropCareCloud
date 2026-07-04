# Sprint 10 - Admin User & Access Management

## Sprint Goal

Replace the documentation-style Users / Roles page with a real Admin User & Access Management module backed by admin-only API endpoints.

## Date

2026-07-04 23:47 +03:00

## Why This Sprint Was Needed

Sprint 9 closed authentication, role dashboards, backend RBAC, tenant isolation, and tenant-unit consistency. The remaining admin page still behaved like a role explanation screen instead of a SaaS administration console. Sprint 10 turns that route into a protected operational screen for accounts and tenant-unit assignment management.

## Features Implemented

- Admin / Owner can list user accounts.
- Admin / Owner can filter accounts by role and active/disabled status.
- Admin / Owner can create internal Property Manager accounts.
- Admin / Owner can create internal Maintenance Staff accounts.
- Admin / Owner can edit user profile names.
- Admin / Owner can disable and reactivate accounts.
- Disabled accounts cannot log in.
- Admin / Owner can reset user passwords.
- Admin / Owner can view active and historical tenant-unit assignments.
- Admin / Owner can assign tenants to available rental units.
- Admin / Owner can end active tenant-unit assignments without deleting history.
- The sidebar label changed from `Users / Roles` to `Access Management`.
- Non-admin roles remain blocked by frontend route behavior and backend `AdminOnly` policy.

## Backend Endpoints

Base route: `api/admin/users`

- `GET /api/admin/users`
- `GET /api/admin/users/{userProfileId}`
- `POST /api/admin/users/internal`
- `PUT /api/admin/users/{userProfileId}/profile`
- `PATCH /api/admin/users/{userProfileId}/status`
- `PATCH /api/admin/users/{userProfileId}/password`
- `GET /api/admin/users/tenant-assignments`
- `POST /api/admin/users/tenant-assignments`
- `PATCH /api/admin/users/tenant-assignments/{assignmentId}/end`
- `GET /api/admin/users/available-units`

## Backend Changes

- Added `DTOs/UserManagement` request/response models.
- Added `IUserManagementService` and `UserManagementService`.
- Added `UserManagementController` protected by `AdminOnly`.
- Registered the user management service when the database is configured.
- Updated login behavior so disabled accounts fail with `Account is disabled. Contact an administrator.`
- No database migration was required because `AuthUserAccount.IsActive`, `LastLoginAtUtc`, and tenant-unit assignment tables already existed.

## Frontend Admin Console Behavior

- `/users` remains the route, but the sidebar now shows `Access Management`.
- Non-admin users see the existing access denied screen if they reach the route directly.
- The page now contains:
  - Account summary cards.
  - Accounts list with role/status filtering.
  - Edit profile action.
  - Disable/reactivate action.
  - Reset password action.
  - Internal user creation form.
  - Tenant-unit assignment management.
  - Short operational access rules.
- Prototype/sprint explanation text was removed from the production UI.

## Role Permissions

- Admin / Owner: full account and tenant assignment management.
- Property Manager: no admin credential management.
- Tenant: no user management access.
- Maintenance Staff: no user management access.

## Tenant Assignment Rules

- The selected profile must have the Tenant role.
- The selected rental unit must exist.
- One rental unit cannot have two active tenant assignments.
- One tenant can have multiple active assigned units.
- Ending an assignment sets it inactive and keeps historical data.
- Ended assignments make the unit available for a future assignment.

## Tenant Invitation Foundation

Direct tenant account creation is intentionally not exposed through the internal-user endpoint. A future tenant onboarding flow should use invitations:

1. Admin or manager creates a tenant invitation for a selected unit.
2. Tenant opens the invitation link.
3. Tenant completes registration and personal details.
4. System links the tenant account to the assigned unit.
5. Tenant can create requests only for assigned units.

## Tests Added

- Admin can list users.
- Non-admin roles are blocked by the admin-only controller policy.
- Admin can create Property Manager account.
- Admin can create Maintenance Staff account.
- Admin cannot create Tenant through the internal-user endpoint.
- Duplicate email is rejected.
- Password is stored as a BCrypt hash, not plaintext.
- Disabled account cannot log in.
- Admin cannot disable own account.
- Admin cannot disable last active Admin / Owner account.
- Admin can reactivate a disabled account.
- Admin can reset password and the new password works.
- Tenant assignment rejects non-tenant profiles.
- Tenant assignment rejects already assigned units.
- Tenant can have multiple active assigned units.
- Ending an assignment makes the unit available again.
- Existing Sprint 9 RBAC and tenant isolation tests still pass.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Validation Result

- Direct backend test run using separate artifacts path: PASS, 65 tests passed
- Direct frontend build: PASS
- `check-fullstack-local`: PASS
- `check-backend`: PASS, restore/build/test completed successfully, 65 tests passed
- `check-frontend`: PASS, `npm install` and `npm run build` completed successfully

## Security Notes

- No real database passwords were added.
- No AWS keys were added.
- No production JWT secrets were added.
- No password hashes are exposed to frontend responses.
- New passwords are accepted only as request input and stored as BCrypt hashes.
- Demo credentials remain documented for assignment testing only.

## Manual Validation Checklist

Backend:

- Apply migrations if needed with `powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1`.
- Run backend at `http://localhost:5015`.
- Run `POST /api/auth/ensure-demo-accounts`.
- Login as admin.
- Call `GET /api/admin/users` with admin bearer token.
- Call `GET /api/admin/users` with tenant bearer token and confirm `403`.

Frontend:

- Run frontend at `http://localhost:5173`.
- Login as Admin / Owner.
- Confirm sidebar shows `Access Management`.
- Confirm the page loads real user accounts.
- Create Property Manager account.
- Create Maintenance Staff account.
- Confirm duplicate email is rejected.
- Disable an account and confirm login fails.
- Reactivate the account.
- Reset password and confirm the new password works.
- View tenant-unit assignments.
- End an assignment and confirm the unit becomes available.
- Confirm Manager, Tenant, and Staff cannot access the page.

## Final Evidence Captured

- `docs/sprints/screenshots/sprint_10_admin_access_management_overview.png`
- `docs/sprints/screenshots/sprint_10_create_internal_user_form.png`
- `docs/sprints/screenshots/sprint_10_user_disable_reactivate.png`
- `docs/sprints/screenshots/sprint_10_tenant_unit_assignment_management.png`
- `docs/sprints/screenshots/sprint_10_non_admin_access_denied.png`

## Final Status

COMPLETE.

Sprint 10 is fully closed. Admin User & Access Management, admin-only backend access, account creation, disable/reactivate, password reset, tenant-unit assignment management, and evidence screenshots are complete.
