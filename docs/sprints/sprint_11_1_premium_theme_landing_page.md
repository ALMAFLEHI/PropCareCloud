# Sprint 11.1 - Premium Visual Theme & Public Landing Page

## Sprint Goal

Add a general public landing page and a premium visual theme layer for PropCare Cloud while keeping all existing application behavior unchanged.

## Why This Sprint Was Needed

Sprint 11 made the product wording cleaner and added request workflow depth. Sprint 11.1 improves the first impression and visual polish so the portal feels more like a modern property maintenance SaaS product before and after login.

## Scope

This sprint was UI visual polish only.

- No backend changes.
- No API changes.
- No database schema changes.
- No authentication logic changes.
- No RBAC logic changes.
- No business workflow changes.
- No button behavior changes.

## Public Landing Page

- Added public route: `/welcome`.
- Added page: `frontend/src/pages/LandingPage.tsx`.
- The landing page is general and not role-specific.
- Main CTA routes to `/login`.
- Authenticated users also receive a dashboard shortcut.
- Feature cards cover tenant requests, manager assignment, staff progress tracking, and secure role-based access.

## Premium Theme Changes

- Added a light navy/teal premium background layer.
- Added reusable visual classes for premium panels, cards, hover lift, and small eyebrow labels.
- Added softer shadows, borders, backdrop surfaces, and hover states.
- Kept existing page layout structure and core card arrangements.

## Login Page Polish

- Login page now uses the premium background and hero card styling.
- Existing email, password, sign-in behavior, validation, and credential helper are unchanged.
- Assessment credentials remain inside a collapsible helper.

## App Visual Polish

- Dashboard hero panels and summary cards received subtle premium styling.
- Requests page cards and service request panels received visual depth.
- Request detail page uses premium header and card surfaces.
- Properties page uses the shared premium header/card surfaces.
- Access Management page uses premium header, tab, summary, and panel surfaces.
- Topbar and sidebar received a softer background, shadow, and hover treatment.

## Wording Cleanup

- Replaced the visible `This role...` request wording with account-friendly wording.
- Rechecked frontend page/component wording for sprint, prototype, local development, backend API, testing, seeded, future production, and `this role` phrases.
- The assessment credential helper remains intentionally available for assignment validation.

## What Was Intentionally Not Changed

- Backend controllers, services, DTOs, and tests were not edited.
- API calls and route protection logic were not changed.
- Database schema and migrations were not changed.
- Auth storage, login request behavior, and RBAC helpers were not changed.
- Request assignment, status update, note posting, and access management actions were not changed.

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
- Full-stack validation: PASS.
- Backend validation: PASS.
- Frontend validation: PASS.

## Manual Validation Checklist

- Open `/welcome` and confirm the landing page loads.
- Confirm the Sign in CTA opens `/login`.
- Confirm `/login` looks polished and existing sign-in still works.
- Confirm the assessment credential helper still fills the form.
- Log in as Admin and confirm dashboard, requests, properties, and access management still work.
- Log in as Property Manager and confirm assignment/status workflow still works.
- Log in as Tenant and confirm only own requests are visible.
- Log in as Maintenance Staff and confirm assigned work workflow still works.
- Confirm existing buttons behave as before.

## Security Notes

- No real database passwords were added.
- No AWS keys were added.
- No private keys were added.
- No production JWT secrets were added.
- No `.env` files were committed.
- No password hashes were exposed in the frontend.
- No real private user data was added.

## Evidence Screenshots Needed Later

- `docs/sprints/screenshots/sprint_11_1_public_landing_page.png`
- `docs/sprints/screenshots/sprint_11_1_premium_login_page.png`
- `docs/sprints/screenshots/sprint_11_1_admin_dashboard_visual_polish.png`
- `docs/sprints/screenshots/sprint_11_1_tenant_request_progress_wording.png`
- `docs/sprints/screenshots/sprint_11_1_request_detail_visual_polish.png`

## Final Status

COMPLETE for code.

Sprint 11.1 public landing page, premium visual theme, login polish, app visual polish, wording cleanup, documentation, validation, and security checks are complete.
