# Sprint 03 Frontend Setup

## Sprint Name

Sprint 3: Frontend Setup & UI Foundation

## Sprint Goal

Create the React + TypeScript + Vite frontend foundation for PropCare Cloud, add Tailwind CSS styling, create a professional dashboard UI foundation, connect the frontend to the Sprint 2 backend endpoints, validate build success, and document the sprint result.

## Date/Time

Frontend validation completed on 2026-07-02 15:26:19 +03:00.

## Files Created/Changed

Created or updated frontend foundation:

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/index.html`
- `frontend/vite.config.ts`
- `frontend/.env.example`
- `frontend/README.md`
- `frontend/src/main.tsx`
- `frontend/src/App.tsx`
- `frontend/src/styles/index.css`
- `frontend/src/api/propCareApi.ts`
- `frontend/src/types/api.ts`
- `frontend/src/components/AppLayout.tsx`
- `frontend/src/components/Sidebar.tsx`
- `frontend/src/components/Topbar.tsx`
- `frontend/src/components/StatCard.tsx`
- `frontend/src/components/ApiStatusCard.tsx`
- `frontend/src/components/RoleCard.tsx`
- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/pages/RequestsPage.tsx`
- `frontend/src/pages/PropertiesPage.tsx`
- `frontend/src/pages/UsersPage.tsx`
- `frontend/src/assets/.gitkeep`

Created or updated project documentation and scripts:

- `README.md`
- `scripts/check-frontend.ps1`
- `docs/sprints/sprint_03_frontend_setup.md`

Removed default Vite demo content/assets:

- `frontend/src/App.css`
- `frontend/src/index.css`
- `frontend/src/assets/react.svg`
- `frontend/src/assets/vite.svg`
- `frontend/src/assets/hero.png`
- `frontend/public/icons.svg`
- `frontend/public/favicon.svg`

## Frontend Structure

```text
frontend/
|-- README.md
|-- package.json
|-- index.html
|-- vite.config.ts
|-- .env.example
`-- src/
    |-- main.tsx
    |-- App.tsx
    |-- api/
    |   `-- propCareApi.ts
    |-- components/
    |   |-- AppLayout.tsx
    |   |-- Sidebar.tsx
    |   |-- Topbar.tsx
    |   |-- StatCard.tsx
    |   |-- ApiStatusCard.tsx
    |   `-- RoleCard.tsx
    |-- pages/
    |   |-- DashboardPage.tsx
    |   |-- RequestsPage.tsx
    |   |-- PropertiesPage.tsx
    |   `-- UsersPage.tsx
    |-- types/
    |   `-- api.ts
    |-- styles/
    |   `-- index.css
    `-- assets/
```

## Pages/Routes Created

- `/` - Dashboard page with overview, placeholder statistics, backend API status, role cards, architecture summary, and future AWS service summary.
- `/requests` - Placeholder for future maintenance request creation, assignment, progress update, and completion tracking.
- `/properties` - Placeholder for future property and unit management.
- `/users` - Placeholder for future role-based access and user management.

## Components Created

- `AppLayout` - Main application shell with sidebar, topbar, and content area.
- `Sidebar` - Navigation for Dashboard, Maintenance Requests, Properties, and Users / Roles.
- `Topbar` - Project title, sprint label, and local development status.
- `StatCard` - Reusable dashboard statistic card.
- `ApiStatusCard` - Calls backend health and system info endpoints and shows loading, connected, or error state.
- `RoleCard` - Reusable role card for Admin / Owner, Property Manager, Tenant, and Maintenance Staff.

## Backend Integration Summary

The frontend API client uses Axios and reads:

```text
VITE_API_BASE_URL
```

If the environment variable is not set, it falls back to:

```text
http://localhost:5015
```

Integrated Sprint 2 endpoints:

- `GET /api/health`
- `GET /api/system-info`

The API status card displays a connected state if both requests succeed. If the backend is not running, it displays a clear error message: `Start the backend using dotnet run if this check fails.`

## Commands/Checks Performed

Initial Git state:

```powershell
git status --short
```

Frontend scaffold and dependencies:

```powershell
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
npm install tailwindcss @tailwindcss/vite
npm install axios react-router-dom lucide-react
```

Final frontend validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

Validation script commands:

```powershell
npm install
npm run build
```

## Build Results

Final validation results:

- `npm install`: PASS
- `npm run build`: PASS
- Build output: `dist/`
- Vite build: PASS
- npm audit: 0 vulnerabilities

Production build output:

- `dist/index.html`
- `dist/assets/index-70lkPM9I.css`
- `dist/assets/index-RDrBX6UP.js`

## Issues Found

- Initial npm scaffold/install commands were blocked by sandbox access to the user npm cache. The commands were rerun with approved npm cache access and completed successfully.
- The generated Vite demo content and assets were removed to keep the UI PropCare-specific.
- The validation script uses `npm.cmd` on Windows to avoid PowerShell npm shim issues.

## What Was Intentionally Not Done Yet

- No authentication was implemented.
- No real database data was displayed.
- No AWS deployment was performed.
- No full maintenance request CRUD was implemented.
- No real `.env` file or secrets were created.

## Manual UI Validation

- Frontend opened at `http://localhost:5173`.
- Backend opened at `http://localhost:5015`.
- Dashboard loaded successfully.
- Sidebar navigation tested successfully.
- Routes tested: `/`, `/requests`, `/properties`, `/users`.
- Backend API status card showed Connected.
- Important screenshots saved:
  - `docs/sprints/screenshots/sprint_03_frontend_dashboard.png`
  - `docs/sprints/screenshots/sprint_03_frontend_api_connected.png`

## Final Status

COMPLETE

Sprint 3 is complete because the React + TypeScript + Vite app exists under `frontend/`, Tailwind CSS is configured, the dashboard layout and routes exist, the backend API status card is implemented, frontend documentation exists, the validation script exists, `npm install` passes, `npm run build` passes, and the sprint is ready to be committed.
