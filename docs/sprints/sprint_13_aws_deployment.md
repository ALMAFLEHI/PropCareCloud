# Sprint 13 - AWS Deployment Preparation and Seed Repair

## Sprint Goal

Prepare PropCare Cloud for AWS deployment by adding deployment-ready configuration, local packaging scripts, deployment readiness checks, and documentation without creating AWS resources automatically.

## Target Architecture

```text
React/Vite frontend hosted on AWS -> ASP.NET Core Web API hosted on AWS -> Amazon RDS PostgreSQL
```

Planned AWS deployment options:

- Backend: Elastic Beanstalk or EC2 running the ASP.NET Core Web API.
- Frontend: Amazon S3 static website hosting or another AWS-hosted frontend option.
- Database: existing Amazon RDS PostgreSQL from Sprint 12.

## Deployment Preparation Completed

- Backend keeps `PROPCLOUD_CONNECTION_STRING` support for RDS.
- Backend can read `PORT` when `ASPNETCORE_URLS` is not already configured.
- Backend CORS keeps local `http://localhost:5173` support and can add deployed frontend origins through `PROPCLOUD_ALLOWED_ORIGINS`.
- Frontend continues to use `VITE_API_BASE_URL` with a local fallback.
- Safe backend and frontend package scripts were added under `scripts/aws/`.
- A combined deployment readiness script was added.
- No AWS resources are created, updated, or deleted by these scripts.

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

After uploading the rebuilt backend package to Elastic Beanstalk, run:

```text
POST /api/seed/demo-data
POST /api/auth/ensure-demo-accounts
```

Then verify the deployed frontend dashboard, properties, requests, and access management pages show seeded data.

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

## Manual AWS Console Steps Placeholder

The user will perform the AWS deployment manually after this preparation sprint.

Backend deployment outline:

1. Choose Elastic Beanstalk or EC2 for the ASP.NET Core API.
2. Upload the backend package from `artifacts/deployment/backend/PropCareCloud.Api.zip`.
3. Configure environment variables in AWS with masked/hidden values.
4. Confirm `/api/health` responds.
5. Confirm `/api/database/readiness` connects to RDS.
6. Confirm `/api/system-info` responds without exposing secrets.

Frontend deployment outline:

1. Build the frontend with `VITE_API_BASE_URL` pointing to the deployed backend.
2. Upload the contents of `frontend/dist` to the selected AWS frontend hosting target.
3. Configure frontend hosting access and routing.
4. Confirm the welcome page, login page, and authenticated dashboard work with the deployed backend.

## Evidence Screenshots Needed

- `docs/sprints/screenshots/sprint_13_backend_deployed_health.png`
- `docs/sprints/screenshots/sprint_13_backend_database_readiness_deployed.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_welcome.png`
- `docs/sprints/screenshots/sprint_13_frontend_deployed_login.png`
- `docs/sprints/screenshots/sprint_13_live_dashboard_admin.png`
- `docs/sprints/screenshots/sprint_13_aws_deployment_service_status.png`
- `docs/sprints/screenshots/sprint_13_deployment_environment_variables_masked.png`

The environment variable screenshot must not show secret values.

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

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-deployment-readiness.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\aws\check-sprint13-demo-data.ps1
```

## Final Status

SEED REPAIR CODE SUPPORT COMPLETE.

Manual upload of the rebuilt backend package and final deployed demo-data verification are still pending.
