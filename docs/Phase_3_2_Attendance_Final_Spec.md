# Farm Management Platform
# Phase 3.2 — Labor Attendance Final Specification

**Status:** Finalized / Implementation Ready  
**Phase:** 3.2  
**Primary Area:** Labor Management / Attendance  
**Architecture:** ASP.NET Core Web API + Clean Architecture + EF Core/PostgreSQL + Angular/TypeScript/Angular Material + Docker  
**Implementation Style:** Vertical Slice / Page-by-Page  
**Depends On:** Phase 3.1 — Worker Master & Labor Payroll Foundation  
**Testing Strategy:** Manual/API verification and Docker verification; do not add Unit/Integration tests unless explicitly requested.

---

## 1. Objective

Phase 3.2 introduces the operational labor attendance workflow on top of the completed Phase 3.1 Worker Master, Farm Assignment, Wage Rate Master, Earnings Ledger, and Worker Payment foundation.

The attendance model must reflect the actual farm business:

> Workers are not corporate-style monthly employees who are expected to report every day. Workers come to a farm when there is work available and are paid based on the attendance actually recorded for the work performed that day.

Therefore, Phase 3.2 is a **daily farm attendance workflow**, not a monthly employee attendance sheet.

The primary operational question is:

> **Who worked at this farm on this date, and how should that attendance be paid?**

---

# 2. Critical Business Rules

## 2.1 Attendance is the only payroll source

The existing Phase 3.1 rule remains mandatory:

```text
Worker
   ↓
Attendance
   ↓
Worker Earnings Ledger
   ↓
Existing Settlement / Payments
```

Labor Activity is intentionally outside this flow:

```text
Worker
   ↓
Labor Activity
   ↓
Work performed / operational history
```

Labor Activity must never create wage earnings.

## 2.2 Attendance is daily and farm-specific

Attendance must always contain:

- organization
- farm
- worker
- attendance date
- attendance type

Farm is mandatory.

The system must not create an attendance record without a farm.

## 2.3 No monthly attendance expectation

Do not implement a corporate-style assumption that every active worker should have an attendance record for every calendar day.

A worker with no attendance record on a date means only that no attendance was recorded for that worker. It does not automatically mean the worker was absent.

The primary operational concept is:

- WORKED / PAID ATTENDANCE
- NOT_WORKED

`NOT_WORKED` is preferred over treating every missing worker as `ABSENT`.

## 2.4 Workforce is selected on demand

For each farm/date, users should explicitly build the daily attendance workforce by:

1. Loading workers eligible for that farm/date.
2. Adding workers who actually worked.
3. Optionally copying the prior day's worker list.
4. Searching and adding individual workers.
5. Removing workers who did not actually work before finalization.

The organization may have 500 workers while only 30 work on a given day. The attendance UI must support this naturally.

## 2.5 Farm assignment eligibility

A worker should normally be selectable for attendance only when the worker has a valid worker-to-farm assignment covering the attendance date.

However, do not trust the Angular UI alone. The backend must enforce the rule.

If the existing business implementation supports an explicit override for emergency/manual attendance, the override must be permission-controlled and auditable. Do not add such an override unless the existing product conventions already support it.

## 2.6 No labor activity association

Phase 3.2 attendance must NOT require:

- labor activity
- activity type
- plantation
- crop cycle
- crop
- farm area
- task/work description

Those belong to the Labor Activity phase.

## 2.7 No overtime

No overtime fields, calculations, multipliers or screens.

Supported paid attendance types are initially:

- FULL_DAY
- HALF_DAY
- HOURLY

Supported non-paid attendance state:

- NOT_WORKED

Leave management is not part of Phase 3.2.

---

# 3. Recommended Daily Attendance Experience

## 3.1 Primary page

Route:

`/labor/attendance`

This is the primary daily operational screen.

It is not a conventional CRUD table of all attendance rows.

The primary layout is:

```text
┌─────────────────────────────────────────────────────────────────────┐
│ Labor Attendance                                                   │
├─────────────────────────────────────────────────────────────────────┤
│ Date   [12-Sep-2026]     Farm [Farm A ▼]                          │
│ Search [Worker name / mobile .........................]            │
│                                                                     │
│ [Load Workers]   [Add Worker]   [Copy Previous Day]                │
├─────────────────────────────────────────────────────────────────────┤
│ Daily Summary                                                      │
│ Worked: 38   Full Day: 30   Half Day: 6   Hourly: 2               │
│ Not Worked: 0   Estimated Earnings: ₹16,450                       │
├─────────────────────────────────────────────────────────────────────┤
│ Worker | Gender | Category | Attendance | Hours | Rate | Earnings  │
│---------------------------------------------------------------------│
│ Ramesh | Male   | Skilled  | Full Day   | —     | ₹500 | ₹500      │
│ Sita   | Female | Unskilled| Full Day   | —     | ₹350 | ₹350      │
│ Mohan  | Male   | Skilled  | Half Day   | —     | ₹275 | ₹275      │
│ Sunita | Female | Unskilled| Hourly    | 6.00  | ₹70  | ₹420      │
│                                                                     │
│                         [Save Draft] [Finalize Attendance]          │
└─────────────────────────────────────────────────────────────────────┘
```

## 3.2 Important UX principle

Do not initially display every worker in the organization as a required attendance row.

Instead, the user should select or load the workers who actually worked.

Recommended entry methods:

### A. Load eligible workers

Load currently assigned active workers for the selected farm/date.

The user can select the workers who actually came to work.

### B. Add individual worker

Search by:

- display name
- mobile number

and add the worker to today's draft attendance.

### C. Copy previous day

For the selected farm and date, provide:

`Copy Previous Day`

This copies the worker roster from the previous attendance date into a new draft. It must copy the worker selection only, not silently finalize attendance or copy old earnings.

The user can then:

- remove workers
- add workers
- change attendance type
- change hours

before saving/finalizing.

If a previous worker is no longer eligible for the selected farm/date, the backend must prevent an invalid copied assignment from being finalized.

---

# 4. Attendance Types

## 4.1 FULL_DAY

Example:

```text
Male FULL_DAY rate = ₹500
Quantity = 1
Earnings = ₹500
```

## 4.2 HALF_DAY

Example:

```text
Female HALF_DAY rate = ₹200
Quantity = 1
Earnings = ₹200
```

## 4.3 HOURLY

Example:

```text
Male HOURLY rate = ₹80
Hours = 6
Earnings = ₹480
```

Hours must be positive and use the project's established decimal precision.

## 4.4 NOT_WORKED

Meaning:

> The worker did not work / should not receive attendance-based earnings for this record.

Earnings must be zero.

No earnings ledger entry should be created for NOT_WORKED.

Whether NOT_WORKED rows are persisted or simply omitted should follow the implementation choice described in Section 7. The preferred design is to persist only actual daily attendance plus an explicit status where the product needs to preserve an operational correction/history record; do not create thousands of unnecessary rows for every worker who did not work.

---

# 5. Attendance Status / Lifecycle

Recommended attendance record lifecycle:

```text
DRAFT
  ↓
FINALIZED
```

## 5.1 DRAFT

Draft attendance:

- can be edited
- can add/remove workers
- can change attendance type
- can change hours
- shows earnings preview
- does not create an approved/finalized earning

The implementation may calculate a provisional preview for display, but the backend remains authoritative.

## 5.2 FINALIZED

Finalization must:

1. Validate organization scope.
2. Validate farm.
3. Validate worker.
4. Validate worker-to-farm assignment for the attendance date.
5. Validate attendance type.
6. Validate hours for HOURLY.
7. Resolve worker gender from Worker Master.
8. Resolve applicable wage type.
9. Resolve effective wage rate from Phase 3.1 Wage Rate Master.
10. Calculate gross earnings.
11. Snapshot wage rate and currency.
12. Create the Worker Earnings Ledger entry.
13. Lock the attendance row against ordinary editing.

Finalization must be transactional so attendance and earnings cannot become inconsistent.

---

# 6. Wage and Earnings Integration

Phase 3.2 must reuse Phase 3.1 services. It must not create duplicate rate-selection or payroll formulas.

Rate selection key:

```text
organization + worker gender + wage type + attendance date
```

The existing Phase 3.1 design requires historical earnings to preserve a wage snapshot. fileciteturn5file5L663-L681

## 6.1 Full Day

```text
quantity = 1
amount = 1 × resolved rate
```

## 6.2 Half Day

```text
quantity = 1
amount = 1 × resolved rate
```

The HALF_DAY rate itself determines the amount. Do not calculate HALF_DAY as 50% of FULL_DAY.

## 6.3 Hourly

```text
quantity = hours
amount = hours × resolved hourly rate
```

## 6.4 Not Worked

```text
amount = 0
```

No payroll earning should be created.

---

# 7. Attendance Data Model

## 7.1 Table: `labor_attendance`

Recommended columns:

| Column | Type | Required | Notes |
|---|---|---:|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Organization owner |
| farm_id | UUID | Yes | Mandatory farm |
| worker_id | UUID | Yes | Worker |
| attendance_date | date | Yes | Business date |
| attendance_type | enum | Yes | FULL_DAY/HALF_DAY/HOURLY/NOT_WORKED |
| working_hours | decimal(18,2) | No | Required for HOURLY |
| status | enum | Yes | DRAFT/FINALIZED |
| calculated_rate | decimal(18,2) | No | Provisional/final resolved rate snapshot if stored |
| calculated_amount | decimal(18,2) | No | Provisional/final calculated earnings |
| currency_id | UUID | No | Currency snapshot/reference if existing architecture prefers it here |
| finalized_at | timestamp | No | Finalization metadata |
| finalized_by | UUID | No | User who finalized |
| notes | text | No | Optional attendance notes |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

### Important design note

The authoritative financial history remains `worker_earnings_ledger`. Attendance fields such as calculated rate/amount are primarily useful for operational display and traceability. The final earnings record must contain the required wage snapshot according to Phase 3.1.

## 7.2 Uniqueness

Recommended business rule:

> A worker may have at most one payroll-generating attendance record for an organization and attendance date.

Recommended uniqueness:

```text
organization_id + worker_id + attendance_date
```

This prevents accidental double-pay.

Because Phase 3.2 has no overtime and no split-day/multi-shift payroll requirement, do not allow multiple farm attendance records for the same worker/date.

If a worker is expected to work across multiple farms on the same day in a future phase, this must be a deliberate business-model change rather than an accidental duplicate.

---

# 8. Farm Assignment Validation

When an attendance record is created/finalized:

```text
Worker
   ↓
worker_farm_assignments
   ↓
Does an active assignment cover attendance_date?
```

Recommended rule:

```text
assigned_from <= attendance_date
AND
assigned_to IS NULL OR assigned_to >= attendance_date
AND
is_active = true
```

The backend must enforce this.

Cross-organization farm/worker combinations are forbidden.

---

# 9. Daily Attendance Roster Behavior

## 9.1 Initial load

For selected farm/date:

- retrieve eligible active workers for that farm/date
- do not automatically create attendance records for everyone
- allow user to select/add actual workers

## 9.2 Existing draft

If a draft exists for the farm/date:

- load it
- show selected workers
- allow edits
- do not create duplicate rows

## 9.3 Existing finalized attendance

If the farm/date has finalized attendance:

- show read-only daily attendance
- hide/disable normal edit actions
- provide correction/reversal workflow only if later business requirements explicitly support it

Do not silently mutate finalized earnings.

---

# 10. Attendance History

A separate history page or history mode is required.

Recommended route:

`/labor/attendance/history`

Suggested filters:

- Date From
- Date To
- Farm
- Worker
- Attendance Type
- Status

Suggested grid:

```text
Date       Farm      Worker     Type       Hours  Earnings  Status
12-Sep     Farm A    Ramesh     FULL_DAY   —      ₹500      Finalized
12-Sep     Farm A    Sita       HALF_DAY   —      ₹200      Finalized
11-Sep     Farm B    Mohan      HOURLY     6      ₹480      Finalized
```

Clicking a row opens attendance detail.

---

# 11. Attendance Detail Page

Recommended route:

`/labor/attendance/:id`

Display:

- date
- farm
- worker
- gender
- labor category
- employment type
- attendance type
- hours
- resolved rate
- calculated earnings
- status
- finalized by/date
- related earnings ledger entry
- notes

The detail page must make the wage calculation traceable without pretending that Angular owns the calculation.

---

# 12. Create / Edit Attendance

Recommended route:

`/labor/attendance/new`

The primary daily workflow may operate from the main grid rather than a separate one-row create form. The route may still be useful for direct entry and must follow the established UI pattern.

Edit route:

`/labor/attendance/:id/edit`

Edit is available only for DRAFT attendance.

For FINALIZED attendance:

- no normal edit
- use a controlled correction flow if implemented
- correction must preserve earnings history

---

# 13. Daily Summary

The daily attendance page should show operational totals for the selected farm/date.

Minimum summary:

```text
Worked Workers
Full Day Count
Half Day Count
Hourly Count
Not Worked Count if applicable
Estimated/Finalized Earnings
```

The summary must come from backend APIs or backend-calculated fields. Angular must not be the authoritative payroll calculator.

For drafts, label the amount as something like:

`Estimated Earnings`

For finalized attendance:

`Finalized Earnings`

---

# 14. Permissions

Add permissions following existing project conventions.

Recommended:

### Attendance

- `Attendance.View`
- `Attendance.Create`
- `Attendance.Update`
- `Attendance.Finalize`
- `Attendance.Unfinalize` only if future business requirements explicitly allow unfinalization; otherwise do not create it
- `Attendance.Reverse` if controlled correction/reversal is implemented

Recommended separation:

- Create/update controls draft attendance.
- Finalize controls the financial transition.
- View controls visibility.

Do not grant finalization merely because a user can create attendance unless that matches the existing product authorization model.

Permission seeding must be idempotent and must not delete existing permissions.

---

# 15. API Scope

Recommended API capabilities:

### Daily Attendance

- get daily attendance roster
- load eligible farm workers
- create draft attendance
- update draft attendance
- remove draft worker attendance
- add worker to daily draft
- copy previous day roster
- finalize daily attendance

### History

- list attendance history
- get attendance detail

### Financial integration

- preview applicable wage/earnings for draft attendance
- finalize attendance and create earnings through the Phase 3.1 earnings service

Avoid duplicating Phase 3.1 wage lookup APIs.

---

# 16. Suggested API Contracts

The actual route naming must follow existing API conventions.

Conceptually:

```text
GET    /api/attendance/daily?farmId=&date=
GET    /api/attendance/eligible-workers?farmId=&date=&search=
POST   /api/attendance/draft
PUT    /api/attendance/{id}
DELETE /api/attendance/{id}          // draft only, if delete pattern permits
POST   /api/attendance/copy-previous-day
POST   /api/attendance/finalize
GET    /api/attendance/history
GET    /api/attendance/{id}
```

Do not assume these exact URLs if the existing API conventions use a different pattern.

---

# 17. Copy Previous Day

This is an important farm-specific productivity feature.

Input:

```text
farm_id
source_date
target_date
```

Behavior:

1. Confirm source farm/date exists.
2. Read workers in source daily roster.
3. Validate each worker's current assignment for target date.
4. Create a target draft roster for eligible workers.
5. Do not copy final earnings.
6. Do not copy finalized status.
7. Do not create earnings ledger entries.
8. Prevent duplicate target attendance records.

If a worker from the source day is no longer eligible for the target date, exclude the worker and return a useful warning/result message.

---

# 18. Draft Earnings Preview

For a DRAFT attendance row the UI may display a backend-generated preview:

```text
Worker Gender = Female
Attendance Type = FULL_DAY
Applicable Rate = ₹350
Calculated Earnings = ₹350
```

For HOURLY:

```text
Hours = 6
Rate = ₹70
Calculated Earnings = ₹420
```

Preview must call backend logic.

Do not implement financial formulas only in Angular.

---

# 19. Finalization Transaction

Finalization should be implemented as one backend business operation.

Conceptually:

```text
Begin Transaction

Validate attendance rows
Validate organization
Validate worker
Validate farm assignment
Validate attendance type/hours

For each paid attendance:
    Resolve wage rate using Phase 3.1 service
    Calculate earnings
    Snapshot wage data
    Create earnings ledger entry

Mark attendance FINALIZED

Commit Transaction
```

On any validation failure:

- no attendance row should become finalized
- no earnings ledger entries should remain partially created

Use the project's existing transaction/unit-of-work conventions.

---

# 20. Correction After Finalization

Phase 3.2 should not implement arbitrary direct mutation of finalized attendance.

If future requirements permit correction:

```text
Original Finalized Attendance
        ↓
Controlled Correction
        ↓
Reverse Original Earnings
        ↓
Create Corrected Earnings
        ↓
Maintain Audit Trail
```

This follows the existing Phase 3.1 financial correction rule. fileciteturn5file3L685-L711

For initial Phase 3.2, the safer default is:

> Finalized attendance is read-only.

---

# 21. Out of Scope

The following are explicitly out of Phase 3.2:

- Monthly corporate attendance calendar
- Automatic daily absence generation
- Leave management
- Overtime
- Shift management
- Multiple payroll attendance records per worker/day
- Labor Activity integration
- Farm Area association
- Plantation association
- Crop Cycle association
- Attendance productivity scoring
- GPS/mobile attendance
- Biometric attendance
- Payroll deductions
- Statutory payroll
- Contractor invoicing
- Mobile app

---

# 22. Angular Routes

Recommended:

```text
/labor/attendance
/labor/attendance/new
/labor/attendance/history
/labor/attendance/:id
/labor/attendance/:id/edit
```

The primary page `/labor/attendance` should remain the operational daily attendance screen.

---

# 23. Recommended Angular Component Structure

Follow existing project conventions. Conceptually:

```text
attendance/
├── pages/
│   ├── attendance-daily/
│   ├── attendance-history/
│   ├── attendance-detail/
│   └── attendance-form/
├── components/
│   ├── daily-attendance-grid/
│   ├── attendance-worker-selector/
│   ├── attendance-summary/
│   ├── attendance-filters/
│   └── attendance-finalize-dialog/
└── services/
    └── attendance.service.ts
```

Do not create this exact structure if the existing Angular architecture uses another established pattern.

---

# 24. Daily Attendance Workflow Example

### Scenario

Farm A needs 30 workers on 12-Sep-2026.

There are 150 registered workers in the organization.

User opens:

`Labor → Attendance`

Selects:

```text
Date = 12-Sep-2026
Farm = Farm A
```

Clicks:

`Load Workers`

The system finds eligible assigned workers.

The user selects 30 workers.

Example:

```text
20 Full Day
7 Half Day
3 Hourly
```

Hourly workers:

```text
Worker A = 6 hours
Worker B = 5 hours
Worker C = 7 hours
```

The backend resolves rates:

```text
Male Full Day   ₹500
Female Full Day ₹350
Male Half Day   ₹275
Female Half Day ₹200
Male Hourly     ₹80
Female Hourly   ₹70
```

The page displays the calculated earnings.

User saves draft.

Later user reopens the page, verifies everything, and clicks:

`Finalize Attendance`

Backend creates the earnings ledger entries and locks the attendance.

The existing Phase 3.1 Worker Financial/Settlement system can now include these earnings.

---

# 25. Security and Organization Isolation

Every attendance query and mutation must derive organization scope from authenticated server-side context.

Never trust browser-supplied:

`organization_id`

Never allow:

- worker from organization A with farm from B
- attendance access from organization B
- earnings creation for another organization

All foreign key relationships must be organization-safe.

---

# 26. Audit Requirements

Attendance should retain:

- created_at
- created_by
- updated_at
- updated_by
- finalized_at
- finalized_by

Where the existing audit architecture supports it, record corrections/reversals explicitly.

Do not hard-delete finalized attendance history.

---

# 27. Reporting Readiness

Phase 3.2 should make these dimensions available for future reporting:

- organization
- farm
- worker
- gender
- labor category
- employment type
- attendance date
- attendance type
- wage type
- earnings
- status

Future reports should be able to answer:

- labor cost by farm
- labor cost by date
- labor cost by worker
- worker attendance history
- full-day vs half-day vs hourly labor usage
- daily farm labor cost

Do not build advanced reporting screens in Phase 3.2.

---

# 28. Definition of Done

Phase 3.2 is complete when:

## Attendance

- Daily attendance works.
- Farm is mandatory.
- Workers can be selected on demand.
- Eligible farm workers can be loaded.
- Individual workers can be added.
- Previous day roster can be copied.
- Worker/date duplicate payroll attendance is prevented.
- Full Day works.
- Half Day works.
- Hourly works.
- Not Worked works.
- No overtime exists.
- No monthly attendance assumption exists.

## Wage Integration

- Correct gender-based wage rate is resolved.
- Effective date is respected.
- Existing Phase 3.1 wage lookup is reused.
- Historical wage snapshot is preserved.

## Earnings

- Finalized paid attendance creates earnings ledger entries.
- Not Worked does not create earnings.
- Earnings are calculated by backend logic.
- Finalized attendance cannot be silently edited.

## Farm Assignment

- Worker must be eligible for the selected farm/date.
- Cross-organization worker/farm combinations are rejected.

## UI

- Daily attendance page works.
- Attendance history works.
- Attendance detail works.
- Draft editing works.
- Finalization confirmation works.
- Daily summary works.
- Loading/empty/error states work.
- Permissions work.

## Financial Integration

- New attendance earnings appear in the existing Worker Earnings Ledger.
- Existing worker settlement/payment screens automatically reflect new earnings.
- No duplicate payroll calculation has been added.

## Infrastructure

- PostgreSQL migration works in Docker.
- API starts successfully.
- Angular starts successfully.
- Phase 3.1 remains fully operational.

---

# 29. Phase 3.3 Handoff

After Phase 3.2, the next labor feature should be Labor Activity.

Recommended direction:

```text
Worker
   ↓
Attendance  ─────────────→ Earnings Ledger
   │
   └── Farm

Worker
   ↓
Labor Activity
   ↓
Farm Area / Plantation / Crop Cycle / Activity
```

Labor Activity must remain operationally separate from payroll earnings.

