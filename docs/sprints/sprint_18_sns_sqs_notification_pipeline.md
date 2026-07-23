# Sprint 18 - SNS and SQS Notification Pipeline

## Sprint Objective

Add a non-blocking, asynchronous notification pipeline to the existing PropCare Cloud maintenance workflow without replacing Task 1 or Sprint 17 resources.

## Final Status

**COMPLETE - LIVE END-TO-END VERIFIED**

## Implemented Architecture

`ASP.NET Core backend -> API Gateway -> publisher Lambda -> encrypted SNS -> encrypted SQS -> processor Lambda -> structured runtime logs`

The primary SQS queue redrives repeatedly failed messages to an encrypted DLQ after three receives. Sprint 18 uses a separate additive CloudFormation stack named `propcarecloud-task2-sprint18`.

## Event Contract

- Schema version: `1.0`.
- Generated `eventId` and `correlationId`.
- Server-side UTC occurrence timestamp.
- Maintenance request and optional actor identifiers.
- Allowlisted target role and at most 20 target profile IDs.
- Title maximum 120 characters; message maximum 500 characters.
- Source fixed to `PropCareCloud.Api`.
- Payloads exclude passwords, tokens, keys, connection strings, addresses, and unnecessary personal data.

Implemented event types:

- `MaintenanceRequestCreated`
- `MaintenanceRequestAssigned`
- `MaintenanceRequestStatusChanged`
- `AttachmentConfirmed`

## AWS Resources

- REST API: `propcarecloud-task2-notifications-api`.
- Route: `POST /notifications/publish`, API key required.
- Publisher: `propcarecloud-task2-notification-publisher`, Python 3.12.
- SNS topic: `propcarecloud-task2-notifications`, encrypted.
- Primary queue: `propcarecloud-task2-notification-events`, encrypted, four-day retention.
- DLQ: `propcarecloud-task2-notification-events-dlq`, encrypted.
- Processor: `propcarecloud-task2-notification-processor`, Python 3.12.
- Event mapping: Enabled, batch size 5, `ReportBatchItemFailures` enabled.
- Runtime log retention: 14 days.
- Usage plan: rate 5/second, burst 10, quota 1,000/day.

## Backend Integration

- A typed backend-only publisher calls API Gateway after the database or attachment metadata commit.
- Elastic Beanstalk stores the API base URL, secret service key, and three-second timeout as environment settings.
- Request creation always emits after successful persistence.
- Assignment and status events emit only when the value actually changes.
- Attachment events emit after S3 verification and metadata persistence.
- HTTP 202 maps to `notificationQueued: true`.
- Timeout, configuration, HTTP, or transport failure maps to `notificationQueued: false` without rolling back the successful business operation.

## Frontend Feedback

The existing success-banner pattern displays the backend notification message after creation, assignment, status, and attachment actions. The browser continues to call only the ASP.NET backend and contains no notification API key.

## Security Controls

- Backend-only API key and usage-plan throttling.
- Publisher IAM permits only `sns:Publish` on the Sprint 18 topic plus scoped runtime logging.
- Processor IAM permits only receive/delete/attribute access on the primary SQS queue plus scoped runtime logging.
- Neither Lambda has RDS or S3 permission; the processor has no SNS publish permission.
- Queue policy permits SNS delivery only from the Sprint 18 topic and denies insecure transport.
- SNS, SQS, and DLQ encryption enabled.
- No migration or new database table was required.
- No secret, account ID, full ARN, signed URL, credential, token, or connection string is documented or committed.

## Validation Results

- Backend restore/build/test: PASS, 117 tests.
- Frontend install/build: PASS, zero reported vulnerabilities.
- Full-stack local validation: PASS.
- Publisher Lambda tests: PASS, 9.
- Processor Lambda tests: PASS, 6.
- Sprint 17 Lambda regression tests: PASS, 9.
- CloudFormation template validation: PASS.
- Fresh backend Release package: PASS; Sprint 18 symbols confirmed in the compiled DLL.
- Frontend production package: PASS; notification feedback binding present and backend key markers absent.

## Live Deployment Results

- CloudFormation stack: `CREATE_COMPLETE`, 22 resources.
- Publisher and processor Lambdas: Active.
- SNS-to-SQS subscription: confirmed, raw delivery enabled.
- Primary queue and DLQ encryption: enabled.
- Redrive policy: `maxReceiveCount` 3.
- Live status update: HTTP 200, notification queued.
- Live request creation: HTTP 201, notification queued; controlled record removed afterward.
- Live attachment confirmation: HTTP 201, notification queued.
- Direct valid API Gateway publish: HTTP 202.
- Publisher and processor structured records matched event and correlation IDs.
- Primary queue after processing: zero visible and zero in flight.
- DLQ after processing: zero visible and zero in flight.
- Elastic Beanstalk: Green/Ready after deployment.
- Frontend S3 website, backend health, and database readiness: HTTP 200.
- Database readiness: connected, zero pending migrations, six applied migrations.

## Negative Test Results

| Test | Result |
| --- | --- |
| API request without key | HTTP 403 |
| Malformed JSON | HTTP 400 |
| Unknown event type | HTTP 400 |
| Invalid event ID | HTTP 400 |
| Invalid maintenance request ID | HTTP 400 |
| Oversized title | HTTP 400 |
| Oversized message | HTTP 400 |
| More than 20 targets | HTTP 400 |
| Unauthenticated maintenance endpoint | HTTP 401 |
| Tenant cross-request access | HTTP 403 |
| Unassigned staff request access | HTTP 403 |
| Unchanged assignment | HTTP 200, no event published |
| Unchanged status | HTTP 200, no event published |
| Publisher timeout/failure | Business record persists; non-breaking warning returned |

## Regression Results

- Admin / Owner identity, user management, properties, requests, and tenant registrations: PASS.
- Property Manager properties, requests, registration review, assignment, and status flows: PASS.
- Tenant request scope, assigned units, and cross-tenant isolation: PASS.
- Maintenance Staff assigned queue, progress update, and unrelated-request restriction: PASS.
- JWT authentication, RBAC, health, RDS readiness, and six migrations: PASS.
- Sprint 17 authorization, private upload, confirmation, metadata listing, secure download, and unsigned-access denial: PASS.
- Frontend public routes and authenticated SPA routes remain present.
- Task 1 `main` and annotated `task1-final` remain unchanged at the protected Task 1 commit.

## Rollback

1. Roll the Elastic Beanstalk environment back to the retained Sprint 17 application version.
2. Restore the retained local frontend backup to the existing S3 website if required.
3. Run `aws/task2/sprint18/rollback-sprint18.ps1 -ConfirmRemoval` only for a deliberate Sprint 18 stack removal.
4. The rollback script refuses any stack name other than `propcarecloud-task2-sprint18`.
5. Task 1, RDS, the frontend bucket, and the Sprint 17 stack are never rollback targets.

## Genuine Limitations

- The processor currently records safe successful processing rather than delivering email, SMS, WebSocket, or an in-app notification center.
- The live frontend feedback still requires a manually captured browser screenshot.
- Standard Lambda runtime logs are used only as Sprint 18 evidence; dashboards, alarms, and X-Ray belong to Sprint 19.
- Evidence screenshots must be captured manually and must mask account IDs, complete ARNs, keys, tokens, and sensitive bodies.

## Manual Evidence Required

1. `sprint_18_stack_resources.png`
   - AWS Console: CloudFormation -> Stacks -> `propcarecloud-task2-sprint18` -> Resources.
   - Show successful status and the API, Lambdas, SNS, SQS, DLQ, mapping, and IAM resources.
2. `sprint_18_sns_sqs_subscription.png`
   - AWS Console: SNS -> Topics -> Sprint 18 topic -> Subscriptions.
   - Show confirmed SQS subscription and, where practical, the topic-restricted queue policy.
3. `sprint_18_sqs_dlq_redrive.png`
   - AWS Console: SQS -> `propcarecloud-task2-notification-events` -> Dead-letter queue / Redrive policy.
   - Show the DLQ, `maxReceiveCount` 3, and encryption enabled.
4. `sprint_18_lambda_processing_success.png`
   - AWS Console: Lambda -> `propcarecloud-task2-notification-processor` -> Monitor -> CloudWatch logs -> latest successful stream.
   - Show safe event/correlation IDs and successful processing without secrets.
5. `sprint_18_live_notification_queued.png`
   - Live PropCare Cloud request page after an assignment or status action.
   - Show successful operation and “Notification queued” feedback with the existing UI preserved.

Do not automate these screenshots. Mask account IDs, full ARNs, API key values, tokens, signed URLs, and sensitive message content.

## Sprint Boundary

Sprint 19 was not implemented. No X-Ray, custom CloudWatch dashboard or alarm, email/SMS subscription, WebSocket notification, DynamoDB, EventBridge, SES, or Step Functions resource was added.
