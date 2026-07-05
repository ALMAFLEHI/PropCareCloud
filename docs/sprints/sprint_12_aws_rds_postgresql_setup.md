# Sprint 12 - AWS RDS PostgreSQL Setup & Migration

## Sprint Goal

Prepare PropCare Cloud for Amazon RDS PostgreSQL connection, EF Core migration, API readiness validation, and demo data seeding without committing secrets or creating AWS resources automatically.

## Why RDS Is Needed For Task 1

The assignment cloud architecture needs a managed cloud database stage. Amazon RDS PostgreSQL provides the target database provider while preserving the existing EF Core and PostgreSQL data access model.

## Current Local PostgreSQL vs Target RDS PostgreSQL

Current local architecture:

```text
React frontend -> ASP.NET Core Web API -> EF Core -> Local PostgreSQL
```

Target Sprint 12 architecture:

```text
React frontend -> ASP.NET Core Web API -> EF Core -> Amazon RDS PostgreSQL
```

Local PostgreSQL remains supported for development. RDS PostgreSQL is introduced as the cloud database target for manual setup and evidence capture.

## Files And Scripts Added

- `docs/architecture/aws_rds_postgresql_setup.md`
- `scripts/aws/check-aws-cli.ps1`
- `scripts/aws/validate-rds-environment.ps1`
- `scripts/aws/update-rds-database.ps1`
- `scripts/aws/check-rds-api-readiness.ps1`
- `scripts/aws/seed-rds-demo-data.ps1`
- `scripts/aws/check-rds-sprint12.ps1`

Supporting updates:

- `.gitignore`
- `README.md`
- `backend/README.md`
- Runtime database configuration now safely falls back to `PROPCLOUD_CONNECTION_STRING` when the committed `DefaultConnection` value is empty.

## Manual AWS RDS Setup Checklist

1. Verify AWS billing and cost settings.
2. Create an Amazon RDS PostgreSQL database manually in AWS Console.
3. Use `propcarecloud-postgres` as the suggested DB instance identifier.
4. Use `propcarecloud_db` as the initial database name.
5. Use `propcareadmin` as the suggested master username.
6. Choose a strong password and keep it outside the repository.
7. Use a region selected by the user, recommended near Malaysia such as `ap-southeast-1`.
8. Allow public access only if needed for local migration/testing.
9. Restrict inbound PostgreSQL `5432` to the user's current IP `/32`.
10. Wait until the RDS instance status is Available.

## Safe Connection String Setup

Use a local PowerShell environment variable only:

```powershell
$env:PROPCLOUD_CONNECTION_STRING="Host=<rds-endpoint>;Port=5432;Database=propcarecloud_db;Username=propcareadmin;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true"
```

Do not commit this value. Do not paste it into chat. Do not store it in `appsettings.json`.

## Migration Process

Validate the RDS environment variable:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\validate-rds-environment.ps1
```

Apply EF Core migrations to RDS:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\update-rds-database.ps1
```

The migration script uses the existing EF Core migration setup and does not print the full connection string.

## Seed Process

Start the backend from the same PowerShell session where `PROPCLOUD_CONNECTION_STRING` is set:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

Check readiness:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-rds-api-readiness.ps1
```

Seed demo data:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\seed-rds-demo-data.ps1
```

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
```

Sprint 12 RDS support checklist:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-rds-sprint12.ps1
```

The RDS checklist requires a real RDS connection string and manual AWS setup before it can fully pass.

## Security Notes

- No database password is committed.
- No AWS access key is committed.
- No AWS secret key is committed.
- No `.env` file is committed.
- No private key file is committed.
- No RDS endpoint with password is stored in committed config.
- `appsettings.json` keeps an empty `DefaultConnection`.
- RDS connection strings must be supplied through environment variables only.
- Screenshots must not show passwords, AWS keys, private keys, or production JWT secrets.

## Final Evidence Captured

- `docs/sprints/screenshots/sprint_12_rds_instance_available.png`
- `docs/sprints/screenshots/sprint_12_rds_endpoint_details.png`
- `docs/sprints/screenshots/sprint_12_rds_security_group_restricted_ip.png`
- `docs/sprints/screenshots/sprint_12_rds_migration_success.png`
- `docs/sprints/screenshots/sprint_12_rds_api_readiness.png`
- `docs/sprints/screenshots/sprint_12_frontend_rds_backed_dashboard.png`

## Manual Validation Results

- RDS instance created successfully.
- RDS status: Available.
- Engine: PostgreSQL.
- Instance class: `db.t3.micro`.
- Database name: `propcarecloud_db`.
- Public accessibility enabled for local migration testing.
- Security group inbound PostgreSQL `5432` restricted to one `/32` IP.
- EF Core migrations applied successfully.
- API readiness passed.
- `canConnect`: true.
- Pending migrations: 0.
- Applied migrations: 4.
- Demo seed endpoint passed and skipped duplicates because data already existed.
- Demo auth accounts confirmed ready.
- Frontend dashboard loaded live RDS-backed data.

## Final Status

COMPLETE.

Sprint 12 is fully closed. AWS RDS PostgreSQL setup, migration, API readiness validation, seed validation, frontend RDS-backed dashboard validation, security checks, and evidence screenshots are complete.
