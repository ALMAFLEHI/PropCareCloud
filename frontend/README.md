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
| Tenant - Sara | `tenant@propcare.demo` | `PropCare@Tenant123` |
| Tenant - Imran | `imran@propcare.demo` | `PropCare@Imran123` |
| Maintenance Staff | `staff@propcare.demo` | `PropCare@Staff123` |

Auth API calls:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/demo-credentials`
- `POST /api/auth/ensure-demo-accounts`

## Sprint 9.1 Role-Based UI Behavior

The frontend now follows the backend RBAC rules instead of only changing dashboard text.

- Admin / Owner sees dashboard, maintenance requests, properties, and users/roles.
- Property Manager sees dashboard, maintenance requests, and properties.
- Tenant sees dashboard and maintenance requests only.
- Maintenance Staff sees dashboard and maintenance requests only.
- Tenant request creation uses `GET /api/user-profiles/me/assigned-units` and does not ask for `tenantProfileId`.
- Tenant request cards show tracking only; no status or assignment controls.
- Maintenance Staff request page shows assigned jobs only, no create button, and status options limited to In progress and Completed.
- Admin / Owner and Property Manager request page includes status controls and staff assignment controls.

Sprint 9.1 API calls:

- `GET /api/user-profiles/maintenance-staff`
- `GET /api/user-profiles/tenants`
- `GET /api/user-profiles/me/assigned-units`
- `PATCH /api/maintenance-requests/{id}/assign`

## Sprint 9.2 Tenant Isolation Demo

The login page now shows five demo accounts, including two tenant accounts:

- Sara Tenant: primary tenant demo.
- Imran Tenant: secondary tenant isolation demo.

Frontend validation expectations:

- Sara and Imran each see only Dashboard and Maintenance Requests.
- Sara and Imran receive request data filtered by the backend for their own tenant profile.
- Sara and Imran get assigned-unit dropdowns from `GET /api/user-profiles/me/assigned-units`.
- Tenant users do not see property management, users/roles, status update controls, or staff assignment controls.

## Sprint 9.3 Tenant Unit Consistency

The frontend did not require new logic for Sprint 9.3. It already renders assigned tenant units from the backend.

Manual validation expectations after the backend fix:

- Sara Tenant assigned units: `B-1102` and `A-0101`.
- Sara Tenant requests: `Lobby access card issue` and `Kitchen sink leaking`.
- Imran Tenant assigned units: `A-0205` and `B-1208`.
- Imran Tenant requests: `Air conditioner not cooling` and `Bathroom light flickering`.
- The tenant create request dropdown should show only the signed-in tenant's assigned units.

## Validation

From the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

## Notes

AWS deployment, Amazon RDS cloud connectivity, production identity management, password reset, email invitations, production file storage, and complete business workflows will be added in later sprints. No secrets or production credentials are configured in the frontend.
