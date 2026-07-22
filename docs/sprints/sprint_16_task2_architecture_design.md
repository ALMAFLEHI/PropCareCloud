# Sprint 16 - Task 2 Architecture Design

## Sprint Goal

Design the Task 2 serverless and event-driven AWS extension while protecting the completed Task 1 cloud application.

## Task 1 Baseline Protected

- Task 1 is protected by the `task1-final` Git tag.
- Task 2 work is isolated on the `task2` branch.
- The existing Task 1 frontend, backend, database schema, migrations, authentication, RBAC, tenant isolation, deployment packages, and AWS resources were not changed.

## Previous Architecture

- React and Vite frontend hosted on Amazon S3 static website hosting.
- ASP.NET Core Web API hosted on AWS Elastic Beanstalk.
- Entity Framework Core data access.
- Amazon RDS PostgreSQL database.
- JWT authentication, backend RBAC, tenant request isolation, and tenant-unit isolation.

## Proposed Task 2 Architecture

- Existing Task 1 core path remains active: S3 frontend to Elastic Beanstalk API to RDS PostgreSQL.
- New serverless attachment path is added beside the existing application.
- Existing backend validates JWT, role access, maintenance request access, and tenant isolation before allowing attachment upload.
- API Gateway invokes Lambda to generate a short-lived S3 presigned upload URL.
- Browser uploads directly to a private S3 attachment bucket.
- S3 ObjectCreated events publish to SNS and SQS.
- CloudWatch and X-Ray monitor the serverless path.

## New AWS Components

- API Gateway.
- Lambda.
- S3 attachment bucket.
- SNS.
- SQS.
- CloudWatch.
- X-Ray.

## Main Design Decisions

- Task 2 extends Task 1 instead of replacing it.
- The existing ASP.NET Core backend remains the authorization boundary.
- File objects are stored in S3, while metadata remains in RDS.
- Presigned upload URLs reduce backend bandwidth use.
- SNS and SQS decouple upload events from notification processing.

## Security Decisions

- Frontend never receives AWS credentials.
- S3 attachment bucket remains private with Block Public Access enabled.
- Presigned URLs are short-lived.
- File type and size should be validated before upload authorization.
- Lambda IAM permissions should be limited to the attachment bucket actions required.
- RDS is not accessed directly by Lambda.
- Secrets and private configuration remain outside source control.

## Reliability Decisions

- Task 1 remains operational if the attachment service is unavailable.
- Direct-to-S3 upload reduces load on the Elastic Beanstalk API.
- Lambda scales automatically for presigned URL generation.
- SQS preserves event messages if downstream processing is delayed.
- RDS remains the source of truth for maintenance request and attachment metadata.

## Cost-Conscious Decisions

- Serverless components are used only for the small attachment workflow.
- S3 stores binary objects outside the database.
- Direct upload avoids routing large files through the backend.
- CloudWatch retention should be limited to an assignment-appropriate period.
- No duplicate database or second full backend environment is required.

## Diagram Files

- `docs/architecture/diagrams/task1_architecture_final.mmd`
- `docs/architecture/diagrams/task2_architecture_proposed.mmd`

SVG or PNG exports were not generated because Mermaid CLI was not available locally and no new diagram dependency was added to the application.

## Validation

Planned regression validation:

- `powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1`
- `powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1`
- `powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1`

Validation confirms Task 1 remains intact after architecture-only documentation changes.

## Evidence Needed

- `sprint_16_task1_architecture_blueprint.png`
- `sprint_16_task2_architecture_blueprint.png`

## Intentionally Not Implemented Yet

- No AWS resources created.
- No source code modified.
- No deployment performed.
- No API Gateway implementation.
- No Lambda implementation.
- No S3 attachment bucket created.
- No SNS/SQS implementation.
- No CloudWatch/X-Ray configuration.

## Final Status

COMPLETE - ARCHITECTURE DESIGN ONLY
