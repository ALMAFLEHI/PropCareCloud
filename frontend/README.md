# PropCare Cloud Frontend

React + TypeScript + Vite frontend for the PropCare Cloud property maintenance and tenant service portal.

## Tech Stack

- React
- TypeScript
- Vite
- Tailwind CSS
- React Router
- Axios
- lucide-react icons

## Install

```powershell
cd frontend
npm install
```

## Run

```powershell
cd frontend
npm run dev
```

The local Vite app runs on:

```text
http://localhost:5173
```

## Build

```powershell
cd frontend
npm run build
```

## Backend Dependency

The frontend expects the ASP.NET Core backend to run locally at:

```text
http://localhost:5015
```

The app reads `VITE_API_BASE_URL` and falls back to `http://localhost:5015`.

## Current Pages and Routes

- `/` - Dashboard with live property/request counts, recent requests, API status, role cards, and architecture summary
- `/requests` - Maintenance request list, create form using seeded IDs, and status update dropdown
- `/properties` - Property list, selected property unit list, and create property form
- `/users` - Planned users and roles with authentication deferred

## Sprint 8 API Integration

The frontend currently calls:

- `GET /api/properties`
- `POST /api/properties`
- `GET /api/properties/{propertyId}/units`
- `GET /api/maintenance-requests`
- `POST /api/maintenance-requests`
- `PATCH /api/maintenance-requests/{id}/status`
- `GET /api/health`
- `GET /api/system-info`

## Validation

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

## Notes

Authentication, authorization enforcement, AWS deployment, Amazon RDS connectivity, production file storage, and complete business workflows will be added in later sprints. No secrets or production credentials are configured in the frontend.
