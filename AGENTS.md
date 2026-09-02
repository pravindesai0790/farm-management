# Repository Guidelines

## Project Structure & Module Organization

The repository is organized as a layered .NET solution:

- `backend/FarmManagement.sln` is the solution entry point.
- `backend/src/FarmManagement.Domain` contains core business concepts and rules.
- `backend/src/FarmManagement.Application` contains use cases and application orchestration; it references Domain.
- `backend/src/FarmManagement.Infrastructure` contains persistence and external-service implementations, including Entity Framework Core and PostgreSQL integration.
- `backend/src/FarmManagement.API` is the ASP.NET Core HTTP host and local API client entry point (`FarmManagement.API.http`).
- `frontend/`, `docker/`, `docs/`, and `scripts/` are reserved for future client, deployment, documentation, and automation work. Keep generated `bin/` and `obj/` output out of commits.

The Angular frontend follows a feature-first structure:

- `frontend/farm-management-web/src/app/core` contains singleton services, auth state, guards, interceptors, and API-facing models.
- `frontend/farm-management-web/src/app/features` contains route-level screens grouped by business area, such as `auth`, `dashboard`, `farms`, `crops`, `activities`, and `settings`.
- `frontend/farm-management-web/src/app/layouts` contains shared application shells such as the main authenticated layout.
- `frontend/farm-management-web/src/app/pages` contains standalone full-page routes like forbidden and other app-level screens.
- `frontend/farm-management-web/src/app/shared` contains reusable UI building blocks that are not tied to one feature.
- `frontend/farm-management-web/src/environments` contains environment-specific API and runtime configuration.

Keep frontend business logic in services and stores under `core`, keep screens thin, and prefer feature-specific code over shared abstractions until reuse is proven.

## Current Implementation Snapshot

Use this as the starting point for feature work.

### Backend foundation already in place

- ASP.NET Core API host with Serilog, OpenAPI in development, CORS, JWT authentication, permission-based authorization, health checks, request logging, and global error handling.
- Authentication endpoints for login, refresh, logout, current-user lookup, and change-password.
- Administration APIs for users, roles, and permissions, including pagination, activation/deactivation, role assignment, and role-permission management.
- PostgreSQL persistence with EF Core migrations, database seeding, JWT/refresh-token infrastructure, and password hashing.

### Frontend foundation already in place

- Angular standalone app shell with login, authenticated layout, route guards, and a dashboard that pings the backend.
- Auth state management keeps the access token in memory and relies on an HttpOnly refresh-token cookie.
- Farms, crops, activities, and settings routes currently render placeholders.

### Feature implementation priorities

- Build the crop-agnostic core first: farms, fields, crop master data, crop varieties, crop cycles, and activities.
- Keep business rules in `FarmManagement.Domain` and orchestration in `FarmManagement.Application`; keep API and persistence concerns out of those layers.
- Reuse the existing permission model and actor pattern for all new administration and business endpoints.
- Replace placeholder frontend pages with API-driven screens only when the corresponding backend use case exists.

### Not yet implemented

- Farm and crop business workflows.
- File uploads, reports, and crop-specific modules.
- Mobile client support.
- Test projects and automated coverage.

## Build, Test, and Development Commands

Run commands from `backend/` with the .NET 10 SDK installed:

```powershell
dotnet restore FarmManagement.sln
dotnet build FarmManagement.sln
dotnet run --project src/FarmManagement.API/FarmManagement.API.csproj
dotnet test FarmManagement.sln
```

`dotnet test` is the standard verification command; it currently succeeds only when test projects are added to the solution. The API exposes development-only OpenAPI metadata and uses the configured HTTPS development profile.

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, nullable reference types enabled, implicit usings enabled, `PascalCase` for types and public members, and `camelCase` for local variables and parameters. Prefer focused classes and keep domain logic independent of ASP.NET Core or database concerns. Follow the existing SDK-style `.csproj` layout and format changed C# files with `dotnet format` when available.

## Testing Guidelines

No test projects or coverage thresholds are present yet. Add tests under a parallel `backend/tests/` directory, name projects and files after the production unit (for example, `FarmManagement.Domain.Tests`), and use descriptive method names such as `Create_WhenInputIsInvalid_ReturnsError`. Run the solution test command before submitting changes.

## Commit & Pull Request Guidelines

The history currently contains only `Initial commit`, so no detailed convention is established. Use short, imperative subjects (for example, `Add livestock registration endpoint`). Pull requests should explain the behavior change, identify validation commands run, link the relevant issue, and include request/response examples or screenshots when API or UI behavior changes.

## Security & Configuration Tips

Keep secrets and local connection strings out of tracked configuration. Use environment variables or user-secrets for development values, and review `appsettings.*.json` changes carefully before opening a pull request.
