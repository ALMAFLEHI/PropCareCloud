# Sprint 13 - AWS Deployment

## Sprint Goal

Deploy PropCare Cloud to AWS using Elastic Beanstalk for the backend, Amazon S3 static website hosting for the frontend, and Amazon RDS PostgreSQL for the database.

## Target Architecture

```text
React/Vite frontend hosted on AWS -> ASP.NET Core Web API hosted on AWS -> Amazon RDS PostgreSQL
```

Final AWS deployment:

- Backend: AWS Elastic Beanstalk running the ASP.NET Core Web API.
- Frontend: Amazon S3 static website hosting.
- Database: Amazon RDS PostgreSQL in `us-east-1`.

## Deployment Preparation Completed

- Backend keeps `PROPCLOUD_CONNECTION_STRING` support for RDS.
- Backend can read `PORT` when `ASPNETCORE_URLS` is not already configured.
- Backend CORS keeps local `http://localhost:5173` support and can add deployed frontend origins through `PROPCLOUD_ALLOWED_ORIGINS`.
- Frontend continues to use `VITE_API_BASE_URL` with a local fallback.
- Safe backend and frontend package scripts were added under `scripts/aws/`.
- A combined deployment readiness script was added.
- No AWS resources are created, updated, or deleted by these scripts.

## Final Deployment Summary

- A normal AWS account was used instead of AWS Academy because Academy reset resources during the deployment work.
- Amazon RDS PostgreSQL was created in `us-east-1`.
- The backend was deployed using AWS Elastic Beanstalk.
- The frontend was deployed using Amazon S3 static website hosting.
- The backend connected to RDS successfully.
- The deployed frontend login and dashboard work from the S3 website URL.
- The Sprint 13 seed repair was deployed and verified.
- Demo properties, units, users, tenant-unit assignments, and maintenance requests now show correctly in the deployed admin dashboard.

## Final Live URLs

- Backend API: `http://propcarecloud-api.us-east-1.elasticbeanstalk.com`
- Frontend website: `http://propcarecloud-frontend-20260706.s3-website-us-east-1.amazonaws.com`

## Demo Seed Repair Update

During normal AWS deployment, demo authentication accounts existed but the portfolio seed data was empty. The dashboard showed active users, but properties, units, occupied-unit assignments, maintenance requests, comments, and attachment metadata were missing.

Root cause:

- `POST /api/auth/ensure-demo-accounts` created the five demo login accounts and profiles.
- The previous `POST /api/seed/demo-data` logic skipped all seeding when any `UserProfile` already existed.
- Because the auth endpoint ran first in AWS, the portfolio data was considered already seeded even though it was incomplete.

Seed repair completed:

- `POST /api/seed/demo-data` now behaves as an idempotent repair operation.
- It ensures the five demo account-backed profiles are present.
- It repairs or creates the demo properties, rental units, tenant-unit assignments, maintenance requests, comments/activity notes, and attachment metadata.
- Running the endpoint repeatedly does not duplicate demo portfolio records.
- Running the endpoint after `POST /api/auth/ensure-demo-accounts` repairs the missing portfolio data.
- The seed response now reports success, created/repaired status, created row counts, repaired row count, and final totals.

After uploading the rebuilt backend package to Elastic Beanstalk, the deployed seed and demo-account endpoints were run:

```text
POST /api/seed/demo-data
POST /api/auth/ensure-demo-accounts
```

The deployed frontend dashboard, properties, requests, and access management pages were then verified with seeded data.

## Final Validation Results

- Backend `/api/health` works on the deployed Elastic Beanstalk API.
- Backend `/api/database/readiness` works on the deployed Elastic Beanstalk API.
- Database readiness returned `canConnect: true`.
- Database readiness returned `pendingMigrations: 0`.
- Database readiness returned `appliedMigrations: 4`.
- Deployed seed repair completed successfully.
- Admin dashboard shows seeded demo data:
  - Open requests: `4`
  - Properties: `2`
  - Active users: `5`
  - High priority requests: `1`

## Backend Package Process

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-backend-package.ps1
```

The script:

- Restores the ASP.NET Core API project.
- Publishes the backend in Release mode.
- Outputs files under `artifacts/deployment/backend/publish`.
- Creates `artifacts/deployment/backend/PropCareCloud.Api.zip`.
- Does not deploy to AWS.
- Does not include real secrets.

## Frontend Package Process

Before a final deployed build, set the backend API base URL locally:

```powershell
$env:VITE_API_BASE_URL="https://<deployed-backend-host>"
```

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\aws\build-sprint13-frontend-package.ps1
```

The script:

- Runs `npm ci` when `package-lock.json` is available.
- Builds the React/Vite production bundle.
- Outputs static frontend files under `frontend/dist`.
- Does not upload to S3.

## Required AWS Environment Variables

Backend deployment environment variables:

- `PROPCLOUD_CONNECTION_STRING`: RDS PostgreSQL connection string. Store only in AWS environment configuration.
- `Jwt__SigningKey`: production JWT signing key for ASP.NET Core configuration binding.
- `PROPCLOUD_ALLOWED_ORIGINS`: deployed frontend origin list, separated by comma or semicolon.
- `ASPNETCORE_URLS` or `PORT`: hosting bind configuration, depending on the AWS hosting option.

Frontend build variable:

- `VITE_API_BASE_URL`: deployed backend API base URL used at build time.

Do not commit any real values.

## Final Evidence Captured

- `docs/sprints/screenshots/sprint_13_backend_deployed_health.png`
- `docs/sprints/screenshots/sprint_13_backend_database_readiness_deployed.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_welcome.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_login.png`
- `docs/sprints/screenshots/sprint_13_live_dashboard_admin.png`
- `docs/sprints/screenshots/sprint_13_aws_deployment_service_status.png`

The AWS account-identifying area in the service status screenshot was masked before commit. The S3 website URL is visible in the deployed welcome/login screenshots, and the RDS readiness result is visible in the deployed database-readiness screenshot.

## Security Checklist

- No real database password committed.
- No full connection string committed.
- No AWS access key committed.
- No AWS secret key committed.
- No private key committed.
- No `.env` file committed.
- `appsettings.json` keeps an empty `DefaultConnection`.
- Deployment secrets are configured only in AWS environment variables.
- Screenshots must mask or hide secret values.
- Budget alert was created before deployment.
- Resources should be deleted after assignment submission if they are no longer needed.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-deployment-readiness.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-demo-data.ps1
```

## Final Status

COMPLETE.

Sprint 13 AWS deployment is fully closed. Elastic Beanstalk backend deployment, S3 frontend deployment, RDS PostgreSQL connectivity, deployed API validation, seed repair verification, admin dashboard validation, documentation, security checks, and final evidence screenshots are complete.
