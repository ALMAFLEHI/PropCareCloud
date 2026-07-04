# Sprint 8 - Frontend CRUD Integration

## Sprint Goal

Integrate the React frontend with the Sprint 7 backend CRUD APIs for properties and maintenance requests, keep the UI professional and cloud-assignment ready, validate the local full-stack build, and document the sprint result.

## Date

2026-07-04

## Files Created or Changed

- `frontend/src/api/propCareApi.ts`
- `frontend/src/types/api.ts`
- `frontend/src/utils/formatters.ts`
- `frontend/src/components/EmptyState.tsx`
- `frontend/src/components/LoadingState.tsx`
- `frontend/src/components/ErrorState.tsx`
- `frontend/src/components/StatusBadge.tsx`
- `frontend/src/components/Modal.tsx`
- `frontend/src/components/Topbar.tsx`
- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/pages/PropertiesPage.tsx`
- `frontend/src/pages/RequestsPage.tsx`
- `frontend/src/pages/UsersPage.tsx`
- `frontend/README.md`
- `README.md`
- `scripts/check-fullstack-local.ps1`
- `docs/sprints/sprint_08_frontend_crud_integration.md`

## Frontend API Integration

- `GET /api/properties`
- `POST /api/properties`
- `GET /api/properties/{propertyId}/units`
- `GET /api/maintenance-requests`
- `POST /api/maintenance-requests`
- `PATCH /api/maintenance-requests/{id}/status`
- `GET /api/health`
- `GET /api/system-info`

The frontend reads `VITE_API_BASE_URL` and falls back to `http://localhost:5015`.

## Components Added

- `EmptyState`
- `LoadingState`
- `ErrorState`
- `StatusBadge`
- `Modal`

## Pages Updated

- Dashboard now loads live properties and maintenance requests, shows live counts, recent requests, API status, role cards, and architecture summary.
- Properties now lists real backend property records, displays selected property units, and includes an add property modal form.
- Maintenance Requests now lists real backend request records, includes a status update dropdown, and includes an add request modal form using seeded tenant/unit IDs.
- Users/Roles now clearly notes that authentication and user management are deferred.

## Manual UI Validation

- Backend opened at `http://localhost:5015`.
- Frontend opened at `http://localhost:5173`.
- Dashboard route `/` loaded and the backend API health/system card showed connected.
- Live CRUD browser validation was blocked in this Codex process because no local `DefaultConnection` user-secret or environment variable was configured.
- Direct CRUD endpoint checks returned HTTP 500 because the backend only registers the CRUD services when `DefaultConnection` is configured.
- Properties route `/properties` and Maintenance Requests route `/requests` are ready for browser validation after local database configuration is supplied.
- Users route `/users` shows the planned role model and Sprint 8 scope note.
- Evidence screenshots are intentionally deferred until the sprint evidence capture step.

## Commands and Checks

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

This script runs:

- `scripts/check-frontend.ps1`
- `scripts/check-backend.ps1`

## Build Results

- Frontend install/build: PASS
- Backend restore/build/test: PASS
- Full-stack local validation: PASS
- Live CRUD UI browser check: BLOCKED by missing local `DefaultConnection` configuration in this Codex process

## Security Notes

- No real database password is stored in frontend files.
- No `.env` file with secrets is committed.
- No AWS credentials are configured.
- Real local database settings must continue to use user-secrets or environment variables.

## Intentionally Not Done Yet

- No authentication or authorization enforcement.
- No production user lookup screen.
- No Amazon RDS cloud connection.
- No AWS deployment.
- No S3 file upload implementation.
- No complete production maintenance workflow.

## Final Status

PARTIAL: frontend/backend build validation passed and CRUD integration code is complete, but final live CRUD browser validation still needs a configured local PostgreSQL connection string in user-secrets or an environment variable.
