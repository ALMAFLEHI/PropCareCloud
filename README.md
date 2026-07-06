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

- Task 1 current system:
  - Amazon RDS for PostgreSQL
  - AWS Elastic Beanstalk for the ASP.NET Core backend
  - Amazon S3 static website hosting for the React frontend
- Task 2 later enhancements:
  - Amazon S3 for maintenance document or image storage
  - Amazon API Gateway for cloud API exposure
  - AWS Lambda for selected serverless workflows
  - Amazon SNS or SQS for notifications and maintenance workflow messaging
  - Amazon CloudWatch and AWS X-Ray for monitoring, logs, and tracing

## Sprint Workflow Summary

The project will be developed iteratively. Sprint 1 prepares the workspace, folder structure, documentation, and environment checks. Later sprints will introduce the React frontend, ASP.NET Core Web API, database design, cloud integration, security, deployment, and monitoring.

## Assignment Task Plan

Task 1 covers the core PropCare Cloud system:

- Core React frontend and ASP.NET Core backend system
- Amazon RDS PostgreSQL cloud database
- Elastic Beanstalk backend deployment
- S3 static website frontend deployment
- Tenant Registration & Approval Workflow
- Task 1 final PowerPoint/PDF and ZIP package

Task 2 is a later cloud-services extension:

- Sprint 16: Task 2 Architecture Design
- Sprint 17: API Gateway + Lambda + S3 Integration
- Sprint 18: SNS/SQS Notification
- Sprint 19: CloudWatch + X-Ray Monitoring
- Sprint 20: Task 2 DOCX Report

Sprint 14 does not add API Gateway, Lambda, S3 maintenance attachments, SNS, SQS, CloudWatch, or X-Ray. Those services remain part of Task 2.

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

## Sprint 10 Admin User & Access Management

- Sprint 10 is complete.
- Admin User & Access Management module added.
- Admin / Owner can list user accounts and filter by role/status.
- Admin / Owner can create internal Property Manager and Maintenance Staff accounts.
- Admin / Owner can edit profile names, disable/reactivate accounts, and reset passwords.
- Disabled accounts cannot log in.
- Admin / Owner can view tenant-unit assignments and assign tenants to available units.
- One rental unit cannot have two active tenant assignments.
- Evidence screenshots were captured under `docs/sprints/screenshots/`.
- Tenant onboarding remains deferred to a future invitation/register flow.
- No AWS Cognito, email sending, cloud deployment, or real secrets were added.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Sprint 11 Production UI/UX Polish & Workflow Enhancements

- Sprint 11 is complete for code.
- User-facing UI wording was cleaned so the application reads like a production property maintenance portal instead of sprint documentation.
- Login, dashboards, properties, requests, and access management screens now use professional product wording.
- Maintenance request detail route added: `/requests/:id`.
- Status timeline component added for request progress tracking.
- Activity notes are available from the request detail page with role-aware visibility.
- Tenant request views remain read-only for status/assignment controls.
- Maintenance Staff workflow remains limited to assigned jobs and allowed progress updates.
- Admin / Owner and Property Manager assignment/status controls remain available.
- Existing backend RBAC rules remain intact.
- Evidence screenshots will be captured later under `docs/sprints/screenshots/`.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Sprint 11.1 Premium Visual Theme & Public Landing Page

- Sprint 11.1 is complete for code.
- General public landing page added at `/welcome`.
- Premium visual theme layer added across the React frontend.
- Login and app UI were visually polished with softer surfaces, shadows, transitions, and teal/navy accents.
- Remaining visible developer-style request wording was cleaned up.
- No backend, API, database, authentication, RBAC, or business functionality changes were made.
- Evidence screenshots will be captured later under `docs/sprints/screenshots/`.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Sprint 11.2 Premium Landing & Portal Visual Storytelling

- Sprint 11.2 is complete.
- Premium landing page storytelling improved on `/welcome`.
- CSS/JSX-only product mockup visual added to preview the service request workflow.
- How it Works section added for report, assign, and track flow.
- Trust/security cues added for role-based access, tenant privacy, manager oversight, staff assignment tracking, and activity history.
- Login portal visual presentation improved with a visual-only side panel.
- Logout now returns users to `/welcome` after the existing token/user clearing step.
- No backend, API, database, authentication, RBAC, or business functionality changes were made.
- Evidence screenshots were captured under `docs/sprints/screenshots/`.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
```

## Sprint 12 AWS RDS PostgreSQL Setup & Migration

- Sprint 12 AWS RDS PostgreSQL setup and migration is complete.
- Safe Amazon RDS PostgreSQL setup documentation was added under `docs/architecture/`.
- Safe RDS helper scripts were added under `scripts/aws/`.
- The backend can use `PROPCLOUD_CONNECTION_STRING` for RDS migration and local API runtime validation.
- No real database passwords, AWS credentials, private keys, or `.env` files are committed.
- Manual RDS setup, EF Core migration, API readiness, seed validation, and frontend RDS-backed dashboard evidence were captured.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

RDS support commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-aws-cli.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\validate-rds-environment.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\update-rds-database.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-rds-api-readiness.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\seed-rds-demo-data.ps1
```

## Sprint 13 AWS Deployment

- Sprint 13 AWS deployment is complete.
- Backend API deployed to AWS Elastic Beanstalk:
  `http://propcarecloud-api.us-east-1.elasticbeanstalk.com`
- Frontend deployed to Amazon S3 static website hosting:
  `http://propcarecloud-frontend-20260706.s3-website-us-east-1.amazonaws.com`
- Amazon RDS PostgreSQL readiness was validated through the deployed backend.
- Database readiness returned `canConnect: true`, `pendingMigrations: 0`, and `appliedMigrations: 4`.
- Sprint 13 seed repair was deployed and validated.
- The deployed admin dashboard shows seeded properties, units, users, tenant assignments, and maintenance requests.
- No AWS keys, real passwords, full connection strings, private keys, or `.env` files are committed.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-deployment-readiness.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-demo-data.ps1
```

## Sprint 14 Tenant Registration & Approval Workflow

- Sprint 14 is code complete for local validation.
- Sprint 14 readiness fix is code complete and ready for live redeployment.
- Public tenant registration route added at `/register`.
- Welcome and login pages now link to the tenant access request flow.
- Public API endpoint added: `POST /api/tenant-registrations`.
- Admin / Owner and Property Manager review APIs added for listing, approving, rejecting, and loading available units.
- Protected frontend route added at `/tenant-registrations` for Admin / Owner and Property Manager.
- Approval creates or activates a tenant portal account, hashes the temporary password with BCrypt, and assigns the tenant to an available rental unit.
- Rejection marks the request rejected and does not create an account or tenant-unit assignment.
- Existing demo accounts, JWT authentication, RBAC rules, tenant isolation, RDS support, and Sprint 13 deployment package scripts are preserved.
- Demo seed data now includes approval-ready available units: `Cloud Residence - Unit A-0303` and `Harbor Heights - Unit B-1401`.
- The approval available-units endpoint now returns only unassigned units with `Available` status.
- Login page tenant access CTA was moved into a modern `New tenant?` secondary CTA block.
- Welcome page tenant access CTA was moved out of the crowded hero CTA row into a separate premium panel.
- Final login UX polish maps invalid credentials to a user-friendly message instead of a technical 401 error.
- Sprint 14 migration: `AddTenantRegistrationRequests`.
- No AWS resources were created, updated, or deployed automatically.
- No API Gateway, Lambda, S3 maintenance attachments, SNS/SQS, CloudWatch, or X-Ray work was added in Sprint 14.

Validation commands:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
```

Next sprint: Sprint 15 - Task 1 Final Package.
