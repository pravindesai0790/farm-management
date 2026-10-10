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
- `frontend/farm-management-web/src/app/features` contains route-level screens grouped by business area, such as `auth`, `dashboard`, `farms`, `crops`, `activities`, `expenses`, `sprays`, and `settings`.
- `frontend/farm-management-web/src/app/layouts` contains shared application shells such as the main authenticated layout.
- `frontend/farm-management-web/src/app/pages` contains standalone full-page routes like forbidden and other app-level screens.
- `frontend/farm-management-web/src/app/shared` contains reusable UI building blocks that are not tied to one feature.
- `frontend/farm-management-web/src/environments` contains environment-specific API and runtime configuration.

Keep frontend business logic in services and stores under `core`, keep screens thin, and prefer feature-specific code over shared abstractions until reuse is proven.

### Frontend UI/UX Standards
All frontend features, screens, dialogs, tables, and form controls must strictly adhere to the design system rules, spacing scale, density specifications, and typography hierarchy defined in [`UI_UX_GUIDE.md`](UI_UX_GUIDE.md). Avoid creating duplicate page-specific styling; always leverage shared design tokens and global component classes.

## 3. Existing Operational Entities & Cascading Hierarchy

Reference the standard agricultural hierarchy:

$$\text{Farm} \longrightarrow \text{Farm Area} \longrightarrow \text{Plantation} \longrightarrow \text{Crop Cycle} \longrightarrow \text{Crop Cycle Stage}$$

### 3.1 Entity Mapping
- **Farm** ([`Farm`](backend/src/FarmManagement.Domain/Entities/Farm.cs)): Only required application context (`FarmId NOT NULL`).
- **Farm Area** ([`FarmArea`](backend/src/FarmManagement.Domain/Entities/FarmArea.cs)): Optional child of Farm (`FarmAreaId NULL`).
- **Plantation** ([`CropPlantation`](backend/src/FarmManagement.Domain/Entities/CropPlantation.cs)): Optional child of Farm Area (`PlantationId NULL`).
- **Crop Cycle** ([`CropCycle`](backend/src/FarmManagement.Domain/Entities/CropCycle.cs)): Optional child of Plantation (`CropCycleId NULL`).
- **Crop Cycle Stage** ([`CropCycleStage`](backend/src/FarmManagement.Domain/Entities/CropCycleStage.cs)): Optional child of Crop Cycle (`CropCycleStageId NULL`).

### 3.2 Cascading Validation Rules
The hierarchy is optional but strictly cascading:

- If `PlantationId` is set, `FarmAreaId` is required and must match `plantation.FarmAreaId`.
- If `CropCycleId` is set, `PlantationId` is required and must match `cycle.PlantationId`.
- If `CropCycleStageId` is set, `CropCycleId` is required and must match `stage.CropCycleId`.
- Any parent mismatch (e.g., Farm + Plantation without Area, Area not belonging to Farm, or Cycle not belonging to Plantation) must be rejected by backend validation with a `ValidationException`.

## Current Implementation Snapshot

Use this as the starting point for feature work. All core capabilities across Phase 1, Phase 2, Phase 3, Phase 3.5 (Inventory), Phase 3.6 (Expense Management & Procurement), and Phase 3.7 (Spray Application & Plant Protection) are implemented across the backend and frontend.

### Backend Capabilities in Place

- **Platform & Security Foundation**:
  - ASP.NET Core 10 API host with Serilog structured logging, OpenAPI in development, health checks (`/health`), CORS, and global RFC 7807 error handling.
  - PostgreSQL persistence with Entity Framework Core and migrations up to `Phase3_7_001_AddSprayAndPlantProtection` (auto-applied in development).
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
  - Full adherence to the operational hierarchy rules (Farm, Farm Area, Plantation, Crop Cycle, Stage).
- **Inventory & Warehouse Management**:
  - Master data: Inventory items (`InventoryItem`), storage locations (`StorageLocation`), stock units (`Unit`), and categories master table (`InventoryItemCategory`, `Phase3_6_005`) supporting system defaults and tenant-specific categories.
  - Stock balance & ledger (`StockBalance`, `StockMovement`): real-time on-hand stock balances and audit movements across Opening Stock, Receipts, Issues, Adjustments (In/Out), Transfers (In/Out), and Reversals.
  - Concurrency & Integrity: PostgreSQL transaction-level advisory locking (`pg_advisory_xact_lock`), balance update row locking (`FOR UPDATE`), and insufficient stock validation.
  - Transaction Reversals: atomic reversal workflow (`ReverseStockMovementAsync`) generating opposing movement records and linking reversal IDs with audit logging.
  - Operational Linkages: foreign key linkages (`CropCycleId`, `CropCycleStageId`, `PlantationId`, `FarmAreaId`, `LaborActivityId`, `SprayId`) on stock movements for agronomic cost accounting and input tracking.
- **Farm Expense Management & Procurement (Phase 3.6)**:
  - Master data: Expense categories (`ExpenseCategory`), suppliers (`Supplier`) with balance tracking, and currencies (`Currency`).
  - Direct Expenses (`Expense`, `ExpenseStatus`: Draft, Confirmed, Reversed): direct operational costs, amounts, currencies, suppliers, reference numbers, receipt attachments, reversal reason/linkage, and cascading operational hierarchy (Farm, Farm Area, Plantation, Crop Cycle, Stage).
  - Purchase Invoices & 3-Way Receiving (`PurchaseInvoice`, `PurchaseInvoiceLine`, `PurchaseInvoiceReceiptLine`, `PurchaseInvoiceStatus`: Draft, Posted, Reversed): itemized procurement invoices with mixed inventory lines and direct expense lines. Partial and full receiving directly into farm storage locations (`StockMovement` receipt) with receipt groups, idempotency, and received quantity tracking.
  - Supplier Payments & Settlements (`SupplierPayment`, `SupplierPaymentAllocation`, `PaymentStatus`: Draft, Posted, Reversed, `SettlementStatus`: Unpaid, PartiallyPaid, Paid): payments to suppliers with automated FIFO or manual allocation across open posted purchase invoices, reversal workflows, and supplier account balance tracking (`SupplierBalanceService`).
  - Expense Reports & Financial Analytics (`ExpenseReportService`, `ExpenseReportsController`): aggregation by expense category, farm, crop cycle, plantation, and monthly trends, plus supplier aging and statement balances.
- **Spray Application & Plant Protection (Phase 3.7)**:
  - Master data & Chemical Profiles: Plant protection products (`PlantProtectionProduct`) linked to `InventoryItem`, product types (`ProductType`), spray targets (`Target`), and application methods (`ApplicationMethod`). Chemical safety profiles with active ingredients, formulation types, registration numbers, REI (re-entry interval in hours), PHI (pre-harvest interval in days), and standard dosage guidelines.
  - Spray Execution Lifecycle (`Spray`, `SprayProduct`, `SprayStatus`: Draft, Scheduled, In Progress, Completed, Cancelled):
    - Planning & scheduling with target pest, application method, planned area/water volume, and multi-product tank mix recipes conforming to cascading operational hierarchy.
    - Real-time in-progress execution with actual weather telemetry (temperature, relative humidity, wind speed, wind direction), equipment used, and applicator/supervisor tracking.
    - Automated stock deductions: spray completion atomically deducts chemical quantities from farm storage locations via `StockMovement` (Issue) with PostgreSQL advisory locks and `FOR UPDATE` row locks.
    - Safety countdowns: automated calculation and display of REI active expiration timestamps and harvest clearance PHI dates.
    - Direct recording: 1-step "Record Completed Spray" for historical field logging without advance scheduling.
    - Controlled cancellation: cancellation workflow with audit cancellation reasons.
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
- **Inventory & Stock Management**:
  - Inventory items catalog (list, create/edit, category filters, active toggle).
  - Storage locations management per farm (list, create/edit).
  - Stock Overview & On-hand Balances tab with farm/location/item filters and stock operation quick actions.
  - Stock Operations Modal (`StockOperationDialogComponent`) supporting Opening Stock, Receipts, Issues, Adjustments, and Transfers with real-time stock availability check, advisory lock safety, and cascading operational linkages (Crop Cycle, Stage, Plantation, Area, Activity).
  - Stock Movement Ledger (`StockLedgerTabComponent`) with movement badges, operational link tags with deep linking (`?cropCycleId=...`), and transaction reversal dialog integration.
  - Crop Cycle detail page **"Inputs & Inventory Utilized"** tab (`CropCycleInputsTabComponent`) with KPI metrics strip, aggregated net applied quantities ($\sum \text{Issues} - \sum \text{Reversals}$) per item, category chips (Fertilizer, Seed, Chemical, Material), stage breakdowns, interactive reversal dialog, and 1-click "Issue Inputs" modal shortcut pre-selected with cycle metadata.
- **Farm Expenses & Procurement**:
  - Expenses hub with sub-navigation tabs (Direct Expenses, Invoices, Payments, Suppliers, Categories, Balances, Reports).
  - Direct expense list with status/category/farm filters, create/edit modal with cascading operational hierarchy picker (Farm -> Area -> Plantation -> Cycle -> Stage), detail modal, and reversal dialog.
  - Purchase invoice list with supplier/status filters, comprehensive invoice editor supporting mixed inventory and expense lines, detail page, stock receipt dialog with storage location assignment, and invoice reversal dialog.
  - Supplier payment list, payment editor with auto/manual allocation to open invoices, payment detail page, and reversal dialog.
  - Suppliers directory (list and modal editor with tax/contact details).
  - Expense categories management (list and modal editor).
  - Supplier balances & statement page with aging.
  - Expense reporting page with KPI strip, breakdowns by category/farm/cycle, and monthly trend analysis.
- **Spray Application & Plant Protection**:
  - Spray list page with status tabs (All, Scheduled, In Progress, Completed, Cancelled), weather/safety badges, and search/filter controls.
  - Spray editor page for planning sprays with target pest, application method, planned area/water volumes, and multi-product tank mix recipes.
  - Spray execution page for starting and completing sprays with actual weather telemetry (temperature, humidity, wind), operator attribution, actual product quantities, and storage location selection.
  - Record completed spray page for direct 1-step historical logging.
  - Spray detail page with status timeline, safety countdowns (active REI warnings, PHI clearance date), product consumption breakdown with stock movement links, and weather conditions summary.
  - Spray scheduling and cancellation modals with audit reasons.
  - Plant protection products catalog page and editor modal (active ingredients, chemical category, formulation, REI/PHI values, dilution rates).
  - Spray master data management page for product types, application methods, and spray targets.
- **Operational Dashboard**:
  - Farm 360 overview cards, active cycle progress indicators, and daily labor attendance summary cards.
- **Shared UI**:
  - Breadcrumbs navigation, dynamic navbar, reusable pagination, error alert component, and datepicker integrations.

### Automated Test Suite in Place

- Backend test project at `backend/tests/FarmManagement.API.Tests/`.
- 857 passing unit and integration tests verifying domain models, lifecycle stage invariants, wage calculations, attendance finalization, payment allocation engines, inventory transactions, stock balances, advisory locks, transaction reversals, purchase invoices, 3-way receiving, supplier payments, direct expenses, spray lifecycle, chemical deductions, REI/PHI validation, and cascading operational linkages.

### Future Roadmap / Not Yet Implemented

- Irrigation and fertigation management (Phase 3.8: water sources, irrigation blocks/methods, soil moisture, scheduling, and execution tracking).
- Machinery and farm implement maintenance tracking.
- Harvest logging, yield collection, grading, and packing.
- Sales, crop invoicing, dispatch, and customer orders.
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
