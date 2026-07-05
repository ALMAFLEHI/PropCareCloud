# AWS Deployment Plan

## Purpose

This document describes the planned AWS deployment shape for PropCare Cloud after Sprint 12 RDS setup.

## Simple Deployment Architecture

```text
User browser
  -> AWS-hosted React/Vite frontend
  -> AWS-hosted ASP.NET Core Web API
  -> Amazon RDS PostgreSQL
```

## Backend Deployment Option

The ASP.NET Core API can be deployed using either:

- AWS Elastic Beanstalk for a managed application environment.
- EC2 for a manually managed Windows or Linux host.

The backend deployment package is produced by:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-backend-package.ps1
```

The package is created locally only. The script does not create, update, or delete AWS resources.

## Frontend Deployment Option

The React/Vite frontend can be hosted as static files, for example with Amazon S3 static website hosting or another AWS-hosted frontend option.

The frontend build is produced by:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-frontend-package.ps1
```

Before the final production build, set:

```powershell
$env:VITE_API_BASE_URL="https://<deployed-backend-host>"
```

Upload only the generated `frontend/dist` files during manual deployment.

## RDS Connection

The deployed backend connects to Amazon RDS PostgreSQL through environment variables.

Required backend variable:

```text
PROPCLOUD_CONNECTION_STRING
```

Use a value in this placeholder shape only:

```text
Host=<rds-endpoint>;Port=5432;Database=propcarecloud_db;Username=propcareadmin;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

Never commit the real value.

## CORS Explanation

Local development remains allowed:

```text
http://localhost:5173
```

Deployed frontend origins should be configured through:

```text
PROPCLOUD_ALLOWED_ORIGINS
```

Use comma or semicolon separation if more than one frontend origin is needed. Do not hard-code a fake production URL as the only allowed origin.

## Security Notes

- Store database passwords only in AWS environment configuration or a managed secret store.
- Store `Jwt__SigningKey` only in AWS environment configuration.
- Do not commit `.env` files.
- Do not commit private keys or AWS credentials.
- Keep screenshots masked when showing environment variables.
- Confirm RDS security group rules are limited to the intended source.
- Keep local validation working before and after deployment preparation.

## Screenshots To Capture Later

- Backend deployed `/api/health` response.
- Backend deployed `/api/database/readiness` response connected to RDS.
- Deployed frontend welcome page.
- Deployed frontend login page.
- Admin dashboard using the deployed backend.
- AWS deployment service status.
- Deployment environment variables with secret values masked.
