# Sprint 19 - CloudWatch, X-Ray, and Minimal Notification Inbox

## Sprint Objective

Add production-style monitoring to the existing Task 2 serverless services and a small authenticated in-app notification inbox without replacing Task 1, Sprint 17, or Sprint 18 resources.

## Final Status

**COMPLETE - LIVE END-TO-END VERIFIED**

## Implemented Scope

- Separate additive monitoring stack: `propcarecloud-task2-sprint19`.
- CloudWatch dashboard: `propcarecloud-task2-monitoring`.
- Nine focused CloudWatch alarms with no destructive actions or user subscriptions.
- Logs Insights widgets for safe publisher and processor correlation records.
- Native active X-Ray tracing for the Sprint 17 and Sprint 18 API Gateway `prod` stages and their three Lambda functions.
- AWS-required X-Ray telemetry actions only: `xray:PutTraceSegments` and `xray:PutTelemetryRecords`, scoped to `Resource "*"`.
- Structured serverless logs with safe operation, status, event, correlation, Lambda request, and X-Ray trace identifiers.
- Additive `UserNotification` persistence in RDS PostgreSQL.
- Authenticated current-user-only notification APIs.
- Compact top-bar notification bell, unread badge, recent-notification panel, refresh, mark-read, mark-all-read, and 45-second polling.

## Notification Inbox Architecture

`Business operation -> one Sprint 18 event -> UserNotification rows in RDS -> Sprint 18 API Gateway -> publisher Lambda -> SNS -> SQS -> processor Lambda`

The same event and correlation identifiers are used for inbox persistence and asynchronous publishing. Notification persistence and publishing happen after the business operation is committed. Either notification step may fail safely without rolling back the maintenance operation.

## Database Model and Migration

Migration: `AddUserNotifications`

The additive table stores:

- recipient `UserProfileId`
- `EventId` and `CorrelationId`
- bounded `EventType`, `Title`, and `Message`
- optional `MaintenanceRequestId`
- read status and UTC timestamps

Indexes:

- unique `(UserProfileId, EventId)` to prevent duplicate recipient rows
- `(UserProfileId, IsRead, CreatedAtUtc)` for current-user inbox queries

Both foreign keys use restricted delete behavior. The migration `Up` method contains no table/column drop, seed deletion, destructive SQL, reset, or data deletion.

## Recipient Rules

| Event | Recipients |
| --- | --- |
| `MaintenanceRequestCreated` | Tenant acknowledgement |
| `MaintenanceRequestAssigned` | Tenant and newly assigned maintenance staff |
| `MaintenanceRequestStatusChanged` | Tenant and assigned staff, excluding a redundant actor |
| `AttachmentConfirmed` | Tenant and assigned staff, excluding a redundant actor |

Recipient IDs are de-duplicated. Missing or inactive profiles are skipped safely.

## Notification APIs

- `GET /api/notifications?limit=20&unreadOnly=false`
- `GET /api/notifications/unread-count`
- `PATCH /api/notifications/{id}/read`
- `PATCH /api/notifications/read-all`

All routes require an authenticated portal role. The backend derives the profile ID from the JWT context; no endpoint accepts a target user ID. A notification belonging to another user is indistinguishable from a missing record and returns HTTP 404 for read operations.

## Frontend Behavior

- Bell appears only in the authenticated top bar.
- Badge is hidden at zero and capped visually at `99+`.
- Panel shows the latest 15 notifications, newest first.
- Unread rows are visually distinct.
- Opening the panel refreshes immediately.
- Background polling runs every 45 seconds and is cleaned up when the authenticated top bar unmounts.
- Selecting a request notification marks it read and navigates to `/requests/{maintenanceRequestId}`.
- Refresh and mark-all-read actions use existing API and authentication utilities.
- No notification page, sidebar item, WebSocket, push notification, email, or SMS feature was added.

## CloudWatch Dashboard

The dashboard contains concise widgets for:

- Sprint 17 presign Lambda and Sprint 18 publisher/processor Lambda invocations, errors, and duration
- Sprint 17 and Sprint 18 API Gateway count, 4XX, 5XX, and latency
- SNS published and failed notifications
- SQS visible, oldest, received, deleted, and DLQ-visible metrics
- Elastic Beanstalk environment health
- RDS CPU utilization, connections, and free storage
- publisher and processor Logs Insights correlation tables

## CloudWatch Alarms

- Sprint 17 Lambda errors
- Sprint 18 publisher Lambda errors
- Sprint 18 processor Lambda errors
- Sprint 17 API Gateway 5XX
- Sprint 18 API Gateway 5XX
- notification queue oldest-message age
- notification DLQ visible messages
- Elastic Beanstalk degraded health
- RDS high CPU

All alarms treat missing data as non-breaching and have no automatic remediation, email, or SMS action.

## X-Ray and Logging Security

- API Gateway and Lambda native active tracing is used; no fragile SDK dependency was added.
- Logs contain safe identifiers and status fields only.
- Logs and traces must not include request authorization headers, API keys, passwords, connection strings, notification message bodies, addresses, or presigned URLs.
- Lambda roles have no RDS access.
- No API key or monitoring identifier is exposed to the frontend.
- Existing Lambda log groups retain logs for 14 days.

## Validation Commands

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-backend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-frontend.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\check-fullstack-local.ps1
powershell -ExecutionPolicy Bypass -File .\aws\task2\sprint17\test-sprint17.ps1
powershell -ExecutionPolicy Bypass -File .\aws\task2\sprint18\test-sprint18.ps1
powershell -ExecutionPolicy Bypass -File .\aws\task2\sprint19\test-sprint19.ps1 -Profile fresh-propcare-task2-deployer -Region us-east-1
```

## Deployment and Migration Safety

- Create an RDS snapshot beginning `propcarecloud-pre-task2-sprint19-` before migration.
- Verify readiness shows one pending and six applied migrations.
- Apply only `AddUserNotifications`.
- Remove any exact temporary `/32` PostgreSQL security-group rule immediately.
- Verify zero pending and seven applied migrations with existing records intact.
- Update Sprint 17/18 stacks in place only for tracing, X-Ray telemetry permission, and safe log changes.
- Deploy the dashboard and alarms through the separate Sprint 19 stack.
- Preserve the previous Elastic Beanstalk application version and a local frontend S3 backup.

## Rollback Readiness

- `rollback-sprint19.ps1` refuses any stack name except `propcarecloud-task2-sprint19`.
- It removes only the Sprint 19 dashboard and alarms after explicit confirmation.
- Shared Sprint 17/18 tracing remains enabled because it is non-destructive operational configuration.
- Backend and frontend deployments retain their previous versions/backups.
- The additive notification table should normally remain in place; database snapshot restoration is a disaster-recovery action, not a routine rollback.

## Final Live Validation

- RDS snapshot `propcarecloud-pre-task2-sprint19-20260723-172338` was available before migration.
- The additive `AddUserNotifications` migration was applied successfully.
- Database readiness returned HTTP 200, `canConnect: true`, zero pending migrations, and seven applied migrations.
- Existing Task 1 users, properties, requests, assignments, and live RDS data remained available.
- The backend deployment remained Green/Ready and the frontend website returned HTTP 200.
- Current-user notification list, unread count, mark-read, and mark-all-read operations passed.
- Cross-user notification access returned HTTP 404; unauthenticated and invalid requests returned safe 4xx responses.
- Assignment, status, and attachment events created the expected recipient notifications without duplicate rows.
- The live frontend displayed the notification bell, unread badge, and recent-notification panel.
- Sprint 17 attachment upload and Sprint 18 asynchronous processing regression checks passed.
- The Sprint 19 stack reached `UPDATE_COMPLETE` with 11 dashboard widgets and nine alarms.
- All nine alarms reported `OK`; none reported `ALARM` or insufficient data.
- Active tracing was confirmed for both API Gateway stages and all three Task 2 Lambda functions.
- X-Ray returned successful Sprint 17 and Sprint 18 traces plus API Gateway-to-Lambda service relationships.
- Publisher and processor structured logs matched on event and correlation identifiers.
- The primary notification queue and dead-letter queue were empty after controlled testing.
- Backend validation passed with 127 tests; frontend install/build passed with zero reported vulnerabilities.

## Manual Evidence Required

Do not automate these screenshots. Mask account IDs, complete ARNs, trace IDs where unnecessary, API keys, tokens, passwords, connection strings, and personal data.

1. `sprint_19_cloudwatch_dashboard.png`
   - CloudWatch -> Dashboards -> `propcarecloud-task2-monitoring`.
   - Show readable Lambda, API Gateway, SNS/SQS, and DLQ widgets.
2. `sprint_19_cloudwatch_alarms.png`
   - CloudWatch -> Alarms -> All alarms.
   - Show Lambda, API 5XX, queue-age, and DLQ alarms in healthy states.
3. `sprint_19_xray_service_map.png`
   - CloudWatch -> X-Ray traces -> Service map.
   - Show successful API Gateway-to-Lambda relationships.
4. `sprint_19_xray_trace_detail.png`
   - X-Ray -> Traces -> select one successful Sprint 18 trace.
   - Show the successful API Gateway/Lambda timeline with sensitive values hidden.
5. `sprint_19_notification_inbox.png`
   - Live PropCare Cloud as a target tenant or maintenance staff user.
   - Show the bell badge and open panel with a safe relevant notification.

No screenshot placeholders are committed.

## Genuine Limitations

- The inbox uses modest polling rather than WebSockets or push delivery.
- Native API Gateway/Lambda X-Ray tracing is used; explicit SNS subsegments depend on AWS native service instrumentation.
- CloudWatch metrics can take several minutes to populate after controlled traffic.
- Manual evidence screenshots remain a user capture step so AWS account details and trace identifiers can be reviewed and masked before commit.
