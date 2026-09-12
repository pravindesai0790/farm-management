# Farm Management Application
# Phase 3.2 – Attendance Integration & Worker Earnings Contract

**Document Version:** 1.0  
**Target Phase:** Phase 3.2 (Attendance & Daily Work Logging)  
**Integrated Subsystems:** Worker Master (Phase 3.1), Labor Wage Rate Master (Phase 3.1), Worker Earnings Ledger (Phase 3.1)  
**Primary Integration Interface:** `IAttendanceEarningsIntegration`  
**Rate Query Interface:** `ILaborWageRateService`

---

## 1. Executive Summary & Architectural Invariants

In the Farm Management architecture:
> **Attendance is the sole operational source of earned labor wages.**

When recording, updating, or finalizing worker attendance:
1. **No Rate Duplication**: Attendance code must **never** duplicate wage rate selection, date window matching, gender resolution, or gross earning calculation formulas.
2. **Zero Dependency on Labor Activity**: Attendance earnings calculation and ledger entry lifecycle must **never depend on `LaborActivity`**. Whether workers are engaged in spraying, harvesting, irrigation, or general field work, attendance drives earnings based purely on worker eligibility and organization wage rate policy.
3. **Wage Rate Snapshotting**: Historical earnings must be immutable against future changes to the wage master. When attendance is processed, the applicable rate and currency are snapshotted directly onto the earning record.

---

## 2. Exactly Which Service / Interface Phase 3.2 Must Call

Phase 3.2 Attendance has two backend integration points:

| Purpose | Interface to Call | Implementation | Method to Call |
| :--- | :--- | :--- | :--- |
| **Earnings Calculation & Preview** (Real-time UI calculation, validation before save) | `IAttendanceEarningsIntegration` | `WorkerEarningsLedgerService` | `CalculateAttendanceEarningsAsync(actor, request)` |
| **Earnings Ledger Generation & Finalization** (Recording attendance draft, update, or approval) | `IAttendanceEarningsIntegration` | `WorkerEarningsLedgerService` | `ProcessAttendanceEarningsAsync(actor, request)` |
| **Organization Wage Rate Schedule Inspection** (e.g. displaying active rate table in attendance header) | `ILaborWageRateService` | `LaborWageRateService` | `ListAllAsync(...)` or `LookupApplicableRateAsync(...)` |

### Dependency Injection Setup
Both interfaces are pre-registered in ASP.NET Core DI in `backend/src/FarmManagement.API/Program.cs`:
```csharp
builder.Services.AddScoped<ILaborWageRateService, LaborWageRateService>();
builder.Services.AddScoped<IWorkerEarningsLedgerService, WorkerEarningsLedgerService>();
builder.Services.AddScoped<IAttendanceEarningsIntegration, WorkerEarningsLedgerService>();
```
In any Phase 3.2 service or controller, inject:
```csharp
public sealed class AttendanceService(
    IAttendanceEarningsIntegration attendanceEarningsIntegration,
    ILaborWageRateService wageRateService)
{
    // ...
}
```

---

## 3. The 9-Step Earnings Resolution Lifecycle

Whenever attendance earnings are calculated (for preview) or processed (for persistence), the service executes the following 9 steps in strict order:

```text
1. Load Worker (by worker_id and authenticated organization_id)
      ↓
2. Validate Worker Attendance Eligibility (active, date within joining/leaving dates)
      ↓
3. Determine Worker Gender (male, female, other)
      ↓
4. Determine Applicable Wage Type (FULL_DAY, HALF_DAY, HOURLY, MONTHLY, or non-earning ABSENT/LEAVE)
      ↓
5. Resolve Wage Rate (match organization + gender + wage_type + attendance_date)
      ↓
6. Snapshot Wage Rate (freeze applicable rate and currency)
      ↓
7. Calculate Gross Earnings (round(quantity * wage_rate, 2))
      ↓
8. Create / Update Earnings State (CALCULATED / APPROVED / REVERSED based on attendance finalization rules)
      ↓
9. Never Depend on Labor Activity (zero coupling with crop activity or task assignments)
```

### Detailed Step Requirements

#### Step 1: Load the Worker
- Worker is fetched using `worker_id` constrained by `organization_id` from the authenticated user context.
- If worker does not exist or belongs to another organization, a `ResourceNotFoundException` is raised.

#### Step 2: Validate Worker Eligibility on Attendance Date
The worker entity enforces date and status eligibility via `worker.ValidateAttendanceEligibility(attendanceDate)`:
- `IsActive`: Worker must be active. Deactivated workers cannot be attended.
- `JoiningDate`: If `JoiningDate` is set, `attendance_date >= JoiningDate`. You cannot record attendance before a worker joined.
- `LeavingDate`: If `LeavingDate` is set, `attendance_date <= LeavingDate`. You cannot record attendance after a worker left.
- Violations raise a `ValidationException` with clear error messages.

#### Step 3: Determine Worker Gender
- Extracted directly from `worker.Gender`. Gender is required because organization wage rate policies can vary by gender (e.g. Male vs. Female daily rates).

#### Step 4: Determine Applicable Wage Type
The input `attendance_type` or `wage_type` is resolved:
- `FULL_DAY` / `PRESENT` → `WageType.FullDay` (earning-producing, default quantity = 1.0)
- `HALF_DAY` → `WageType.HalfDay` (earning-producing, default quantity = 1.0 session)
- `HOURLY` → `WageType.Hourly` (earning-producing, quantity = hours worked)
- `MONTHLY` → `WageType.Monthly` (earning-producing)
- `ABSENT` → Non-earning (`GrossAmount = 0`, `WageRate = 0`, `IsEarningEligible = false`)
- `LEAVE` → Non-earning (`GrossAmount = 0`, `WageRate = 0`, `IsEarningEligible = false`)

#### Step 5: Resolve Wage Rate by Organization + Gender + Wage Type + Attendance Date
- The wage rate master (`labor_wage_rates`) is queried for an active rate where:
  - `OrganizationId == actor.OrganizationId`
  - `Gender == worker.Gender`
  - `WageType == resolvedWageType`
  - `business_date >= EffectiveFrom` AND (`EffectiveTo == null` OR `business_date <= EffectiveTo`)
- If no active rate matches the date, a descriptive `ValidationException` is thrown (e.g. `"No active labor wage rate found for gender 'Male' and wage type 'FULL_DAY' on 2026-09-01."`).

#### Step 6: Snapshot Wage Rate
- The exact rate and currency ID are frozen. If organization rates are revised later, this snapshot ensures historical attendance records remain immutable.

#### Step 7: Calculate Gross Earnings
- Formula:
  $$\text{GrossAmount} = \text{Math.Round}(\text{Quantity} \times \text{WageRate}, 2)$$
- Rounded to standard currency cents / paisa.

#### Step 8: Create / Update Earnings State According to Attendance Finalization Rules
When `ProcessAttendanceEarningsAsync` is called:
- **New Attendance**:
  - Creates a new row in `worker_earnings_ledger`.
  - `entry_type = EARNING`
  - `status = CALCULATED` (or `APPROVED` if `AutoApprove == true` / attendance is finalized).
  - `attendance_id` is linked.
- **Existing Draft/Calculated Attendance Updated**:
  - Updates `quantity`, `wage_rate`, `gross_amount`, `wage_type`, `description`, `updated_at`, `updated_by` in-place.
  - If `AutoApprove == true`, transitions status to `APPROVED`, setting `finalized_at` and `finalized_by`.
  - No redundant ledger rows created.
- **Approved Attendance Modified**:
  - Creates a paired `REVERSAL` ledger entry for the old earning to maintain financial auditability.
  - Marks the original row as `REVERSED`.
  - Creates a new `EARNING` entry with the revised values.
- **Attendance Changed to Non-Earning (`ABSENT` or `LEAVE`)**:
  - If a `CALCULATED` entry existed, marks it `REVERSED`.
  - If an `APPROVED` entry existed, generates an official reversal entry.
  - Returns `null` (no active gross earnings).

#### Step 9: Zero Dependency on Labor Activity
- Under no circumstances does attendance earnings logic call, reference, or filter on `LaborActivity`.

---

## 4. Backend Contracts & Data Models

### 4.1 Preview / Calculation Contract

#### Request: `CalculateAttendanceEarningsRequest`
```csharp
public sealed record CalculateAttendanceEarningsRequest(
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,    // FULL_DAY, HALF_DAY, HOURLY, ABSENT, LEAVE
    decimal Quantity = 1m);   // Days or Hours
```

#### Response: `AttendanceEarningsCalculationResult`
```csharp
public sealed record AttendanceEarningsCalculationResult(
    Guid WorkerId,
    string WorkerDisplayName,
    Gender Gender,
    string AttendanceType,
    string? WageType,
    decimal Quantity,
    decimal WageRate,
    decimal GrossAmount,
    Guid? CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    bool IsEarningEligible,
    bool IsWorkerEligible,
    string? IneligibilityReason = null);
```

### 4.2 Attendance Processing Contract

#### Request: `ProcessAttendanceEarningsRequest`
```csharp
public sealed record ProcessAttendanceEarningsRequest(
    Guid AttendanceId,
    Guid WorkerId,
    DateOnly AttendanceDate,
    string AttendanceType,
    decimal Quantity = 1m,
    string? Description = null,
    bool AutoApprove = false);
```

#### Response: `WorkerEarningsLedgerResponse?`
```csharp
public sealed record WorkerEarningsLedgerResponse(
    Guid Id,
    Guid OrganizationId,
    Guid WorkerId,
    string WorkerDisplayName,
    Guid? AttendanceId,
    DateOnly EarningsDate,
    string WageType,
    decimal Quantity,
    decimal WageRate,
    Guid CurrencyId,
    string CurrencyCode,
    string CurrencySymbol,
    decimal GrossAmount,
    string EntryType,         // EARNING, REVERSAL, ADJUSTMENT
    string Status,            // CALCULATED, APPROVED, REVERSED
    Guid? ReferenceLedgerId,
    string? Description,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedBy,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);
```

---

## 5. REST API Endpoints

### 5.1 Preview Earnings via HTTP
`POST /api/labor/workers/{workerId}/earnings/calculate-attendance`

**Headers:**
```http
Authorization: Bearer <jwt_access_token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "attendanceDate": "2026-09-12",
  "attendanceType": "HOURLY",
  "quantity": 6.5
}
```

**Response (`200 OK`):**
```json
{
  "workerId": "7d9b9a67-27b2-4d56-a49d-3f0dc7f8c148",
  "workerDisplayName": "Ramesh Kumar",
  "gender": "Male",
  "attendanceType": "HOURLY",
  "wageType": "HOURLY",
  "quantity": 6.5,
  "wageRate": 60.00,
  "grossAmount": 390.00,
  "currencyId": "48b61c71-3cb5-455b-b9f8-b3d5b2c7e099",
  "currencyCode": "INR",
  "currencySymbol": "₹",
  "isEarningEligible": true,
  "isWorkerEligible": true,
  "ineligibilityReason": null
}
```

---

## 6. Testing & Quality Verification

All 9 steps and integration behaviors are verified by automated tests in `FarmManagement.API.Tests`:
- `CalculateAttendanceEarningsAsync_WithValidWorker_CalculatesGrossAndSnapshotsRate`: Verifies gender, wage type mapping, wage rate resolution, and calculation.
- `CalculateAttendanceEarningsAsync_WithHourlyAttendance_CalculatesGrossAmountCorrectly`: Verifies hourly calculations with decimal hours.
- `CalculateAttendanceEarningsAsync_WhenWorkerNotYetJoined_ThrowsValidationException`: Verifies joining date barrier.
- `CalculateAttendanceEarningsAsync_WhenWorkerAlreadyLeft_ThrowsValidationException`: Verifies leaving date barrier.
- `CalculateAttendanceEarningsAsync_WhenAbsentOrLeave_ReturnsNonEarningZeroGross`: Verifies non-earning attendance.
- `ProcessAttendanceEarningsAsync_NewAttendance_CreatesCalculatedEntry`: Verifies initial ledger creation.
- `ProcessAttendanceEarningsAsync_ExistingCalculatedAttendance_UpdatesInPlace`: Verifies in-place update without row bloat.
- `ProcessAttendanceEarningsAsync_AttendanceChangedToAbsent_CancelsPriorCalculatedEntry`: Verifies auto-cancellation when changed to absent.

Test command:
```powershell
dotnet test FarmManagement.sln
```
Result: **208 passed, 0 failed.**
