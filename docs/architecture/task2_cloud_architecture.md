# PropCare Cloud Task 2 Architecture

## 1. Purpose

Task 2 extends the completed Task 1 cloud application with serverless and event-driven AWS components. The existing S3 frontend, Elastic Beanstalk ASP.NET Core API, and Amazon RDS PostgreSQL database remain the core system, while Task 2 adds an attachment upload and notification architecture around it.

## 2. Task 1 Previous Architecture

Task 1 uses a three-tier cloud architecture:

- Existing Task 1 component: React and Vite frontend hosted by Amazon S3 static website hosting.
- Existing Task 1 component: ASP.NET Core Web API hosted on AWS Elastic Beanstalk.
- Existing Task 1 component: Entity Framework Core data access layer.
- Existing Task 1 component: Amazon RDS PostgreSQL database for application data.
- Existing Task 1 component: JWT authentication, backend role-based access control, and tenant data isolation.

The browser loads the frontend from S3, the frontend calls the Elastic Beanstalk API, the API enforces authentication and business rules, and EF Core persists data to RDS PostgreSQL.

## 3. Task 2 Proposed Architecture

Task 2 keeps the existing Task 1 core path active:

- Existing Task 1 component: User browser.
- Existing Task 1 component: React frontend on S3.
- Existing Task 1 component: Elastic Beanstalk ASP.NET Core API.
- Existing Task 1 component: Amazon RDS PostgreSQL.

Task 2 adds a serverless attachment microservice:

- New Task 2 component: Amazon API Gateway REST API.
- New Task 2 component: AWS Lambda presigned upload service.
- New Task 2 component: Private Amazon S3 attachment bucket.
- New Task 2 component: Amazon SNS topic for attachment upload events.
- New Task 2 component: Amazon SQS queue for durable event handling.
- New Task 2 component: Amazon CloudWatch metrics and logs.
- New Task 2 component: AWS X-Ray tracing for API Gateway and Lambda.

The existing backend remains the security boundary. It validates JWT identity, role permissions, maintenance request access, and tenant isolation before calling the serverless attachment service.

## 4. Architecture Change Summary

| Area | Task 1 | Task 2 Extension | Reason |
| --- | --- | --- | --- |
| Application architecture | S3 frontend, Elastic Beanstalk API, RDS PostgreSQL | Adds API Gateway and Lambda for attachment URL generation | Keeps the core app stable while adding focused serverless capability |
| File storage | Business data stored in RDS | Attachment objects stored in private S3 | Avoids storing large binary files in the relational database |
| Processing model | Server-based ASP.NET Core request handling | Event-driven upload and notification path | Improves scalability for file upload workflows |
| Communication | Frontend calls backend API | Backend calls API Gateway; S3 events publish to SNS and SQS | Decouples upload authorization from notification processing |
| Reliability | Elastic Beanstalk and RDS support the main workflow | SQS retains events if notification processing is delayed | Attachment events do not block the core maintenance workflow |
| Monitoring | Application logs and deployment health | CloudWatch metrics plus X-Ray traces for serverless flow | Improves visibility across the added AWS services |
| Security | JWT, RBAC, tenant isolation, RDS security groups | Private S3, short-lived presigned URLs, least-privilege Lambda IAM | Adds file upload capability without exposing AWS credentials |

## 5. Attachment Upload Data Flow

1. User selects a file for a maintenance request.
2. Frontend sends the upload request to the existing ASP.NET Core backend.
3. Backend validates JWT, role, request access, and tenant isolation.
4. Backend calls API Gateway.
5. API Gateway invokes Lambda.
6. Lambda generates a short-lived presigned S3 upload URL.
7. Frontend uploads directly to private S3.
8. Backend stores attachment metadata in RDS.
9. S3 publishes ObjectCreated event to SNS.
10. SNS sends notification and forwards the event to SQS.
11. CloudWatch and X-Ray collect monitoring information.

## 6. Security Design

- Existing backend remains the authentication and RBAC boundary.
- The frontend must never contain AWS credentials.
- S3 attachment bucket remains private.
- S3 Block Public Access remains enabled.
- Presigned URLs are short-lived.
- File type and size are validated before upload authorization.
- Lambda IAM role uses least privilege for presigned URL generation.
- API Gateway is not treated as the main user authentication system.
- API Gateway access credential or API key, if used, is stored only in Elastic Beanstalk environment configuration.
- Secrets are never committed.
- RDS is not accessed directly by Lambda.
- Existing tenant isolation remains enforced before generating an upload URL.
- S3 object keys should use maintenance request ID and a generated unique filename.
- No sensitive values should appear in architecture files, screenshots, or repository documentation.

## 7. Reliability and Availability

- Existing Task 1 application remains independent if the attachment service fails.
- Direct browser-to-S3 upload reduces load on Elastic Beanstalk.
- Lambda provides automatic scaling for the presigned URL service.
- SNS supports fan-out to multiple subscribers.
- SQS provides durable message retention and decouples event handling.
- Failed notification processing does not break the core maintenance request workflow.
- Existing RDS data remains the source of truth for business metadata.

## 8. Cost-Conscious Design

- Serverless services operate only when used.
- S3 stores attachment objects separately from RDS.
- Direct-to-S3 uploads avoid unnecessary backend bandwidth.
- Lambda is used for a small focused service instead of a second full backend.
- CloudWatch retention should be limited to an assignment-appropriate period.
- Test traffic will be controlled.
- No duplicate database or second full backend environment is required.

## 9. Monitoring Design

Elastic Beanstalk / EC2:

- CPU utilisation.
- Request health.
- Environment health.
- Logs.

API Gateway:

- Request count.
- Latency.
- 4XX and 5XX responses.

Lambda:

- Invocations.
- Duration.
- Errors.
- Execution logs.

S3:

- Object upload evidence.
- Event notification evidence.

SNS:

- Notification delivery metrics.

SQS:

- Available messages.
- Received and deleted messages.
- Message age.

X-Ray:

- API Gateway to Lambda trace.
- Service map.
- Latency and failure analysis.

## 10. Task 1 Protection

- Task 1 deployment remains operational.
- Existing S3 frontend remains unchanged.
- Existing Elastic Beanstalk backend remains unchanged during Sprint 16.
- Existing RDS data and migrations remain unchanged.
- Task 2 components are additive.
- All future code changes must pass Task 1 regression validation.

## 11. Sprint Mapping

| Sprint | Scope |
| --- | --- |
| Sprint 16 | Architecture Design |
| Sprint 17 | API Gateway + Lambda + S3 attachment service |
| Sprint 18 | SNS + SQS event notification pipeline |
| Sprint 19 | CloudWatch + X-Ray monitoring and performance analysis |
| Sprint 20 | Final Task 2 DOCX report |
