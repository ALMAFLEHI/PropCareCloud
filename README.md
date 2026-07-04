# PropCare Cloud

PropCare Cloud is a planned cloud-based property maintenance and tenant service portal for managing rental property support requests, tenant communication, maintenance workflows, and owner oversight.

## Assignment

- Module: CT071-3-3-DDAC Designing and Developing Applications on the Cloud
- Chosen industry: Property Management
- Methodology: Agile Iterative Development

## Main Architecture

The planned solution uses a separated frontend and backend architecture:

```text
React frontend -> ASP.NET Core Web API -> EF Core -> Amazon RDS PostgreSQL
```

This keeps the user interface, business logic, and data persistence responsibilities separate and easier to extend across the assignment tasks.

## Planned Tech Stack

- Frontend: React with Vite
- Backend: ASP.NET Core Web API
- Data access: Entity Framework Core
- Database: PostgreSQL hosted on Amazon RDS
- Version control: Git
- Documentation: Markdown under the `docs/` folder
- Scripts: PowerShell helper scripts under the `scripts/` folder

## Planned AWS Services

- Amazon RDS for PostgreSQL
- Amazon S3 for document or image storage
- Amazon API Gateway for cloud API exposure
- AWS Lambda for selected serverless workflows
- Amazon SNS or SQS for notifications and maintenance workflow messaging
- Amazon CloudWatch and AWS X-Ray for monitoring, logs, and tracing

## Sprint Workflow Summary

The project will be developed iteratively. Sprint 1 prepares the workspace, folder structure, documentation, and environment checks. Later sprints will introduce the React frontend, ASP.NET Core Web API, database design, cloud integration, security, deployment, and monitoring.

## Sprint 2 Backend Foundation

- Backend solution: `backend/PropCareCloud.sln`
- API project: `backend/src/PropCareCloud.Api`
- Test project: `backend/tests/PropCareCloud.Api.Tests`
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

## Sprint 3 Frontend Foundation

- Frontend path: `frontend/`
- Main stack: React, TypeScript, Vite, Tailwind CSS
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

- Local run command:

```powershell
cd frontend
npm run dev
```

## Sprint 4 Database Domain Foundation

- Database/domain model foundation created in `backend/src/PropCareCloud.Api/Domain`
- EF Core `AppDbContext` created in `backend/src/PropCareCloud.Api/Data/AppDbContext.cs`
- Planned database provider: Amazon RDS PostgreSQL
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
```

RDS setup, real connection strings, and migrations will be handled in a later sprint.

## Sprint 5 Database Migration Setup

- EF Core migration foundation added.
- Initial migration: `InitialCreate`.
- PostgreSQL setup documentation: `docs/architecture/postgresql_local_setup.md`
- Database status endpoint: `GET /api/database/status`
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
```

Real database credentials, RDS connectivity, and cloud deployment are still deferred to later sprints.

## Sprint 6 Local PostgreSQL Readiness and Seed Data

- Sprint 6 is complete for local validation.
- Local PostgreSQL readiness foundation added and tested with PostgreSQL 16.14.
- Local database `propcarecloud_db` was used for EF migration and demo seed validation.
- Database readiness endpoint: `GET /api/database/readiness`
- Local demo seed endpoint: `POST /api/seed/demo-data`
- Seed data foundation covers sample users, properties, units, maintenance requests, comments, and fake future S3-style attachment metadata.
- The seed endpoint was tested successfully and repeat execution skipped duplicate demo data.
- Local credentials must use user-secrets or environment variables; no real secrets are committed.
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
```

Amazon RDS connectivity, authentication, full CRUD screens, and AWS deployment remain deferred to later sprints.

## Sprint 7 Backend CRUD APIs

- Backend CRUD APIs added for Task 1 system implementation.
- Supports property and rental unit management.
- Supports tenant maintenance request workflow, staff assignment, status updates, and comments.
- Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-crud-api.ps1
```

Authentication, authorization enforcement, frontend CRUD integration, Amazon RDS, and AWS deployment remain deferred to later sprints.

## Sprint 8 Frontend CRUD Integration

- React frontend integrated with Sprint 7 CRUD APIs.
- Dashboard now shows live property/request counts and recent maintenance requests.
- Properties page lists backend properties, displays selected property units, and creates properties.
- Maintenance Requests page lists backend requests, creates demo requests with seeded IDs, and updates request status.
- Full-stack local validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

Authentication, authorization enforcement, production-ready user lookup, Amazon RDS cloud connectivity, and AWS deployment remain deferred to later sprints.

## Sprint 9 Authentication and Role-Based Dashboards

- Sprint 9 is complete.
- Demo JWT authentication added for assignment testing.
- Demo accounts are available for Admin / Owner, Property Manager, Tenant, and Maintenance Staff roles.
- Frontend login route: `/login`.
- Main frontend routes are protected and redirect unauthenticated users to `/login`.
- Dashboard and sidebar navigation adapt by signed-in role.
- Demo passwords are stored in the database as BCrypt hashes, not plaintext.

Demo credentials:

| Role | Email | Password |
| --- | --- | --- |
| Admin / Owner | `admin@propcare.demo` | `PropCare@Admin123` |
| Property Manager | `manager@propcare.demo` | `PropCare@Manager123` |
| Tenant - Sara | `tenant@propcare.demo` | `PropCare@Tenant123` |
| Tenant - Imran | `imran@propcare.demo` | `PropCare@Imran123` |
| Maintenance Staff | `staff@propcare.demo` | `PropCare@Staff123` |

Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

AWS Cognito, production password reset, email invitation flow, Amazon RDS cloud connectivity, and cloud deployment remain deferred to later sprints.

## Sprint 9.1 Real Role-Based Access Control

- Backend APIs now enforce role-based access instead of relying only on frontend route hiding.
- Maintenance request data is filtered by signed-in role:
  - Admin / Owner and Property Manager can view all demo requests.
  - Tenant can view only their own requests.
  - Maintenance Staff can view only assigned jobs.
- Tenants can create maintenance requests only for active assigned rental units.
- Maintenance Staff can update only assigned work to In Progress or Completed.
- Property and rental unit APIs require Admin / Owner or Property Manager.
- Frontend dashboards, request controls, assignment dropdowns, and status dropdowns now match the backend role rules.
- Sprint 9.1 migration: `AddTenantUnitAssignments`.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

AWS Cognito, production invitations, Amazon RDS cloud connectivity, and cloud deployment remain deferred to later sprints.

## Sprint 9.2 Tenant Unit Account Isolation

- Sprint 9.2 is complete.
- Multi-tenant account/unit logic validated for real SaaS-style tenant isolation.
- Sara Tenant and Imran Tenant are separate demo tenant login accounts with separate `UserProfile` records.
- The tenant isolation logic applies to all future tenants through authenticated `userProfileId` values and tenant-unit assignments.
- One tenant can have multiple active assigned units.
- One rental unit can have only one active tenant assignment at a time.
- Tenants see only their own maintenance requests.
- Tenants create maintenance requests only for their own active assigned units.
- Admin / Owner and Property Manager can still see/manage portfolio-level request data.
- Maintenance Staff still see only assigned jobs.
- Sprint 9.2 migration: `HardenTenantUnitAssignmentIndexes`.

Manual evidence screenshots were captured during final Sprint 9 evidence closure.

## Sprint 9.3 Tenant Request and Unit Consistency

- Sprint 9.3 is complete.
- Manual validation found that demo request units and assigned units did not fully match.
- Sara Tenant now has assigned units `B-1102` and `A-0101`.
- Imran Tenant now has assigned units `A-0205` and `B-1208`.
- Sara request units are only Sara assigned units.
- Imran request units are only Imran assigned units.
- The existing Sprint 9.2 model supports this without a new migration.
- Tenant request creation remains limited to active assigned units for the authenticated tenant.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Sprint 9 Final Evidence Closure

- Authentication and role-based dashboards are complete.
- Sprint 9.1 backend RBAC enforcement is complete.
- Sprint 9.2 tenant account/unit isolation is complete.
- Sprint 9.3 tenant request/unit consistency is complete.
- Manual evidence screenshots were captured and saved under `docs/sprints/screenshots/`.
- Next sprint: Sprint 10 Admin User & Access Management.
