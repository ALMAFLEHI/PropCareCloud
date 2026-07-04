# Sprint 9.3 - Tenant Request and Unit Consistency Fix

## Sprint Goal

Fix the manual validation mismatch where demo tenant assigned units did not fully match the units used by the seeded maintenance requests.

## Date

2026-07-04 22:40 +03:00

## Problem Found

Manual Sprint 9 validation showed that tenant isolation was working, but the demo unit assignments did not fully match seeded request data.

- Sara Tenant had requests for `B-1102` and `A-0101`, but the demo auth setup could assign only `B-1102`.
- Imran Tenant had requests for `A-0205` and `B-1208`, but the demo auth setup could assign only `A-0205`.
- This created a confusing UI result because the tenant request list and tenant assigned-unit dropdown did not describe the same tenant/unit relationship.

## Final Demo Assignments

Sara Tenant:

- `B-1102`
- `A-0101`

Imran Tenant:

- `A-0205`
- `B-1208`

## Final Request Consistency

Sara Tenant requests:

- `Lobby access card issue` for `B-1102`
- `Kitchen sink leaking` for `A-0101`

Imran Tenant requests:

- `Bathroom light flickering` for `B-1208`
- `Air conditioner not cooling` for `A-0205`

## Backend Changes

- Updated demo account setup so each tenant can require multiple active rental-unit assignments.
- Updated seed data tenant-unit assignments so Sara and Imran each receive two active units.
- Kept the existing Sprint 9.2 database model and filtered unique active-unit assignment rule.
- Did not create a new migration because the schema already supports multiple active units per tenant.
- Updated tenant request creation so an active tenant assignment is enough for request creation, including units currently marked under maintenance.
- Kept tenant request filtering based on the authenticated tenant profile.
- Kept the assignment helper generic for future tenants instead of relying on frontend-only or tenant-name-specific behavior.

## Frontend Changes

- No new frontend logic was required.
- The tenant dashboard and maintenance request form already use `GET /api/user-profiles/me/assigned-units`.
- The assigned-unit dropdown now receives the corrected Sara and Imran unit lists from the backend.

## Tests Added or Updated

- Sara has active assignment for `B-1102`.
- Sara has active assignment for `A-0101`.
- Imran has active assignment for `A-0205`.
- Imran has active assignment for `B-1208`.
- Sara active assignment count is `2`.
- Imran active assignment count is `2`.
- Sara request units are only Sara assigned units.
- Imran request units are only Imran assigned units.
- Demo account setup remains idempotent and does not create duplicate assignments.
- Seed data remains idempotent and does not create duplicate assignments.
- One rental unit cannot have two active tenant assignments.
- One tenant can have multiple active units.
- Sprint 9.1 RBAC tests still pass.
- Sprint 9.2 tenant isolation tests still pass.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Manual Validation Checklist

Backend:

- Start backend at `http://localhost:5015`.
- Open Swagger at `http://localhost:5015/swagger`.
- Run `POST /api/auth/ensure-demo-accounts`.
- Run `GET /api/auth/demo-credentials`.

Frontend:

- Start frontend at `http://localhost:5173`.
- Open `http://localhost:5173/login`.

Sara Tenant:

- Login with `tenant@propcare.demo`.
- Confirm assigned units count is `2`.
- Confirm assigned units show `B-1102` and `A-0101`.
- Confirm recent requests show `Lobby access card issue` and `Kitchen sink leaking`.
- Confirm no Imran requests are visible.
- Confirm no tenant status update dropdown is visible.

Imran Tenant:

- Login with `imran@propcare.demo`.
- Confirm assigned units count is `2`.
- Confirm assigned units show `A-0205` and `B-1208`.
- Confirm recent requests show `Bathroom light flickering` and `Air conditioner not cooling`.
- Confirm no Sara requests are visible.
- Confirm no tenant status update dropdown is visible.

Admin, Manager, and Staff:

- Admin can see full navigation and all Sara/Imran requests.
- Property Manager can see all requests, properties, assignment controls, and status controls.
- Maintenance Staff can see only assigned jobs and limited status options.

## Validation Result

- `dotnet test .\backend\PropCareCloud.sln`: PASS, 47 tests passed
- `check-fullstack-local`: PASS
- `check-backend`: PASS, restore/build/test completed successfully, 47 tests passed
- `check-frontend`: PASS, `npm install` and `npm run build` completed successfully

## Security Notes

- No real database passwords or connection strings were added.
- No AWS keys or production JWT secrets were added.
- Demo credentials remain assignment-only test accounts.
- Local secrets must continue to stay in user-secrets or environment variables.

## Evidence Screenshots Needed

- `docs/sprints/screenshots/sprint_09_3_sara_units_match_requests.png`
- `docs/sprints/screenshots/sprint_09_3_imran_units_match_requests.png`
- `docs/sprints/screenshots/sprint_09_3_admin_all_tenant_requests.png`
- `docs/sprints/screenshots/sprint_09_3_manager_request_controls.png`
- `docs/sprints/screenshots/sprint_09_3_staff_assigned_queue.png`

## Final Status

COMPLETE.
