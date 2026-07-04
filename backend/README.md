# PropCareCloud.Api Backend

PropCareCloud.Api is the ASP.NET Core Web API backend foundation for PropCare Cloud.

## Technology

- ASP.NET Core Web API
- .NET 8
- Controllers
- Swagger/OpenAPI in Development
- xUnit test project
- Entity Framework Core 8
- Npgsql Entity Framework Core PostgreSQL provider

## Restore

```powershell
dotnet restore .\backend\PropCareCloud.sln
```

## Build

```powershell
dotnet build .\backend\PropCareCloud.sln
```

## Run

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

## Test

```powershell
dotnet test .\backend\PropCareCloud.sln
```

## Current Endpoints

- `GET /api/health`
- `GET /api/system-info`
- `GET /api/domain-summary`
- `GET /api/database/status`
- `GET /api/database/readiness`
- `POST /api/seed/demo-data`

## Sprint 4 Database Domain Foundation

Sprint 4 adds the backend domain model and EF Core database foundation.

Domain entities added:

- `UserProfile`
- `Property`
- `RentalUnit`
- `MaintenanceRequest`
- `MaintenanceRequestComment`
- `MaintenanceRequestAttachment`

EF Core additions:

- `AppDbContext`
- Fluent API table and relationship configuration
- Enum-to-string storage configuration
- Planned database provider: Amazon RDS PostgreSQL

The application only registers `AppDbContext` when a `DefaultConnection` connection string exists. The current `appsettings.json` contains an empty placeholder only.

## Sprint 5 Migration and PostgreSQL Setup

Sprint 5 adds the EF Core migration and safe PostgreSQL setup foundation.

- EF Core Design package added for migration tooling.
- Local `dotnet-ef` tool configured through `.config/dotnet-tools.json`.
- Initial migration created: `InitialCreate`.
- Migration files: `backend/src/PropCareCloud.Api/Data/Migrations`.
- Local PostgreSQL setup documentation: `docs/architecture/postgresql_local_setup.md`.
- Database status endpoint added: `GET /api/database/status`.

Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-database-migration.ps1
```

Optional local database update command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
```

The optional update script requires `PROPCLOUD_CONNECTION_STRING` to be set and does not print the connection string.

## Sprint 6 Local PostgreSQL and Seed Data Foundation

Sprint 6 adds safe local PostgreSQL readiness checks and demo seed data support. Local PostgreSQL validation has been completed with PostgreSQL 16.14.

- Database readiness endpoint added: `GET /api/database/readiness`.
- Local demo seed endpoint added: `POST /api/seed/demo-data`.
- Seed data creates sample owners, managers, tenants, maintenance staff, properties, units, requests, comments, and fake future S3-style attachment metadata.
- The seed endpoint returns a safe `400 Bad Request` if no database connection is configured.
- Local database `propcarecloud_db` was created and the `InitialCreate` migration was applied.
- The readiness endpoint returned `canConnect: true` with zero pending migrations.
- The seed endpoint returned HTTP 200 and repeat execution skipped duplicates successfully.
- No real credentials are stored in committed configuration files.

Local setup and validation scripts:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-local-postgresql.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\update-local-database.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-seed-data.ps1
```

Use user-secrets or environment variables for local database credentials. Do not commit real connection strings or passwords.

## Sprint 7 Backend CRUD APIs

Sprint 7 adds database-backed CRUD APIs for property/unit management and maintenance request workflows.

Property endpoints:

- `GET /api/properties`
- `GET /api/properties/{id}`
- `POST /api/properties`
- `PUT /api/properties/{id}`
- `DELETE /api/properties/{id}`

Rental unit endpoints:

- `GET /api/properties/{propertyId}/units`
- `GET /api/properties/{propertyId}/units/{unitId}`
- `POST /api/properties/{propertyId}/units`
- `PUT /api/properties/{propertyId}/units/{unitId}`
- `DELETE /api/properties/{propertyId}/units/{unitId}`

Maintenance request endpoints:

- `GET /api/maintenance-requests`
- `GET /api/maintenance-requests/{id}`
- `POST /api/maintenance-requests`
- `PUT /api/maintenance-requests/{id}`
- `PATCH /api/maintenance-requests/{id}/assign`
- `PATCH /api/maintenance-requests/{id}/status`
- `DELETE /api/maintenance-requests/{id}`

Comment endpoints:

- `GET /api/maintenance-requests/{id}/comments`
- `POST /api/maintenance-requests/{id}/comments`

Validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-crud-api.ps1
```

Authentication and authorization enforcement were added in Sprint 9 and Sprint 9.1.

## Sprint 9 Authentication and Demo Accounts

Sprint 9 adds local/demo authentication for assignment testing.

Auth endpoints:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/demo-credentials`
- `POST /api/auth/ensure-demo-accounts`

Backend auth additions:

- `AuthUserAccount` entity
- `IAuthService` / `AuthService`
- `AuthController`
- JWT bearer authentication
- BCrypt password hashing for demo accounts
- EF Core migration: `AddAuthUserAccounts`

Demo credentials:

| Role | Email | Password |
| --- | --- | --- |
| Admin / Owner | `admin@propcare.demo` | `PropCare@Admin123` |
| Property Manager | `manager@propcare.demo` | `PropCare@Manager123` |
| Tenant - Sara | `tenant@propcare.demo` | `PropCare@Tenant123` |
| Tenant - Imran | `imran@propcare.demo` | `PropCare@Imran123` |
| Maintenance Staff | `staff@propcare.demo` | `PropCare@Staff123` |

The demo credentials are public assignment/testing accounts. Passwords are stored in the database as BCrypt hashes only. `Jwt:SigningKey` remains empty in committed configuration and uses a local development fallback for the assignment demo only.

## Sprint 9.1 Role-Based API Enforcement

Sprint 9.1 adds real backend role-based access control and data filtering.

RBAC additions:

- `ICurrentUserService` reads `userProfileId`, email, and role claims from the JWT.
- Authorization policies: `AdminOnly`, `AdminOrManager`, `AdminManagerOrStaff`, `AllRoles`.
- `TenantUnitAssignment` entity and `AddTenantUnitAssignments` migration.
- Demo tenant unit assignment setup without duplicate assignments.

API rules:

- Admin / Owner: full demo portfolio access.
- Property Manager: properties/units and all maintenance requests; no admin users/roles page.
- Tenant: own maintenance requests only; create request only for assigned active unit; no property management or status updates.
- Maintenance Staff: assigned requests only; update assigned work only to `InProgress` or `Completed`; no request creation or assignment.

Role-aware helper endpoints:

- `GET /api/user-profiles/maintenance-staff` - Admin / Owner and Property Manager.
- `GET /api/user-profiles/tenants` - Admin / Owner and Property Manager.
- `GET /api/user-profiles/me/assigned-units` - Tenant only.

No password hashes, real database passwords, AWS keys, or production JWT secrets are exposed by these endpoints.

## Sprint 9.2 Tenant Unit Isolation

Sprint 9.2 validates tenant/unit/account isolation for multiple tenant accounts.

Tenant account rules:

- Every tenant login account has a unique email.
- Every account maps to exactly one `UserProfile`.
- `AuthUserAccount.UserProfileId` is unique.
- Sara Tenant and Imran Tenant are separate demo accounts and separate tenant profiles.

Tenant assignment rules:

- One tenant can have multiple active assigned rental units.
- A rental unit can have only one active assignment where `IsActive` is true and `LeaseEndDateUtc` is null.
- Inactive historical assignments remain allowed.
- Migration `HardenTenantUnitAssignmentIndexes` adds the PostgreSQL filtered unique index for active rental-unit assignments.

Tenant request rules:

- Tenant request listing is filtered by the authenticated tenant profile.
- Tenant create request uses the authenticated tenant profile and ignores submitted `tenantProfileId`.
- Tenant create request is limited to active assigned units.
- Tenant status updates remain forbidden.

## Notes

Real RDS connectivity, AWS Cognito, production password reset, email invitation flow, and AWS deployment will be added in later sprints. No production secrets, AWS credentials, or real database connection strings are configured in this sprint.
