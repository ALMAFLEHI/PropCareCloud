# AWS Deployment Plan

## Purpose

This document describes the AWS deployment shape used for PropCare Cloud after Sprint 12 RDS setup and Sprint 13 deployment.

## Simple Deployment Architecture

```text
User browser
  -> AWS-hosted React/Vite frontend
  -> AWS-hosted ASP.NET Core Web API
  -> Amazon RDS PostgreSQL
```

## Task 1 And Task 2 Boundary

Task 1 uses the server-based deployment architecture above:

- S3 static website hosting for the React/Vite frontend.
- Elastic Beanstalk for the ASP.NET Core Web API.
- Amazon RDS PostgreSQL for data persistence.
- Sprint 14 Tenant Registration & Approval Workflow remains part of this same architecture.

Task 2 later introduces the additional cloud-service integrations:

- API Gateway and Lambda integration.
- S3 maintenance attachment storage.
- SNS/SQS notification workflow.
- CloudWatch and X-Ray monitoring.

Those Task 2 services are not part of Sprint 14.

## Backend Deployment

The selected backend deployment target is AWS Elastic Beanstalk.

Live backend API:

```text
http://propcarecloud-api.us-east-1.elasticbeanstalk.com
```

The backend deployment package is produced by:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-backend-package.ps1
```

The package is created locally only. The script does not create, update, or delete AWS resources. The package is uploaded manually as an Elastic Beanstalk application version.

## Frontend Deployment

The selected frontend deployment target is Amazon S3 static website hosting.

Live frontend website:

```text
http://propcarecloud-frontend-20260706.s3-website-us-east-1.amazonaws.com
```

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

The deployed backend connects to Amazon RDS PostgreSQL through Elastic Beanstalk environment variables.

Database provider:

```text
Amazon RDS PostgreSQL in us-east-1
```

Database name:

```text
propcarecloud_db
```

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

The deployed S3 frontend origin is allowed through:

```text
PROPCLOUD_ALLOWED_ORIGINS
```

Use comma or semicolon separation if more than one frontend origin is needed. The deployed frontend origin was configured so the S3-hosted UI can call the Elastic Beanstalk API.

## Network Security

- RDS PostgreSQL remains protected by security group rules.
- PostgreSQL port `5432` is allowed from the Elastic Beanstalk EC2 security group for backend-to-database access.
- Local migration/testing access should remain limited and removed when no longer needed.

## Security Notes

- Store database passwords only in AWS environment configuration or a managed secret store.
- Store `Jwt__SigningKey` only in AWS environment configuration.
- Do not commit `.env` files.
- Do not commit private keys or AWS credentials.
- Keep screenshots masked when showing environment variables.
- Confirm RDS security group rules are limited to the intended source.
- Keep local validation working before and after deployment preparation.
- Delete AWS resources after assignment submission if they are no longer needed.

## Sprint 13 Evidence Captured

- `docs/sprints/screenshots/sprint_13_backend_deployed_health.png`
- `docs/sprints/screenshots/sprint_13_backend_database_readiness_deployed.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_welcome.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_login.png`
- `docs/sprints/screenshots/sprint_13_live_dashboard_admin.png`
- `docs/sprints/screenshots/sprint_13_aws_deployment_service_status.png`
