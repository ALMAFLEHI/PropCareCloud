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
