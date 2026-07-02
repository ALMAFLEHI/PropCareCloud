# PropCare Cloud Frontend

React + TypeScript + Vite frontend foundation for the PropCare Cloud property maintenance and tenant service portal.

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

## Build

```powershell
cd frontend
npm run build
```

## Backend Dependency

The API status card expects the Sprint 2 backend to run locally at:

```text
http://localhost:5015
```

The frontend reads `VITE_API_BASE_URL` and falls back to `http://localhost:5015`.

## Current Pages and Routes

- `/` - Dashboard
- `/requests` - Maintenance request workflow placeholder
- `/properties` - Property and unit management placeholder
- `/users` - Users and roles placeholder

## Notes

Authentication, database-backed features, full maintenance request CRUD, and AWS deployment will be added in later sprints. No secrets or production credentials are configured in this frontend sprint.
