# Sprint 14 - Tenant Registration & Approval Workflow

## Sprint Date

2026-07-06 11:45 +03:00

## Sprint Goal

Add a public Tenant Registration & Approval Workflow before finalizing Task 1. The workflow lets public visitors request tenant portal access, keeps new requests pending for review, and lets Admin / Owner or Property Manager users approve or reject requests.

## Scope

- Public tenant registration request page.
- Persistent tenant registration request table.
- Public request submission API.
- Admin / Owner and Property Manager review APIs.
- Admin / Owner and Property Manager frontend review page.
- Approval workflow that creates or activates a tenant portal account and assigns an available rental unit.
- Rejection workflow that marks the request rejected without creating an account or assignment.
- Existing authentication, RBAC, tenant isolation, demo credentials, and seed repair behavior remain intact.

Sprint 14 stays inside the Task 1 server-based architecture:

```text
S3-hosted React frontend -> Elastic Beanstalk ASP.NET Core Web API -> Amazon RDS PostgreSQL
```

Sprint 14 does not add API Gateway, Lambda, S3 maintenance attachments, SNS, SQS, CloudWatch, or X-Ray. Those services are reserved for Task 2.

## User Stories

- As a public tenant, I can request portal access without logging in.
- As a public tenant, I see a clear pending approval message after submission.
- As an Admin / Owner, I can review pending tenant registration requests.
- As a Property Manager, I can review pending tenant registration requests.
- As an Admin / Owner or Property Manager, I can approve a request, set a temporary password, and assign an available unit.
- As an Admin / Owner or Property Manager, I can reject a request with a review note.
- As an approved tenant, I can log in after approval and remain limited to my assigned tenant/unit scope.

## Backend Changes

- Added domain entity: `TenantRegistrationRequest`.
- Added enum: `TenantRegistrationStatus`.
- Added DTOs for submit, approve, reject, and registration response payloads.
- Added `TenantRegistrationsController`.
- Added `TenantRegistrationService`.
- Registered `ITenantRegistrationService` in the API dependency injection setup.
- Added Admin / Owner and Property Manager review authorization through the existing `AdminOrManager` policy.

## Backend API Endpoints

- `POST /api/tenant-registrations` - public tenant registration submission.
- `GET /api/tenant-registrations` - protected Admin / Owner and Property Manager list endpoint.
- `GET /api/tenant-registrations/{id}` - protected detail endpoint.
- `GET /api/tenant-registrations/available-units` - protected available unit endpoint.
- `POST /api/tenant-registrations/{id}/approve` - protected approval endpoint.
- `POST /api/tenant-registrations/{id}/reject` - protected rejection endpoint.

## Frontend Changes

- Added public route: `/register`.
- Added protected route: `/tenant-registrations`.
- Added welcome page entry point: `Request tenant access`.
- Added login page entry point: `Request tenant access`.
- Added role-aware sidebar item: `Tenant Registrations`.
- Added public registration form with required first name, last name, and email fields.
- Added admin/manager review screen with summary cards, status filters, registration records, approve modal, reject modal, and available-unit selection.

## Sprint 14 Readiness Fix

Manual live evidence testing found that the approval modal had no available rental units because all original demo units had active tenant assignments.

Fixes added:

- Added idempotent approval-ready demo unit: `Cloud Residence - Unit A-0303`.
- Added idempotent approval-ready demo unit: `Harbor Heights - Unit B-1401`.
- Both new units use the existing `UnitStatus.Available` enum value.
- Neither unit receives an active tenant assignment during seeding.
- `POST /api/seed/demo-data` now creates or repairs these units and remains safe to rerun.
- `GET /api/tenant-registrations/available-units` now returns only units with `Available` status and no active tenant assignment.
- Approval now rejects rental units that are not `Available`.
- Login page `Request tenant access` was moved into a modern `New tenant?` secondary CTA block.
- Welcome page `Request tenant access` was moved out of the crowded main hero CTA row into a separate premium tenant access panel.
- Final login UX polish maps invalid credentials to a user-friendly message instead of a technical 401 error.
- No Task 2 services were added. Sprint 14 remains Task 1 frontend/backend/RDS functionality.

## Database Migration

Migration added:

```text
AddTenantRegistrationRequests
```

Generated files:

- `backend/src/PropCareCloud.Api/Data/Migrations/20260706084417_AddTenantRegistrationRequests.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/20260706084417_AddTenantRegistrationRequests.Designer.cs`
- `backend/src/PropCareCloud.Api/Data/Migrations/AppDbContextModelSnapshot.cs`

## RBAC And Security Rules

- Public visitors can only submit registration requests.
- Public submission does not create login accounts.
- Duplicate active pending requests for the same email are rejected.
- Emails that already have an active portal account are rejected at submission.
- Admin / Owner and Property Manager can list, approve, and reject tenant registrations.
- Tenant and Maintenance Staff accounts cannot list, approve, or reject tenant registrations.
- Approval requires an available rental unit and a temporary password.
- Temporary passwords are hashed with BCrypt and are not returned by the API.
- Rejection does not create an auth account or tenant-unit assignment.
- Existing tenant isolation remains enforced by the current authenticated user and tenant-unit assignment model.
- No AWS keys, database passwords, full connection strings, `.env` files, private keys, or production JWT secrets are committed.

## Tests Added Or Updated

- Public registration creates a pending request.
- Duplicate pending registration for the same email is rejected.
- Missing required fields or invalid email are rejected.
- Existing active account email is rejected.
- Tenant and Maintenance Staff roles cannot use review operations.
- Admin / Owner can list pending registrations.
- Property Manager can list pending registrations.
- Admin / Owner can approve a pending registration.
- Property Manager can approve a pending registration.
- Approval creates a tenant user profile and auth account.
- Approval creates an active tenant-unit assignment.
- Approval prevents assignment to an occupied or actively assigned unit.
- Approval prevents assignment to a unit that is not `Available`.
- Seed data creates approval-ready available units and reruns without duplicating them.
- Available-units service returns seeded available units and excludes assigned or unavailable units.
- Rejection marks the request rejected and does not create an account or assignment.
- Controller authorization metadata uses public submit and protected review endpoints.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
```

## Deployment Package Rebuild Commands

Backend package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-backend-package.ps1
```

Frontend package:

```powershell
$env:VITE_API_BASE_URL="http://propcarecloud-api.us-east-1.elasticbeanstalk.com"
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-frontend-package.ps1
```

The package scripts create local deployment artifacts only. They do not upload or deploy to AWS.

## Evidence Screenshots Needed Later

- `docs/sprints/screenshots/sprint_14_public_registration_page.png`
- `docs/sprints/screenshots/sprint_14_registration_submitted_pending.png`
- `docs/sprints/screenshots/sprint_14_admin_pending_registration_review.png`
- `docs/sprints/screenshots/sprint_14_manager_pending_registration_review.png`
- `docs/sprints/screenshots/sprint_14_approve_registration_modal.png`
- `docs/sprints/screenshots/sprint_14_available_units_for_approval.png`
- `docs/sprints/screenshots/sprint_14_registration_approved_status.png`
- `docs/sprints/screenshots/sprint_14_approved_tenant_login_dashboard.png`
- `docs/sprints/screenshots/sprint_14_approved_tenant_assigned_unit.png`
- `docs/sprints/screenshots/sprint_14_rejected_registration_status.png`
- `docs/sprints/screenshots/sprint_14_login_request_access_cta_fixed.png`
- `docs/sprints/screenshots/sprint_14_welcome_request_access_cta_fixed.png`

## Issues Found

- No final Sprint 14 fix evidence screenshots were committed during this code commit.
- AWS redeployment was intentionally not performed during this sprint code commit.

## Intentionally Not Done

- No API Gateway implementation.
- No Lambda implementation.
- No S3 maintenance attachment implementation.
- No SNS/SQS notification implementation.
- No CloudWatch/X-Ray monitoring implementation.
- No automatic AWS deployment.
- No real secrets, passwords, connection strings, private keys, `.env` files, or AWS keys committed.

## Final Status

CODE COMPLETE / READY FOR LIVE REDEPLOYMENT.

Sprint 14 tenant registration and approval workflow is implemented locally. The available-unit seed repair and tenant access CTA polish are ready for live redeployment. Manual deployment to AWS, seed repair on RDS, live approval validation, and Sprint 14 evidence screenshots are the next manual closure steps.
