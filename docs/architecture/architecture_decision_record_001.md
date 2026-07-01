# Architecture Decision Record 001

## Decision Title

Use separated frontend and backend architecture

## Context

PropCare Cloud is a cloud-based property maintenance and tenant service portal. The application needs a responsive user interface, role-based workflows, API-driven business logic, persistent relational data, and future integration with AWS services.

The assignment also expects a cloud-ready design that can be extended beyond a simple local web application. A clean separation between the client application, backend API, and database will make the solution easier to test, deploy, document, and expand in later tasks.

## Decision

PropCare Cloud will use a separated frontend and backend architecture:

```text
React frontend -> ASP.NET Core Web API -> EF Core -> Amazon RDS PostgreSQL
```

The frontend will be responsible for user interaction and client-side views. The backend API will handle business rules, validation, authentication and authorization logic, and data access. Entity Framework Core will manage database interaction with PostgreSQL hosted in Amazon RDS.

## Why Not Pure MVC

A pure MVC application can be effective for smaller server-rendered systems, but it couples the user interface and backend delivery more tightly. For this project, a separated architecture is a better fit because:

- React supports a richer tenant and maintenance workflow experience.
- A Web API can be reused by future clients or cloud services.
- Backend logic can evolve independently from frontend screens.
- AWS service integrations can be introduced without reshaping the entire application.
- The architecture demonstrates a clearer cloud application boundary for the DDAC assignment.

## Chosen Architecture

```text
React frontend
    -> ASP.NET Core Web API
        -> Entity Framework Core
            -> Amazon RDS PostgreSQL
```

## Future Task #2 Extension

The architecture is intended to support later AWS service integration:

```text
API Gateway + Lambda + S3 + SNS/SQS + CloudWatch/X-Ray
```

Possible extensions include API Gateway for exposing selected services, Lambda for background or event-driven processing, S3 for maintenance evidence uploads, SNS or SQS for notifications and work queues, and CloudWatch or X-Ray for observability.

## Consequences / Benefits

- Clear separation of frontend, backend, and database responsibilities.
- Easier local development and testing by layer.
- Better alignment with cloud deployment patterns.
- More flexible future integration with AWS services.
- Improved maintainability for role-based property management workflows.
- Stronger documentation trail for assignment evaluation.
