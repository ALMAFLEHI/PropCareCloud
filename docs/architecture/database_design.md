# PropCare Cloud Database Design

## Database Name Suggestion

`propcarecloud_db`

## Target Provider

Amazon RDS PostgreSQL

Sprint 4 defines the database/domain model foundation only. The backend does not connect to Amazon RDS yet, and no real connection string or credentials are stored in the project.

## Table Purpose

- `user_profiles`
  - Stores application-level user profile records for owners, property managers, tenants, and maintenance staff. Authentication is not implemented yet, so `IdentityUserId` remains optional.
- `properties`
  - Stores managed property or building records, including address and lifecycle status.
- `rental_units`
  - Stores units within properties, including unit number, floor, bedrooms, and occupancy/maintenance status.
- `maintenance_requests`
  - Stores tenant maintenance requests, request category, priority, status, assignment, and completion timing.
- `maintenance_request_comments`
  - Stores request discussion and internal notes from users.
- `maintenance_request_attachments`
  - Stores metadata for future uploaded evidence files. `StorageKey` represents a future Amazon S3 object key, not a local file path.

## Main Relationships

- `property` 1 to many `rental_units`
- `rental_unit` 1 to many `maintenance_requests`
- `user_profile` tenant 1 to many `maintenance_requests`
- `user_profile` staff 1 to many assigned `maintenance_requests`
- `maintenance_request` 1 to many `maintenance_request_comments`
- `maintenance_request` 1 to many `maintenance_request_attachments`
- `user_profile` 1 to many `maintenance_request_comments`
- `user_profile` 1 to many uploaded `maintenance_request_attachments`

## Text ERD

```text
UserProfile
  |-- TenantRequests --> MaintenanceRequest
  |-- AssignedMaintenanceRequests --> MaintenanceRequest
  |-- Comments --> MaintenanceRequestComment
  |-- UploadedAttachments --> MaintenanceRequestAttachment

Property
  |-- RentalUnits --> RentalUnit
        |-- MaintenanceRequests --> MaintenanceRequest
              |-- Comments --> MaintenanceRequestComment
              |-- Attachments --> MaintenanceRequestAttachment
```

## Future Cloud Extension Notes

- Attachments will later map to Amazon S3 objects through `StorageKey`.
- Notifications may later use Amazon SNS or SQS when requests are assigned, updated, or completed.
- The backend will later connect to Amazon RDS PostgreSQL using a secure connection string outside source control.
- Cloud monitoring can later be connected through CloudWatch and X-Ray.

## Sprint 5 Migration Foundation

- Initial EF Core migration created: `InitialCreate`
- Migration files location: `backend/src/PropCareCloud.Api/Data/Migrations`
- PostgreSQL provider confirmed: `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.x
- Local EF tool configured: `dotnet-ef` 8.0.x through `.config/dotnet-tools.json`
- Real database update deferred.
- Amazon RDS connection deferred to a later cloud sprint.

Sprint 5 prepares the schema migration foundation without requiring PostgreSQL to be installed and without storing any real database credentials.

## Sprint 6 Local Seed Data Foundation

Sprint 6 adds a local demo seed data workflow for testing the database model before Amazon RDS is introduced.

Demo seed data includes:

- 1 admin/owner user profile
- 1 property manager user profile
- 2 tenant user profiles
- 2 maintenance staff user profiles
- 2 properties
- 4 rental units
- 4 maintenance requests
- request comments
- attachment metadata using fake future S3-style storage keys

The seed workflow is intentionally local-development focused. It checks existing `UserProfile` records first and skips duplicate seeding when data already exists.

New Sprint 6 endpoints:

- `GET /api/database/readiness`
- `POST /api/seed/demo-data`

Amazon RDS connectivity remains deferred. The readiness endpoint and seed endpoint do not expose connection strings, passwords, usernames, or host values.

## Sprint 7 CRUD API Layer

Sprint 7 connects the domain model to real database-backed API workflows for Task 1 system implementation.

Property and unit workflow:

- Properties can be listed, created, viewed, updated, and deleted.
- Rental units can be managed under a property.
- Property deletion is blocked when rental units exist.
- Rental unit deletion is blocked when maintenance requests exist.

Maintenance request workflow:

- Tenant maintenance requests can be listed, filtered, created, viewed, updated, assigned, and moved through statuses.
- Request assignment validates that the assigned profile has the `MaintenanceStaff` role.
- Request creation validates that the requesting profile has the `Tenant` role.
- Completing a request records `CompletedAtUtc`.
- Comments can be added and listed for each request.

This CRUD API layer proves that the system is now using the EF Core database model for real backend workflows, while authentication, authorization enforcement, frontend CRUD screens, Amazon RDS, and cloud deployment remain planned for later sprints.
