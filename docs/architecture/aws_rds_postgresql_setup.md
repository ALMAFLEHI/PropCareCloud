# AWS RDS PostgreSQL Setup

## Purpose

Amazon RDS PostgreSQL is the planned cloud database provider for PropCare Cloud.

This stage moves the database target from local PostgreSQL to Amazon RDS PostgreSQL while keeping the same application architecture:

```text
React frontend -> ASP.NET Core Web API -> EF Core -> Amazon RDS PostgreSQL
```

The application still uses EF Core migrations. Real credentials must be supplied through local environment variables or later AWS deployment configuration. No database password, AWS key, or private connection string should be committed.

## Recommended RDS Settings For Assignment Development

Use the AWS Console to create the RDS database manually. Verify AWS cost and billing settings before creating any resource.

Suggested placeholder settings:

- Engine: PostgreSQL
- DB instance identifier: `propcarecloud-postgres`
- Initial database name: `propcarecloud_db`
- Master username: `propcareadmin`
- Region: user-selected, recommended near Malaysia such as `ap-southeast-1`
- Public access: enable only if needed for local migration/testing
- Security group inbound: PostgreSQL port `5432` from your current IP address only
- Storage: smallest cost-conscious assignment/development setting that satisfies the console requirements
- Backups: minimal assignment-safe setting

Do not paste the real master password into chat, documentation, source code, or screenshots.

## Manual AWS Console Checklist

1. Open the AWS Console.
2. Go to Amazon RDS.
3. Choose Create database.
4. Select Standard create.
5. Select PostgreSQL.
6. Choose an assignment/development sized DB instance.
7. Set DB instance identifier to `propcarecloud-postgres`.
8. Set initial database name to `propcarecloud_db`.
9. Set master username to `propcareadmin`.
10. Choose a strong password and store it locally outside the repository.
11. Select the target region, for example `ap-southeast-1`.
12. Configure public access only if local migration/testing requires it.
13. Attach or create a security group with restricted inbound PostgreSQL access.
14. Create the DB instance and wait until the status is Available.
15. Copy the RDS endpoint without exposing the password.

## Security Group Guidance

For local migration/testing, allow inbound PostgreSQL traffic only from your current public IP address:

```text
Type: PostgreSQL
Protocol: TCP
Port: 5432
Source: <your-current-public-ip>/32
```

Do not open port `5432` to `0.0.0.0/0`.

Later deployment work can use application security group access instead of public IP access.

## Connection String Format

Use this placeholder format only:

```text
Host=<rds-endpoint>;Port=5432;Database=propcarecloud_db;Username=propcareadmin;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

The password placeholder must be replaced only in your local terminal or local secret store. Never commit the real value.

## PowerShell Environment Variable

Set the RDS connection string only in your local PowerShell session:

```powershell
$env:PROPCLOUD_CONNECTION_STRING="Host=<rds-endpoint>;Port=5432;Database=propcarecloud_db;Username=propcareadmin;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true"
```

Important:

- Type this locally only.
- Never paste the real password into chat.
- Never commit this value.
- Close the terminal when you are done if you want to clear the session variable.

## Migration Flow

1. Confirm RDS is Available in AWS Console.
2. Restrict the RDS security group inbound rule to your current IP `/32`.
3. Set `PROPCLOUD_CONNECTION_STRING` in PowerShell.
4. Validate the local environment variable:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\validate-rds-environment.ps1
```

5. Apply EF Core migrations to RDS:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\update-rds-database.ps1
```

6. Start the backend from the same PowerShell session so it can read `PROPCLOUD_CONNECTION_STRING`:

```powershell
dotnet run --project .\backend\src\PropCareCloud.Api\PropCareCloud.Api.csproj
```

7. Check RDS readiness:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-rds-api-readiness.ps1
```

8. Seed demo data:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\seed-rds-demo-data.ps1
```

9. Start the frontend and verify it shows the same cloud-backed data:

```powershell
cd frontend
npm run dev
```

## Evidence Screenshots Needed

Capture only screenshots that do not expose passwords, AWS keys, or private credentials.

- AWS RDS instance status showing Available.
- RDS endpoint/details with no password visible.
- Security group inbound rule showing restricted IP access.
- Migration command success.
- `/api/database/readiness` connected to RDS.
- Seed demo data success.
- Frontend dashboard showing RDS-backed data.

## Cleanup And Cost Warning

RDS resources can create AWS charges. Verify the AWS billing dashboard and AWS cost settings before and after creating resources.

Stop or delete assignment resources later if they are no longer needed. Do not include exact pricing in project documentation because AWS cost depends on region, instance class, storage, backup settings, and account eligibility.
