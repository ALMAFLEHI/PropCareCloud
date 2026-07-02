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
