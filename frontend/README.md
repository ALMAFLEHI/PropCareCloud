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

- `/login` - Demo login page with role credentials
- `/` - Role-based dashboard with live property/request data
- `/requests` - Maintenance request list, create form using seeded IDs, and status update dropdown
- `/properties` - Property list, selected property unit list, and create property form for Admin / Owner and Property Manager
- `/users` - Admin-only users and roles page

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

## Sprint 9 Authentication

The frontend now includes:

- Auth context with JWT token storage in `localStorage`
- Protected routes
- Role-based dashboard copy and quick actions
- Role-based sidebar navigation
- Topbar user/role display and logout
- Demo credentials panel on the login page

Demo credentials:

| Role | Email | Password |
| --- | --- | --- |
| Admin / Owner | `admin@propcare.demo` | `PropCare@Admin123` |
| Property Manager | `manager@propcare.demo` | `PropCare@Manager123` |
| Tenant | `tenant@propcare.demo` | `PropCare@Tenant123` |
| Maintenance Staff | `staff@propcare.demo` | `PropCare@Staff123` |

Auth API calls:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/demo-credentials`
- `POST /api/auth/ensure-demo-accounts`

## Validation

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

## Notes

AWS deployment, Amazon RDS cloud connectivity, production identity management, password reset, email invitations, production file storage, and complete business workflows will be added in later sprints. No secrets or production credentials are configured in the frontend.
