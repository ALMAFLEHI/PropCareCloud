# Sprint 17 - API Gateway, Lambda and S3 Integration

## Sprint Goal

- Add secure maintenance-request attachments without replacing the Task 1 application.
- Keep ASP.NET Core as the JWT, RBAC, request-access, and tenant-isolation boundary.
- Upload file bytes directly to private S3 and store metadata only in RDS PostgreSQL.

## Task 1 Baseline Protection

- Work remains on branch `task2`; `main` remains at Task 1 commit `4ca0a0d`.
- Annotated tag `task1-final` still dereferences to `4ca0a0d`.
- Baseline checks before deployment: backend health HTTP 200, database readiness HTTP 200 with `canConnect: true`, and frontend HTTP 200.
- Existing Task 1 untracked artifacts remain untouched and unstaged.

## Implemented Feature

- Authenticated users can authorize, upload, confirm, list, and download request attachments.
- Allowed files: JPEG, PNG, WebP, and PDF.
- Maximum size: 10 MB.
- Upload and download authorization expiry: five minutes.
- File bytes bypass Elastic Beanstalk and RDS.

## End-to-End Data Flow

- Frontend sends file metadata to the authenticated backend.
- Backend reuses existing maintenance-request role filtering before every attachment operation.
- Backend calls API Gateway with a backend-only service key.
- Lambda creates a constrained presigned POST for the private S3 bucket.
- Browser uploads directly to S3 and asks the backend to confirm.
- Lambda verifies object size, content type, encryption, and request-specific key prefix.
- Backend stores verified metadata in RDS.
- Authorized downloads use a new short-lived presigned GET.

## AWS Resources

- CloudFormation stack: `propcarecloud-task2-sprint17`, status `UPDATE_COMPLETE`.
- REST API: `propcarecloud-task2-attachments-api`, stage `prod`.
- Lambda: `propcarecloud-task2-presign-service`, Python 3.12.
- IAM role: `propcarecloud-task2-presign-role` with attachment-prefix-only S3 access and no RDS permission.
- Private bucket: `propcarecloud-task2-attachments-<account>-us-east-1` (account segment intentionally masked).
- Backend service key: `propcarecloud-task2-backend-service-key-v2`; value remains secret.
- Usage plan: `propcarecloud-task2-attachments-usage-plan`, rate 5 requests/second, burst 10, quota 1,000/day.
- Lambda log group: `/aws/lambda/propcarecloud-task2-presign-service`.
- Private encrypted deployment-artifact bucket created idempotently by the deployment script; account segment is not documented.

## Infrastructure as Code

- Template: `aws/task2/sprint17/infrastructure/template.yaml`.
- Deployment: `aws/task2/sprint17/deploy-sprint17.ps1`.
- Local/serverless tests: `aws/task2/sprint17/test-sprint17.ps1`.
- Scoped rollback: `aws/task2/sprint17/rollback-sprint17.ps1`.
- Rollback never targets Task 1 resources and retains the attachment bucket.

## Backend Changes

- Added typed API Gateway client with timeout and safe failure handling.
- Added request-scoped attachment service and controller.
- Added upload authorization, confirmation, listing, and download authorization endpoints.
- Added file policy validation and safe file-name normalization.
- Added empty Task 2 configuration keys to `appsettings.json`; live values use environment variables only.

## Frontend Changes

- Added a compact attachment panel to the existing request-detail page.
- Added client-side type, empty-file, and 10 MB validation.
- Added direct S3 upload progress, secure confirmation, refreshed listing, and download action.
- Existing visual system, routes, and unrelated screens remain unchanged.

## Database Changes

- Additive migration: `AddTask2AttachmentMetadata`.
- Adds `SizeBytes` to the existing attachment entity.
- Adds a unique index on private `StorageKey` to prevent duplicate confirmation.
- Migration `Up()` contains no table drop, column removal, or data reset.

## Security Controls

- S3 Block Public Access enabled; no public ACL, bucket policy, or website hosting.
- Bucket owner enforced and SSE-S3 encryption enabled.
- CORS limited to the deployed frontend and localhost development origins.
- API Gateway key required on all methods with usage-plan throttling.
- Service key is never sent to the browser, logged, documented, or committed.
- Lambda object access is limited to the Sprint 17 bucket request prefix; `s3:ListBucket` is constrained to `maintenance-requests/*` for accurate missing-object checks.
- Lambda has no RDS or Task 1 frontend-bucket permissions.
- No presigned URL is stored in RDS or source control.
- A deployment CLI formatting failure exposed existing environment values only in transient local execution output. The API Gateway key was replaced and the JWT signing key plus RDS password were rotated immediately; no exposed value was committed or documented.

## Authorization Rules

- Admin / Owner: attachments on any visible request.
- Property Manager: attachments within the existing manager request scope.
- Tenant: own requests and existing assigned-unit scope only.
- Maintenance Staff: assigned requests only.
- Unauthenticated requests return HTTP 401; authenticated out-of-scope requests return HTTP 403.

## Validation Rules

- File name required and path elements removed.
- Server-generated object key: `maintenance-requests/{requestId}/{guid}-{safeFileName}`.
- MIME type and matching extension required.
- Size range: 1 byte through 10 MB.
- Backend and Lambda independently validate metadata.
- Confirmation verifies S3 metadata before RDS persistence.

## Deployment Summary

- AWS CLI profile `fresh-propcare-task2-deployer` authenticated successfully in `us-east-1`.
- RDS snapshot `propcarecloud-pre-task2-sprint17-20260722-201649` was created and reached `available` before migration.
- CloudFormation stack deployed successfully and passed resource/security audit.
- Existing Elastic Beanstalk environment is Green/Ready on backend version `propcarecloud-api-sprint17-20260722-211908`; prior versions remain available.
- Migration `AddTask2AttachmentMetadata` applied successfully. Database readiness reports `canConnect: true`, zero pending migrations, and six applied migrations.
- Existing frontend S3 website was backed up and synchronized without `--delete`; index/error document settings were preserved and the landing page returns HTTP 200.
- Backend and frontend deployment packages were rebuilt successfully from the final source.

## Functional Test Results

- Backend upload authorization returned HTTP 200 and direct private-S3 upload returned HTTP 204.
- Backend confirmation returned HTTP 201 after Lambda `HeadObject` verification; metadata was persisted in RDS.
- Attachment listing returned the confirmed record.
- Short-lived secure download returned bytes matching the uploaded test image.
- Duplicate confirmation returned HTTP 409 and unsigned direct S3 access returned HTTP 403.
- Final backend validation passed all 104 tests; Lambda validation passed all 9 tests.
- Frontend production build and full-stack validation passed.

## Negative Test Results

- Missing API key: HTTP 403.
- Unsupported MIME type, declared oversize, zero-byte declaration, malformed body, and invalid maintenance request ID: HTTP 400.
- Unauthenticated backend attachment request: HTTP 401.
- Tenant cross-request and maintenance-staff unassigned-request access: HTTP 403.
- Missing-object verification: HTTP 404.
- Duplicate confirmation: HTTP 409; unsigned direct S3 object access: HTTP 403.

## Task 1 Regression Results

- Backend validation: passed, 104 total test cases.
- Frontend validation: passed.
- Full-stack local validation: passed.
- Live health HTTP 200 and RDS readiness HTTP 200 passed after deployment.
- Public landing, login, registration, and request-detail SPA shells remained available.
- Admin / Owner, Property Manager, Tenant, and Maintenance Staff login/scope checks passed.
- Existing RBAC and tenant/staff isolation restrictions passed.
- Existing data remained present: 7 users, 2 properties, 5 maintenance requests, and 6 tenant assignments.

## Evidence Checklist

- `sprint_17_stack_resources.png`: CloudFormation stack resources showing API Gateway, Lambda, private S3, IAM, API key, and usage plan without account or secret values.
- `sprint_17_api_gateway_lambda_success.png`: successful backend-mediated API Gateway/Lambda authorization result with URLs and keys masked.
- `sprint_17_private_s3_object.png`: uploaded object plus S3 Block Public Access and encryption evidence, with private identifiers masked.
- `sprint_17_live_attachment_upload.png`: live request-detail page showing a completed attachment upload.
- `sprint_17_attachment_download.png`: live attachment list and successful secure open/download result without showing the full signed URL.

## Known Limitations

- Sprint 17 validates declared metadata and S3 object metadata; content scanning is outside the assignment scope.
- A browser upload that succeeds but is never confirmed may require later operational cleanup.
- The S3 static website uses `index.html` as its error document; direct deep links deliver the SPA shell with HTTP 404, while client-side navigation works normally. A CDN rewrite is outside Sprint 17 scope.
- Evidence screenshots still require manual capture; Codex did not control the AWS browser or expose signed URLs.
- SNS/SQS notifications and custom CloudWatch/X-Ray monitoring remain deliberately deferred to Sprints 18 and 19.

## Files Changed

- `aws/task2/sprint17/**`.
- Backend attachment configuration, DTOs, service, controller, entity mapping, migration, and tests.
- Frontend request-detail page, API client, and API types.
- `docs/architecture/task2_cloud_architecture.md`, this Sprint document, and `README.md`.

## AWS Resources Created

- Additive stack `propcarecloud-task2-sprint17` created API Gateway, Lambda, a private S3 attachment bucket, scoped Lambda IAM role, API key, usage plan/key association, API methods/stage, Lambda permission, and Lambda log group.
- A private encrypted deployment-artifact bucket was created idempotently outside the stack for deployment packages.
- No SNS topic, SQS queue, custom CloudWatch dashboard, X-Ray configuration, second backend, second RDS database, or replacement Task 1 resource was created.

## Rollback Plan

- Previous Elastic Beanstalk application versions remain available; Task 1 version `propcarecloud-api-sprint14-registration-fix` is the known baseline rollback target.
- A local backup of the pre-Sprint 17 frontend website objects is retained under ignored deployment artifacts.
- RDS snapshot `propcarecloud-pre-task2-sprint17-20260722-201649` is available for disaster recovery; normal rollback should avoid data restoration unless strictly required.
- Roll back backend/frontend deployments if a Task 1 regression appears.
- Run the scoped rollback script only for stack `propcarecloud-task2-sprint17`.
- Preserve RDS data and retained attachment objects; never target Task 1 resources.

## Final Status

COMPLETE - LIVE END-TO-END VERIFIED

- Serverless stack, secure Elastic Beanstalk configuration, backend, additive RDS migration, and frontend are deployed.
- Upload authorization, direct private upload, verification, metadata persistence/listing, secure download, and required negative tests passed live.
- Task 1 remained operational and passed public-page, four-role, data-preservation, RBAC, and tenant-isolation regression checks.
- Five high-value screenshots listed above remain for manual evidence capture without secrets or complete presigned URLs.
