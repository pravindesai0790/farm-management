# Farm Management Platform – Project Specification

**Document:** `SPEC.md`  
**Version:** 1.0  
**Status:** Active  
**Primary implementation:** Web application first  
**Future client:** React Native mobile application  
**Primary focus:** General crop management with advanced grape farming capabilities

---

# 1. Project Overview

## 1.1 Product Name

**Farm Management Platform**

Internal solution name:

```text
FarmManagement
```

---

## 1.2 Product Vision

Build a modern farm management platform that supports the registration, planning, tracking, and maintenance of farming activities for multiple crop types.

The system must be designed as a **general-purpose crop management platform**, while providing deeper and specialized functionality for **grape farming**.

The architecture must allow future crop-specific modules without redesigning the core application.

Examples of supported crops:

- Grapes
- Mango
- Pomegranate
- Tomato
- Banana
- Onion
- Sugarcane
- Other future crops

---

## 1.3 Primary Goals

The platform must allow users to:

1. Register and manage farms.
2. Register fields or plots within farms.
3. Register crop master data.
4. Register crop varieties.
5. Create crop cycles or growing seasons.
6. Plan farming activities.
7. Assign activities to farm workers.
8. Track activity status and completion.
9. Record materials used.
10. Record labour involved.
11. Record activity costs.
12. Upload activity photos and documents in future phases.
13. View farm and crop dashboards.
14. Generate reports.
15. Provide advanced grape farming workflows.
16. Expose APIs that can later be consumed by a React Native mobile application.

---

# 2. Product Principles

The following principles are mandatory.

## 2.1 Crop-Agnostic Core

The core domain must not assume that every farm grows grapes.

Incorrect design:

```text
Farm
├── GrapeVariety
├── VineCount
├── PruningType
└── RowSpacing
```

Correct design:

```text
Farm
└── Field
    └── CropCycle
        └── Crop
```

Crop-specific information must be implemented separately.

---

## 2.2 Grape-Specific Extensions

Grape farming will be the first specialized crop module.

Architecture:

```text
General Farm Platform
│
├── Farm Management
├── Crop Management
├── Activity Management
│
└── Crop-Specific Modules
    │
    ├── Grapes
    │
    ├── Mango (Future)
    │
    └── Other Crops (Future)
```

---

## 2.3 API-First Design

The backend API must support:

```text
Angular Web Application
        │
        ▼
ASP.NET Core API
        │
        ▼
PostgreSQL
```

Future:

```text
Angular Web Application

React Native Mobile Application

External Integrations

          │
          ▼

ASP.NET Core API
```

Business logic must remain in the backend and must not be duplicated in frontend applications.

---

# 3. Technology Stack

## 3.1 Frontend

```text
Angular
TypeScript
Angular Material
Angular Router
Angular Signals
RxJS
Reactive Forms
SCSS
```

The Angular application must use standalone components unless a future architectural requirement justifies NgModules.

---

## 3.2 Backend

```text
ASP.NET Core Web API
C#
Clean Architecture
Entity Framework Core
PostgreSQL
Npgsql
```

Supporting tools:

```text
OpenAPI / Swagger
Health Checks
Serilog
FluentValidation
```

FluentValidation will be introduced when application use cases are implemented.

---

## 3.3 Database

```text
PostgreSQL
```

Entity Framework Core migrations are the official database schema management mechanism.

Manual production schema changes must be avoided.

---

## 3.4 Infrastructure

```text
Docker
Docker Compose
```

Phase 0 services:

```text
PostgreSQL
ASP.NET Core API
Angular Web Application
```

---

## 3.5 Testing

Backend:

```text
xUnit
FluentAssertions
NSubstitute
Testcontainers PostgreSQL
```

Frontend:

```text
Angular default test runner
Playwright for future end-to-end testing
```

---

# 4. Repository Structure

The project uses a monorepo.

```text
farm-management/
│
├── backend/
│
├── frontend/
│
├── docker/
│
├── docs/
│
├── scripts/
│
├── .env
│
├── .env.example
│
├── .gitignore
│
├── docker-compose.yml
│
├── README.md
│
└── SPEC.md
```

---

# 5. Backend Architecture

## 5.1 Solution

```text
backend/FarmManagement.sln
```

Projects:

```text
src/

FarmManagement.API
FarmManagement.Application
FarmManagement.Domain
FarmManagement.Infrastructure
```

Tests:

```text
tests/

FarmManagement.UnitTests
FarmManagement.IntegrationTests
```

---

# 6. Backend Folder Structure

```text
backend/

├── FarmManagement.sln
│
├── src/
│
│   ├── FarmManagement.API/
│   │   │
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   ├── Configuration/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Dockerfile
│   │
│   ├── FarmManagement.Application/
│   │   │
│   │   ├── Common/
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   ├── Features/
│   │   └── Validators/
│   │
│   ├── FarmManagement.Domain/
│   │   │
│   │   ├── Common/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Events/
│   │   └── ValueObjects/
│   │
│   └── FarmManagement.Infrastructure/
│       │
│       ├── Persistence/
│       │   ├── Configurations/
│       │   ├── Migrations/
│       │   └── ApplicationDbContext.cs
│       │
│       ├── Repositories/
│       └── Services/
│
└── tests/
    │
    ├── FarmManagement.UnitTests/
    │
    └── FarmManagement.IntegrationTests/
```

---

# 7. Clean Architecture Dependency Rules

Dependencies must follow:

```text
API
 │
 ▼
Application
 │
 ▼
Domain
```

Infrastructure may depend on:

```text
Application
Domain
```

API may depend on:

```text
Application
Infrastructure
```

Domain must not depend on:

```text
API
Infrastructure
Entity Framework Core
ASP.NET Core
PostgreSQL
```

---

# 8. Phase 0 Scope

Phase 0 builds the technical foundation.

No major farm business features are implemented in this phase.

## Included

- Solution creation
- Angular application
- Docker setup
- PostgreSQL
- EF Core connection
- Initial migration
- OpenAPI/Swagger
- Health checks
- Logging
- Global exception handling
- API connectivity test
- Angular application layout
- Placeholder routes
- Testing project setup

---

## Not Included

The following must not be implemented in Phase 0:

- Authentication
- Users
- Roles
- Permissions
- Farm CRUD
- Field CRUD
- Crop CRUD
- Crop cycles
- Activity management
- Inventory
- Labour
- Expenses
- Harvest
- Grape-specific workflows

---

# 9. Backend Project Creation Commands

From the repository root:

```bash
mkdir backend
cd backend

dotnet new sln -n FarmManagement

mkdir src
mkdir tests
```

Create projects:

```bash
dotnet new webapi -n FarmManagement.API -o src/FarmManagement.API

dotnet new classlib -n FarmManagement.Application -o src/FarmManagement.Application

dotnet new classlib -n FarmManagement.Domain -o src/FarmManagement.Domain

dotnet new classlib -n FarmManagement.Infrastructure -o src/FarmManagement.Infrastructure
```

Create tests:

```bash
dotnet new xunit -n FarmManagement.UnitTests -o tests/FarmManagement.UnitTests

dotnet new xunit -n FarmManagement.IntegrationTests -o tests/FarmManagement.IntegrationTests
```

Add projects:

```bash
dotnet sln add src/FarmManagement.API/FarmManagement.API.csproj

dotnet sln add src/FarmManagement.Application/FarmManagement.Application.csproj

dotnet sln add src/FarmManagement.Domain/FarmManagement.Domain.csproj

dotnet sln add src/FarmManagement.Infrastructure/FarmManagement.Infrastructure.csproj

dotnet sln add tests/FarmManagement.UnitTests/FarmManagement.UnitTests.csproj

dotnet sln add tests/FarmManagement.IntegrationTests/FarmManagement.IntegrationTests.csproj
```

---

# 10. Project References

Application → Domain:

```bash
dotnet add src/FarmManagement.Application/FarmManagement.Application.csproj reference src/FarmManagement.Domain/FarmManagement.Domain.csproj
```

Infrastructure → Application:

```bash
dotnet add src/FarmManagement.Infrastructure/FarmManagement.Infrastructure.csproj reference src/FarmManagement.Application/FarmManagement.Application.csproj
```

Infrastructure → Domain:

```bash
dotnet add src/FarmManagement.Infrastructure/FarmManagement.Infrastructure.csproj reference src/FarmManagement.Domain/FarmManagement.Domain.csproj
```

API → Application:

```bash
dotnet add src/FarmManagement.API/FarmManagement.API.csproj reference src/FarmManagement.Application/FarmManagement.Application.csproj
```

API → Infrastructure:

```bash
dotnet add src/FarmManagement.API/FarmManagement.API.csproj reference src/FarmManagement.Infrastructure/FarmManagement.Infrastructure.csproj
```

Unit Tests → Application:

```bash
dotnet add tests/FarmManagement.UnitTests/FarmManagement.UnitTests.csproj reference src/FarmManagement.Application/FarmManagement.Application.csproj
```

Integration Tests → API:

```bash
dotnet add tests/FarmManagement.IntegrationTests/FarmManagement.IntegrationTests.csproj reference src/FarmManagement.API/FarmManagement.API.csproj
```

---

# 11. Required NuGet Packages

## Infrastructure

```bash
dotnet add src/FarmManagement.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL

dotnet add src/FarmManagement.Infrastructure package Microsoft.EntityFrameworkCore.Design
```

---

## API

```bash
dotnet add src/FarmManagement.API package AspNetCore.HealthChecks.NpgSql

dotnet add src/FarmManagement.API package Serilog.AspNetCore

dotnet add src/FarmManagement.API package Serilog.Sinks.Console
```

Use the OpenAPI support supplied by the selected .NET SDK/template where possible. Avoid adding duplicate Swagger/OpenAPI packages unnecessarily.

---

## Unit Tests

```bash
dotnet add tests/FarmManagement.UnitTests package FluentAssertions

dotnet add tests/FarmManagement.UnitTests package NSubstitute
```

---

## Integration Tests

Later:

```text
Testcontainers.PostgreSql
Microsoft.AspNetCore.Mvc.Testing
```

---

# 12. Database Configuration

Database:

```text
farm_management
```

Docker user:

```text
farm_user
```

Phase 0 database connectivity must be verified using a small technical entity.

Entity:

```text
SystemSetting
```

Fields:

```text
Id
Key
Value
CreatedAt
UpdatedAt
```

This entity is used only to verify:

```text
EF Core
↓
Migration
↓
PostgreSQL
```

---

# 13. Database Naming Standards

Use:

```text
PascalCase
```

in C#.

Database naming should use:

```text
snake_case
```

Example:

```text
SystemSetting
```

becomes:

```text
system_settings
```

Fields:

```text
CreatedAt
```

becomes:

```text
created_at
```

Configure this consistently in EF Core.

---

# 14. Future Core Domain Model

The future farm hierarchy is:

```text
Organization
      │
      ▼
Farm
      │
      ▼
Field
      │
      ▼
CropCycle
      │
      ▼
Crop
```

Example:

```text
Farm: Main Farm

Field A
│
└── Crop Cycle 2026
    │
    └── Grapes

Field B
│
└── Crop Cycle 2026
    │
    └── Tomato
```

---

# 15. Crop-Specific Architecture

General entities must not contain grape-specific fields.

Use:

```text
CropCycle
│
├── General crop information
│
└── Crop-specific profile
```

Future:

```text
CropCycle
│
├── GrapeProfile
├── MangoProfile
└── Other Crop Profiles
```

---

# 16. Docker Architecture

Services:

```text
postgres
api
web
```

Architecture:

```text
Browser
   │
   ▼
Angular Web
Port 4200
   │
   ▼
ASP.NET Core API
Port 8080
   │
   ▼
PostgreSQL
Port 5432
```

Docker internal communication:

```text
web → api

api → postgres
```

Do not use `localhost` between containers.

---

# 17. Environment Variables

Create `.env`.

```env
POSTGRES_DB=farm_management
POSTGRES_USER=farm_user
POSTGRES_PASSWORD=change_this_password

API_PORT=8080
WEB_PORT=4200
```

Create `.env.example`.

```env
POSTGRES_DB=farm_management
POSTGRES_USER=farm_user
POSTGRES_PASSWORD=your_password_here

API_PORT=8080
WEB_PORT=4200
```

`.env` must be included in `.gitignore`.

---

# 18. Docker Compose Specification

`docker-compose.yml` must provide:

```yaml
services:

  postgres:
    image: postgres:16

    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}

    ports:
      - "5432:5432"

    volumes:
      - postgres_data:/var/lib/postgresql/data

    healthcheck:
      test:
        [
          "CMD-SHELL",
          "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"
        ]

      interval: 10s
      timeout: 5s
      retries: 5


  api:
    build:
      context: ./backend
      dockerfile: src/FarmManagement.API/Dockerfile

    depends_on:
      postgres:
        condition: service_healthy

    environment:

      ASPNETCORE_ENVIRONMENT: Development

      ASPNETCORE_URLS: http://+:8080

      ConnectionStrings__DefaultConnection: >
        Host=postgres;
        Port=5432;
        Database=${POSTGRES_DB};
        Username=${POSTGRES_USER};
        Password=${POSTGRES_PASSWORD}

    ports:
      - "${API_PORT}:8080"


  web:
    build:
      context: ./frontend/farm-management-web
      dockerfile: Dockerfile

    depends_on:
      - api

    ports:
      - "${WEB_PORT}:80"


volumes:

  postgres_data:
```

---

# 19. API Requirements

Phase 0 must provide:

## Health Endpoint

```text
GET /health
```

Expected:

```text
Healthy
```

The health check must validate PostgreSQL connectivity.

---

## Ping Endpoint

```text
GET /api/system/ping
```

Example response:

```json
{
  "message": "Farm Management API is running",
  "timestamp": "UTC_TIMESTAMP"
}
```

---

## OpenAPI

The API documentation endpoint must be available during development.

Target:

```text
/swagger
```

or the equivalent OpenAPI UI generated by the selected .NET version.

---

# 20. Logging

Use Serilog.

Minimum development logging:

```text
Console
```

Log:

```text
Application startup
HTTP requests
Unhandled exceptions
Database connection failures
```

Do not log:

```text
Passwords
Tokens
Sensitive user data
Connection strings
```

---

# 21. Exception Handling

Implement centralized global exception handling.

Unexpected errors must:

1. Be logged.
2. Return a safe response.
3. Not expose stack traces in production.

Example response:

```json
{
  "success": false,
  "message": "An unexpected error occurred",
  "traceId": "TRACE_ID"
}
```

---

# 22. Angular Application Setup

Create:

```bash
mkdir frontend
cd frontend

ng new farm-management-web
```

Recommended:

```text
Routing: Yes
Stylesheet: SCSS
SSR: No
```

Use standalone Angular architecture.

---

# 23. Angular Material

Install:

```bash
cd frontend/farm-management-web

ng add @angular/material
```

Phase 0 components:

```text
Toolbar
Sidenav
Buttons
Icons
Cards
SnackBar
Progress Spinner
```

More components will be introduced only when required.

---

# 24. Angular Folder Structure

```text
src/app/

├── core/
│
│   ├── auth/
│   ├── guards/
│   ├── interceptors/
│   ├── models/
│   └── services/
│
├── shared/
│
│   ├── components/
│   ├── directives/
│   ├── pipes/
│   └── utils/
│
├── features/
│
│   ├── dashboard/
│   ├── farms/
│   ├── fields/
│   ├── crops/
│   ├── crop-cycles/
│   ├── activities/
│   └── administration/
│
├── layouts/
│
│   ├── main-layout/
│   └── auth-layout/
│
├── app.config.ts
├── app.routes.ts
└── app.component.ts
```

Phase 0 pages may be placeholders.

---

# 25. Angular Routes

Initial routes:

```text
/
dashboard

/farms
farms

/crops
crops

/activities
activities

/settings
settings
```

Authentication routes are not required in Phase 0.

---

# 26. Main Layout

The Angular application must have:

```text
┌─────────────────────────────────────────────┐
│ Farm Management Platform          Profile   │
├────────────────┬────────────────────────────┤
│ Dashboard      │                            │
│ Farms          │                            │
│ Crops          │       Router Outlet        │
│ Activities     │                            │
│ Settings       │                            │
└────────────────┴────────────────────────────┘
```

Required components:

```text
MatToolbar
MatSidenav
MatNavList
RouterOutlet
```

---

# 27. Angular API Configuration

Development:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:8080/api'
};
```

Production:

```typescript
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

Do not hardcode API URLs throughout components.

---

# 28. API Connectivity Test

Create:

```text
SystemService
```

Endpoint:

```text
GET /api/system/ping
```

Dashboard displays:

```text
API Status: Connected
```

If unavailable:

```text
API Status: Unavailable
```

This verifies:

```text
Angular
↓
HTTP Client
↓
ASP.NET API
```

---

# 29. CORS

During development allow:

```text
http://localhost:4200
```

Do not allow all origins in production.

---

# 30. API Dockerfile Requirements

Location:

```text
backend/src/FarmManagement.API/Dockerfile
```

Requirements:

- Multi-stage build.
- Restore dependencies.
- Build application.
- Publish application.
- Use runtime image for final container.
- Expose port `8080`.
- Use:

```text
ASPNETCORE_URLS=http://+:8080
```

---

# 31. Angular Dockerfile Requirements

Location:

```text
frontend/farm-management-web/Dockerfile
```

Requirements:

```text
Node build stage
↓
npm ci
↓
Angular production build
↓
Nginx runtime image
```

The final container exposes:

```text
80
```

Docker Compose maps:

```text
4200 → 80
```

---

# 32. Testing Requirements

## Backend

The following commands must work:

```bash
dotnet restore
dotnet build
dotnet test
```

---

## Frontend

The following must work:

```bash
npm ci
npm run build
npm test
```

---

## Docker

The following must work:

```bash
docker compose build
```

Then:

```bash
docker compose up -d
```

Verify:

```bash
docker compose ps
```

All services must be running.

---

# 33. Phase 0 Acceptance Tests

## Test 1 – Database

PostgreSQL container starts successfully.

---

## Test 2 – API

```text
GET /health
```

Returns healthy status.

---

## Test 3 – Ping

```text
GET /api/system/ping
```

Returns success.

---

## Test 4 – OpenAPI

API documentation opens.

---

## Test 5 – Angular

```text
http://localhost:4200
```

loads successfully.

---

## Test 6 – Navigation

All placeholder routes work:

```text
Dashboard
Farms
Crops
Activities
Settings
```

---

## Test 7 – API Integration

Angular dashboard successfully calls the API ping endpoint.

---

# 34. Coding Standards

## Backend

Use:

```text
Async/Await
CancellationToken where appropriate
Dependency Injection
Nullable reference types
Interfaces for infrastructure abstractions
```

Avoid:

```text
Static service classes for business logic
Database access in controllers
Business logic in controllers
God services
```

Controllers should:

```text
Receive request
↓
Call application layer
↓
Return response
```

---

# 35. Frontend Coding Standards

Use:

```text
Standalone Components
Strong TypeScript typing
Reactive Forms
Signals for local reactive state
RxJS for HTTP streams
Feature-based organization
```

Avoid:

```text
any
Business logic inside HTML templates
Duplicate API URLs
Very large components
Direct HttpClient calls scattered across components
```

---

# 36. Git Strategy

Recommended branches:

```text
main
develop
feature/*
```

Examples:

```text
feature/phase-0-foundation

feature/phase-1-authentication

feature/farm-management
```

Each feature should:

```text
Implement
↓
Build
↓
Test
↓
Review
↓
Commit
```

---

# 37. AI Agent Development Rules

AI agents must not receive instructions such as:

```text
Build the entire farm management application.
```

Use focused tasks.

Example:

```text
Implement Phase 0 Docker infrastructure according to SPEC.md.
```

Then:

```text
Implement Phase 0 backend foundation according to SPEC.md.
```

Then:

```text
Implement Phase 0 Angular foundation according to SPEC.md.
```

---

# 38. AI Agent Implementation Prompt

```text
You are implementing the Farm Management Platform.

The complete project specification is defined in SPEC.md.

Your task is limited to the requested phase and feature.

Mandatory rules:

1. Read SPEC.md before implementation.

2. Follow Clean Architecture dependency rules.

3. Do not introduce grape-specific fields into general farm entities.

4. Do not implement functionality outside the requested task.

5. Do not add unnecessary frameworks.

6. Use strongly typed code.

7. Build after implementation.

8. Run tests after implementation.

9. Fix compilation errors.

10. Provide a summary of changed files.

Before making major architectural changes, explain why they are required.
```

---

# 39. AI Agent Review Prompt

```text
Review the current implementation against SPEC.md.

Check:

1. Architecture violations.
2. Incorrect dependencies.
3. Security issues.
4. Hardcoded credentials.
5. Docker configuration.
6. Environment variables.
7. PostgreSQL configuration.
8. Angular structure.
9. Build warnings.
10. Test failures.

Do not rewrite unrelated modules.

Return:

Critical Issues
Recommended Fixes
Optional Improvements
```

---

# 40. AI Agent Testing Prompt

```text
Test the current implementation against SPEC.md.

Execute:

BACKEND

dotnet restore
dotnet build
dotnet test

FRONTEND

npm ci
npm run build
npm test

DOCKER

docker compose build
docker compose up -d

Verify:

1. PostgreSQL is healthy.
2. API starts.
3. GET /health works.
4. GET /api/system/ping works.
5. OpenAPI works.
6. Angular loads.
7. Angular navigation works.
8. Angular can call the API.

Fix implementation errors.

Do not change unrelated functionality.

Return:

Build Results
Test Results
Docker Results
Issues Found
Issues Fixed
Remaining Issues
```

---

# 41. Definition of Done – Phase 0

Phase 0 is complete when:

```text
✓ Solution builds.

✓ Backend tests run.

✓ Angular builds.

✓ Angular tests run.

✓ Docker images build.

✓ PostgreSQL starts.

✓ API starts.

✓ Health endpoint works.

✓ Ping endpoint works.

✓ OpenAPI works.

✓ Angular loads.

✓ Navigation works.

✓ Angular communicates with API.

✓ Environment variables are used.

✓ No real credentials are committed.

✓ Clean Architecture dependency rules are followed.
```

---

# 42. Future Development Roadmap

## Phase 1

```text
Authentication
Users
Roles
Permissions
JWT
Refresh Tokens
```

---

## Phase 2

```text
Organization
Farm
Field
```

---

## Phase 3

```text
Crop Master
Crop Variety
Crop Cycle
```

---

## Phase 4 – Core Activity Engine

```text
Activity Types
Activity Planning
Assignment
Status Workflow
Notes
Materials
Labour
```

---

## Phase 5 – Grape Farming Module

```text
Grape Field Profile
Grape Variety
Pruning
Canopy Management
Spraying
Disease Tracking
Bunch Management
```

---

## Phase 6

```text
Dashboards
Calendar
Notifications
```

---

## Phase 7

```text
Inventory
Labour
Expenses
```

---

## Phase 8

```text
Harvest
Yield
Quality
Revenue
Profitability
```

---

## Phase 9

```text
Reports
Excel Export
PDF Export
Analytics
```

---

## Phase 10

```text
React Native Mobile Application
```

The mobile application must consume the existing ASP.NET Core API.

---

# 43. Final Architecture

```text
                    FARM MANAGEMENT PLATFORM

                              │
               ┌──────────────┴──────────────┐
               │                             │
               ▼                             ▼

        ANGULAR WEB APP              REACT NATIVE APP
        Administration               Future Phase
        Farm Managers
        Supervisors
               │                             │
               └──────────────┬──────────────┘
                              │
                              ▼
                    ASP.NET CORE WEB API
                              │
          ┌───────────────────┼───────────────────┐
          │                   │                   │
          ▼                   ▼                   ▼

      Application          Domain          Infrastructure
          │                   │                   │
          └───────────────────┼───────────────────┘
                              │
                              ▼
                         PostgreSQL
                              │
                              ▼
                           Docker


                    CROP-SPECIFIC MODULES

                              │

              ┌───────────────┼───────────────┐
              │               │               │

              ▼               ▼               ▼

           Grapes          Mango           Future Crops

         Advanced
         Features
```

---

# 44. Implementation Rule

The implementation order must be:

```text
Phase 0 – Foundation
        ↓
Phase 1 – Authentication
        ↓
Phase 2 – Farm Structure
        ↓
Phase 3 – Crop Management
        ↓
Phase 4 – Activity Engine
        ↓
Phase 5 – Grape Module
        ↓
Phase 6+ – Advanced Modules
```

**Do not skip directly to grape-specific functionality before the general Farm, Crop, and Activity architecture is stable.**

---

# End of SPEC.md