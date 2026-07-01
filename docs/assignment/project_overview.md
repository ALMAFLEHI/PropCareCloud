# PropCare Cloud Project Overview

## Project Title

PropCare Cloud: Cloud-Based Property Maintenance and Tenant Service Portal

## Problem Statement

Property maintenance communication is often fragmented across phone calls, messaging apps, spreadsheets, and informal notes. This makes it difficult for property managers and owners to track requests, assign staff, monitor progress, and give tenants reliable updates.

## Proposed Solution

PropCare Cloud will provide a centralized web portal where tenants can submit maintenance requests, property managers can review and assign work, maintenance staff can update job progress, and owners or administrators can monitor overall property service activity.

## Target Users

The portal is intended for property management companies, landlords, building owners, tenants, and maintenance teams who need a clearer workflow for property service requests.

## User Roles

1. Admin / Owner
   - Oversees system users, properties, reports, and cloud-level monitoring.
2. Property Manager
   - Reviews tenant requests, manages property records, assigns maintenance work, and tracks service outcomes.
3. Tenant
   - Creates maintenance requests, attaches supporting information, and views request progress.
4. Maintenance Staff
   - Receives assigned jobs, updates repair status, and records completion notes.

## Task #1 Expected Direction

Task #1 is expected to focus on the core cloud application design and implementation. The planned direction is a React frontend connected to an ASP.NET Core Web API backend, using Entity Framework Core with Amazon RDS PostgreSQL for persistent property, tenant, maintenance request, and user-role data.

## Task #2 Expected Direction

Task #2 is expected to extend the application with additional AWS cloud services. Planned extensions include API Gateway, Lambda, S3, SNS or SQS, and CloudWatch or X-Ray to support scalable integration, file storage, notifications, queue-based workflows, and operational monitoring.
