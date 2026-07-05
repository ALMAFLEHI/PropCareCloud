# Sprint 11 - Production UI/UX Polish & Workflow Enhancements

## Sprint Goal

Make PropCare Cloud feel like a clean, modern property maintenance SaaS portal while preserving the Sprint 9 role-based access rules and Sprint 10 admin access management functionality.

## Why This Sprint Was Needed

Earlier sprint screens included implementation-oriented wording such as sprint names, local environment labels, API validation language, and prototype-style workflow explanations. Sprint 11 shifts the user-facing interface toward business terminology and adds a request detail workflow that better supports tenant tracking, manager assignment, and staff work updates.

## UI Wording Cleanup

- Removed sprint labels from the topbar.
- Replaced the environment badge with `Secure Portal`.
- Cleaned login page copy and moved assessment credentials into a collapsible helper.
- Updated dashboards, requests, properties, access management, loading states, access-denied messaging, and system health wording.
- Hid raw localhost/service URL details from normal dashboard UI.

## Dashboard Polish

- Admin / Owner dashboard uses `Portfolio Operations Dashboard`.
- Property Manager dashboard uses `Property Operations Dashboard`.
- Tenant dashboard uses `My Home Service Portal`.
- Maintenance Staff dashboard uses `Assigned Work Dashboard`.
- Role-specific cards and suggested actions now match real operational workflows.
- System health is shown only in the admin dashboard area.

## Request Detail Page

- Added route: `/requests/:id`.
- Added page: `frontend/src/pages/RequestDetailPage.tsx`.
- Loads request detail with `GET /api/maintenance-requests/{id}`.
- Loads activity notes with `GET /api/maintenance-requests/{id}/comments`.
- Allows note posting with `POST /api/maintenance-requests/{id}/comments`.
- Keeps role-based visibility and action behavior controlled by backend RBAC.

## Status Timeline

- Added `frontend/src/components/StatusTimeline.tsx`.
- Displays Submitted, Under Review, Assigned, In Progress, and Completed.
- Shows Cancelled as a stopped state.
- Used on request detail and request list cards.

## Role-Specific Request Workflow

- Tenant page title: `My Maintenance Requests`.
- Tenants can create requests and view progress but cannot update status or assignment.
- Maintenance Staff page title: `My Assigned Work`.
- Staff can view assigned jobs, open detail pages, add work notes, and update only allowed progress states.
- Property Manager page title: `Maintenance Request Management`.
- Managers can assign staff, update status, and open detail pages.
- Admin page title: `Portfolio Request Oversight`.
- Admins can monitor, assign, update, and open detail pages across visible requests.

## Comments And Activity Notes

- Request detail page shows chronological activity notes.
- Tenants can add visible notes to their own visible requests.
- Maintenance Staff can add notes to assigned jobs.
- Admin / Owner and Property Manager can add notes and mark internal notes.
- Tenant users do not receive internal notes from the backend.

## Backend Changes

- Added `PropertyName` to `MaintenanceRequestResponse`.
- Projected property name from the rental unit property relationship.
- No database schema change was required.
- No AWS service, S3 upload, Cognito, or email invitation workflow was added.

## Frontend Changes

- Updated topbar branding.
- Rebuilt login copy and assessment credential helper.
- Reworked role dashboard wording, cards, and suggested actions.
- Added request detail route and page.
- Added status timeline component.
- Updated request cards with progress previews and detail links.
- Updated Properties and Access Management wording.
- Renamed API status card to a professional system health card.

## Validation Commands

```powershell
cd frontend
npm run build
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Validation Results

- Frontend build: PASS.
- Full-stack local validation: PASS.
- Backend validation: PASS.
- Frontend validation: PASS.

## Manual Validation Checklist

- Login page is clean and assessment credentials are collapsed.
- Topbar does not show a sprint name.
- Admin dashboard looks professional and Access Management still works.
- Manager can review requests, assign staff, and open request detail.
- Tenant Sara sees only Sara requests and can open request detail with timeline.
- Tenant Imran sees only Imran requests and can open request detail with timeline.
- Tenant users do not see status or assignment controls.
- Maintenance Staff sees assigned jobs only and can update allowed progress states.
- Non-admin users cannot access Access Management.

## Security Notes

- No real database passwords were added.
- No AWS keys were added.
- No private keys were added.
- No production JWT secrets were added.
- No `.env` files were committed.
- Demo/assessment credentials remain only for assignment validation.

## Evidence Screenshots Needed Later

- `docs/sprints/screenshots/sprint_11_clean_login_page.png`
- `docs/sprints/screenshots/sprint_11_tenant_request_detail_timeline.png`
- `docs/sprints/screenshots/sprint_11_manager_request_detail_assignment.png`
- `docs/sprints/screenshots/sprint_11_staff_work_detail_progress.png`
- `docs/sprints/screenshots/sprint_11_admin_polished_dashboard.png`

## Final Status

COMPLETE for code.

Sprint 11 production UI wording cleanup, request detail page, status timeline, role-specific workflow improvements, activity notes, backend response support, documentation, validation, and security checks are complete.
