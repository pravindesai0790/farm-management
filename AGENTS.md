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

Use this as the starting point for feature work. All core Phase 1, Phase 2, and Phase 3 capabilities are implemented across the backend and frontend.

### Backend Capabilities in Place

- **Platform & Security Foundation**:
  - ASP.NET Core 10 API host with Serilog structured logging, OpenAPI in development, health checks (`/health`), CORS, and global RFC 7807 error handling.
  - PostgreSQL persistence with Entity Framework Core and migrations up to `Phase3_016` (auto-applied in development).
  - JWT authentication with in-memory access tokens, HttpOnly refresh token cookie rotation, password hashing, and account lockout handling.
  - Granular permission-based authorization policies and centralized actor context resolution (`UserContextHelper`).
- **Organization & Administration**:
  - Multi-tenant organization support (`Organization`) restricted to SuperAdmin role for creation.
  - User administration (`User`, `UserRole`) with pagination, activation/deactivation, and role assignments.
  - Role and permission administration (`Role`, `Permission`, `RolePermission`) with dynamic permission matrix management.
  - User password change and security audit logging (`AuditLog`).
- **Farms & Land Parcelation**:
  - Farm master (`Farm`, `FarmOwnershipType`): location, total area, ownership types, Farm 360 overview, KPI metrics, and active cycle summaries.
  - Land parcelation (`FarmArea`): hierarchical zones, blocks, and plots with capacity calculation and area utilization tracking.
  - Cleaned domain identifier model (legacy `Code` columns removed from Farm, FarmArea, Crop, Plantation, and CropCycle in favor of clean IDs/names).
- **Crops, Varieties & Lifecycle Templates**:
  - Crop and variety master data (`Crop`, `CropVariety`): days to maturity, standard planting density, botanical information.
  - Master data (`Unit`, `UnitCategory`, `Currency`, `PlantationEndReason`).
  - Crop lifecycle templates (`CropLifecycleTemplate`, `CropLifecycleStage`): configurable templates with ordered stages and stage durations.
- **Plantations & Crop Cycles**:
  - Crop plantations (`CropPlantation`, `PlantationStatus`, `PlantationEndReason`): area allocation validation against available land, planting dates, activation, and termination workflows with reason tracking.
  - Crop cycles (`CropCycle`, `CropCycleStatus`): cycle scheduling, season tracking, target vs. actual yield, active cycle cancellation, and completion flows.
  - Crop lifecycle execution (`CropCycleStage`, `CropCycleStageStatus`): automated stage generation on cycle start, sequential progression (Pending -> In Progress -> Completed/Skipped), controlled planned date editing, stage invariants, and plantation status synchronization on completion.
- **Labor Master & Workforce Management**:
  - Workers (`Worker`): worker profiles, employment types, national IDs, contact info, and skill categories.
  - Contractors (`Contractor`) and labor categories (`LaborCategory`).
  - Worker-to-farm assignments (`WorkerFarmAssignment`): primary and secondary farm assignments.
  - Wage rates (`LaborWageRate`, `WageType`, `Currency`): hourly, daily, piece-rate, and overtime wage definitions with effective dating.
- **Labor Attendance, Payroll Earnings & Worker Payments**:
  - Daily attendance (`LaborAttendance`, `AttendanceStatus`, `AttendanceType`): attendance drafting, multi-worker roster grid editing, eligible workers lookup by farm assignment, wage previews, previous day cloning, and finalization confirmation workflows.
  - Worker earnings ledger (`WorkerEarningsLedger`, `EarningsEntryType`, `EarningsLedgerStatus`): automatic, immutable earnings ledger creation on attendance finalization, with unique constraint enforcing one earnings record per attendance entry (`Phase3_010`).
  - Worker payments & settlements (`WorkerPayment`, `WorkerPaymentAllocation`, `SettlementStatus`): payment recording, advances, payout settlements, balance calculations, and FIFO payment-to-earnings allocation.
- **Labor Activities**:
  - Field activity types (`LaborActivityType`) and field activities (`LaborActivity`, `LaborActivityStatus`): scheduling and execution tracking linked to farms, plantations, and cycles, with cancellation workflows and audit reasons.
- **Dashboard APIs**:
  - Farm 360 metrics, active crop cycle progress trackers, and daily workforce attendance/labor cost summaries.

### Frontend Capabilities in Place (Angular 20 Standalone)

- Standalone architecture utilizing Angular Signals, reactive forms, Angular Material, and CDK drag-and-drop.
- **Auth & Administration**: Login, main authenticated layout shell, route guards (`authGuard`, `permissionGuard`), HTTP interceptors, users/roles/permissions management screens, and password settings.
- **Farms & Areas**:
  - Farm list with server-side pagination, filters, and farm editor.
  - Farm details 360 view with Overview, KPI strip, Areas tab, Plantations tab, Activities tab, and active season cycles progress.
  - Farm areas list, hierarchical parcelation editor, and utilization tracking.
- **Crops & Lifecycle Templates**:
  - Crop and variety list, editor, and details.
  - Crop lifecycle template list, detail view, and editor with drag-and-drop stage ordering.
- **Plantations & Crop Cycles**:
  - Plantation list, editor (with dynamic available area calculation), details, and termination modal with reason selection.
  - Crop cycle list, editor (template selection), and details featuring an interactive Lifecycle Tab with visual stage timeline, progression actions (Start, Complete, Skip), and controlled planned dates editing dialog.
- **Labor & Attendance**:
  - Daily attendance grid editor with worker selection dialog, wage previews, previous day cloning, and finalization confirmation.
  - Attendance history list, attendance detail view, and draft editor.
  - Worker master list, editor, detail view, farm assignment dialog, and financial ledger / payment history.
  - Worker payments and payout settlement interface.
  - Wage rates and contractors master management.
- **Labor Activities**:
  - Labor activity list with status filters and pagination, activity editor, activity details, and cancellation modal.
- **Operational Dashboard**:
  - Farm 360 overview cards, active cycle progress indicators, and daily labor attendance summary cards.
- **Shared UI**:
  - Breadcrumbs navigation, dynamic navbar, reusable pagination, error alert component, and datepicker integrations.

### Automated Test Suite in Place

- Backend test project at `backend/tests/FarmManagement.API.Tests/`.
- 425 passing unit and integration tests verifying domain models, lifecycle stage invariants, wage calculations, attendance finalization, and payment allocation engines.

### Future Roadmap / Not Yet Implemented

- Inventory & Warehouse management (seeds, fertilizers, crop protection chemicals, equipment).
- Machinery and farm implement maintenance tracking.
- Harvest logging, yield collection, grading, and packing.
- Sales, crop invoicing, dispatch, and customer orders.
- General farm expense management and accounting beyond labor wages.
- Crop-specific viticulture/specialty modules (e.g. grape trellis systems, brix monitoring, phenological stages).
- File uploads and document storage integration (cloud/local).
- Reporting engine & BI exports (PDF/Excel).
- Mobile client / PWA offline synchronization.
- Frontend automated unit/e2e test coverage.

## Build, Test, and Development Commands

Run commands from `backend/` with the .NET 10 SDK installed:

```powershell
dotnet restore FarmManagement.sln
dotnet build FarmManagement.sln
dotnet run --project src/FarmManagement.API/FarmManagement.API.csproj
dotnet test FarmManagement.sln
```

`dotnet test` runs the test suite in `backend/tests/FarmManagement.API.Tests/`. The API exposes development-only OpenAPI metadata and uses the configured HTTPS development profile.

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, nullable reference types enabled, implicit usings enabled, `PascalCase` for types and public members, and `camelCase` for local variables and parameters. Prefer focused classes and keep domain logic independent of ASP.NET Core or database concerns. Follow the existing SDK-style `.csproj` layout and format changed C# files with `dotnet format` when available.

## Testing Guidelines

Backend unit and integration tests reside in `backend/tests/FarmManagement.API.Tests/`. Add tests matching the production unit or feature being modified (e.g., `Services/`, `Domain/`, or `Helpers/`), and use descriptive test method names such as `ProgressStage_WhenPrecedingStagePending_ThrowsDomainException`. Always verify that all tests pass (`dotnet test FarmManagement.sln`) before submitting changes.

## Commit & Pull Request Guidelines

The history currently contains only `Initial commit`, so no detailed convention is established. Use short, imperative subjects (for example, `Add livestock registration endpoint`). Pull requests should explain the behavior change, identify validation commands run, link the relevant issue, and include request/response examples or screenshots when API or UI behavior changes.

## Security & Configuration Tips

Keep secrets and local connection strings out of tracked configuration. Use environment variables or user-secrets for development values, and review `appsettings.*.json` changes carefully before opening a pull request.
