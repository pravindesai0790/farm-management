# Farm Management Platform
# Phase 3.1 — Worker Master & Labor Payroll Foundation

**Status:** Finalized / Implementation Ready  
**Phase:** 3.1  
**Primary Area:** Labor Management  
**Architecture:** ASP.NET Core Web API + Angular + PostgreSQL + Docker  
**Implementation Style:** Vertical Slice / Page-by-Page  

---

## 1. Objective

Phase 3.1 establishes the foundation for labor management and worker payroll processing.

The phase covers:

- Worker Master
- Contractor Master
- Labor Category Master
- Organization-level Labor Wage Rate Master
- Worker-to-Farm Assignments
- Worker Payment / Advance records
- Worker Earnings Ledger foundation
- Payroll calculation rules driven by attendance
- Partial and final payouts
- Payment allocation and outstanding balance calculation
- Auditability and historical wage preservation
- Angular pages, APIs, permissions, validation, and database changes

### Critical business rule

**Attendance is the sole source of earned labor wages.**

Labor Activity records what work a worker performed, but **labor activity must never calculate or generate payroll earnings**.

The payroll relationship is:

```text
Worker
   ↓
Attendance
   ↓
Worker Earnings Ledger
   ↓
Advances / Payments / Adjustments
   ↓
Outstanding / Final Payout
```

---

# 2. Scope

## 2.1 In Scope

### Worker
- Create worker
- View worker
- Edit worker
- Activate/deactivate worker
- Worker gender
- Employment type
- Labor category
- Contractor association
- Joining/leaving dates
- Contact details
- Notes
- Multiple-farm assignment

### Contractor
- Create contractor
- View contractor
- Edit contractor
- Activate/deactivate contractor
- Contact information

### Labor Category
- System and organization-specific categories
- Create/update/activate/deactivate

### Wage Rate Master
- Gender-based wage rates
- Wage-type-based rates
- Effective-date history
- Full-day, half-day, hourly, monthly rates
- No overtime
- Historical rate preservation
- No overlapping effective periods

### Worker Financial Records
- Advance payments
- Partial payouts
- Final payouts
- Payment history
- Payment allocation
- Outstanding amount
- Carry-forward advance balance

### Earnings Ledger
- Attendance-generated earnings
- Wage snapshot
- Earnings correction/reversal support
- Approval/finalization model
- Immutable financial history

## 2.2 Out of Scope for Phase 3.1

- Attendance screen and attendance transaction processing UI
- Labor Activity screen
- Overtime
- Leave management workflow
- Payroll tax/statutory deductions
- Provident fund/ESI/TDS
- Bank salary processing
- Contractor invoice management
- Labor productivity calculations
- Crop activity costing
- Advanced payroll reports
- Mobile application

Attendance is planned for **Phase 3.2** and will consume the wage configuration created in Phase 3.1.

---

# 3. Core Business Principles

## 3.1 Organization Isolation

Every labor entity must belong to the authenticated user's organization unless explicitly designated as global/system master data.

The organization ID must be derived from the authenticated server-side context and must never be trusted from a client-supplied organization ID.

A user from Organization A must never be able to view or modify workers, contractors, wage rates, payments, earnings, or assignments from Organization B.

---

## 3.2 Worker Has No Code

The Worker table must **not** contain a worker code.

Workers are identified by database ID and display information such as:

- Display name
- Mobile number
- Gender
- Labor category
- Employment type
- Contractor

There is no business requirement for a worker code at this stage.

---

## 3.3 Contractor Has No Code

The Contractor table must **not** contain a contractor code.

Contractors are identified by database ID and name/contact information.

---

## 3.4 Gender Affects Wage Rate

Gender is part of the Worker Master because the organization's wage configuration may differ by gender.

Example:

| Gender | Wage Type | Rate |
|---|---|---:|
| Male | Full Day | ₹500 |
| Female | Full Day | ₹350 |
| Male | Half Day | ₹275 |
| Female | Half Day | ₹200 |

Supported gender values:

- MALE
- FEMALE
- OTHER

The exact labels can be localized in the UI later, but stored values should be stable enum/domain values.

---

# 4. Worker Employment Model

## 4.1 Employment Types

Supported values:

- PERMANENT
- SEASONAL
- DAILY_WAGE
- CONTRACT

### Rules

- CONTRACT workers must have a contractor.
- Other employment types may optionally be associated with a contractor where the business permits it.
- A worker is an individual person regardless of employment type.

---

# 5. Worker Master

## 5.1 Table: `workers`

Suggested columns:

| Column | Type | Required | Notes |
|---|---|---|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Organization owner |
| first_name | varchar | Yes | Worker first name |
| last_name | varchar | No | Worker last name |
| display_name | varchar | Yes | Display/full name |
| gender | varchar/enum | Yes | MALE/FEMALE/OTHER |
| mobile_number | varchar | No | Primary phone |
| alternate_mobile_number | varchar | No | Secondary phone |
| labor_category_id | UUID | No | FK to labor categories |
| employment_type | varchar/enum | Yes | PERMANENT/SEASONAL/DAILY_WAGE/CONTRACT |
| contractor_id | UUID | No | FK to contractor |
| joining_date | date | No | Employment start |
| leaving_date | date | No | Employment end |
| notes | text | No | Additional information |
| is_active | boolean | Yes | Soft-active status |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

### Constraints

- No `worker_code`.
- Contractor belongs to the same organization.
- Labor category belongs to same organization or is a global/system category.
- `leaving_date >= joining_date` when both exist.
- CONTRACT employment type requires `contractor_id`.
- Inactive workers cannot be used for new attendance after their effective leaving date.
- Historical attendance, earnings and payments must remain available after deactivation.

---

# 6. Contractor Master

## 6.1 Table: `contractors`

Suggested columns:

| Column | Type | Required | Notes |
|---|---|---|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Organization owner |
| name | varchar | Yes | Contractor name |
| contact_person | varchar | No | Contact person |
| phone_number | varchar | No | Phone |
| email | varchar | No | Email |
| address | text | No | Address |
| notes | text | No | Notes |
| is_active | boolean | Yes | Soft-active status |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

No contractor code is required.

---

# 7. Labor Category Master

Examples:

- Skilled
- Semi-Skilled
- Unskilled
- Supervisor
- Operator
- Specialized

## 7.1 Table: `labor_categories`

Suggested columns:

| Column | Type | Required |
|---|---|---|
| id | UUID | Yes |
| organization_id | UUID nullable | Yes/No |
| name | varchar | Yes |
| description | text | No |
| is_system | boolean | Yes |
| is_active | boolean | Yes |
| created_at | timestamp | Yes |
| created_by | UUID | Yes |
| updated_at | timestamp | Yes |
| updated_by | UUID | Yes |

`organization_id = NULL` may be used for global/system categories.

Organization-specific categories are scoped to the organization.

---

# 8. Worker-to-Farm Assignment

A worker must not have a single `farm_id` column because a worker can work across multiple farms in the same organization.

## 8.1 Table: `worker_farm_assignments`

| Column | Type | Required |
|---|---|---|
| id | UUID | Yes |
| organization_id | UUID | Yes |
| worker_id | UUID | Yes |
| farm_id | UUID | Yes |
| assigned_from | date | Yes |
| assigned_to | date | No |
| is_active | boolean | Yes |
| notes | text | No |
| created_at | timestamp | Yes |
| created_by | UUID | Yes |
| updated_at | timestamp | Yes |
| updated_by | UUID | Yes |

### Rules

- Worker and farm must belong to the same organization.
- A worker may have multiple active farm assignments.
- Assignment dates cannot be invalid.
- Future attendance should normally require a valid farm assignment for that date.
- Removing/deactivating a worker does not delete historical assignments.

---

# 9. Organization Wage Rate Master

## 9.1 Why wage rates are not worker-specific

The organization's wage policy is based on:

- Gender
- Wage type
- Effective date

Therefore, wage rates should be maintained at organization level instead of storing a separate wage amount against every worker.

A worker's applicable rate is resolved from the current wage master using the worker's gender and attendance wage type.

---

## 9.2 Table: `labor_wage_rates`

| Column | Type | Required | Notes |
|---|---|---|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Owner |
| gender | varchar/enum | Yes | MALE/FEMALE/OTHER |
| wage_type | varchar/enum | Yes | FULL_DAY/HALF_DAY/HOURLY/MONTHLY |
| wage_rate | decimal(18,2) | Yes | Positive amount |
| currency_id | UUID | Yes | FK to currency master |
| effective_from | date | Yes | Rate start date |
| effective_to | date | No | Rate end date |
| notes | text | No | Notes |
| is_active | boolean | Yes | Active/inactive |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

### Supported wage types

Initially:

- FULL_DAY
- HALF_DAY
- HOURLY
- MONTHLY

### Overtime

**No overtime implementation.**

No overtime multiplier, overtime hours, or overtime earning calculation is part of this design.

---

# 10. Wage Rate Effective Dating

Wage history must be preserved.

Example:

```text
Male Full Day
01-Jan to 30-Jun   ₹450
01-Jul onward      ₹500
```

When ₹500 becomes effective, the previous rate should be closed at the day before the new effective date.

### Rule

For the same:

```text
organization + gender + wage_type
```

effective periods must not overlap.

### Historical principle

Changing the wage master must never change already finalized historical earnings.

Attendance finalization will snapshot the applicable wage rate into the earnings record.

---

# 11. Worker Earnings Ledger

The Worker Earnings Ledger is the financial foundation for labor payroll.

## 11.1 Key principle

Attendance creates earnings.

No other operational activity creates wage earnings.

```text
Attendance
    ↓
Earnings Ledger Entry
```

Labor Activity is intentionally independent:

```text
Labor Activity
    └── records work performed
        └── does NOT generate payroll
```

---

# 12. Earnings Ledger Table

## 12.1 Table: `worker_earnings_ledger`

Suggested columns:

| Column | Type | Required | Notes |
|---|---|---|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Organization |
| worker_id | UUID | Yes | Worker |
| attendance_id | UUID | No | Source attendance record |
| earnings_date | date | Yes | Business date |
| wage_type | varchar/enum | Yes | FULL_DAY/HALF_DAY/HOURLY/MONTHLY |
| quantity | decimal(18,2) | Yes | 1 day, 0.5 day or hours depending on wage type |
| wage_rate | decimal(18,2) | Yes | Snapshot of rate |
| currency_id | UUID | Yes | Currency snapshot/reference |
| gross_amount | decimal(18,2) | Yes | Calculated earnings |
| entry_type | varchar/enum | Yes | EARNING / REVERSAL / ADJUSTMENT |
| status | varchar/enum | Yes | CALCULATED/APPROVED/REVERSED |
| reference_ledger_id | UUID | No | Links reversal/correction to original |
| description | varchar/text | No | Description |
| finalized_at | timestamp | No | Finalization timestamp |
| finalized_by | UUID | No | User who finalized |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

---

# 13. Earnings Calculation Examples

## Full day

```text
Attendance Type = FULL_DAY
Male Full Day Rate = ₹500
Quantity = 1

Earnings = 1 × ₹500 = ₹500
```

## Half day

```text
Attendance Type = HALF_DAY
Female Half Day Rate = ₹200
Quantity = 1

Earnings = ₹200
```

## Hourly

```text
Attendance Type = HOURLY
Hourly Rate = ₹80
Hours = 6

Earnings = 6 × ₹80 = ₹480
```

No overtime calculation is performed.

---

# 14. Wage Snapshot Rule

When an attendance record becomes finalized, the system must resolve the applicable wage rate and snapshot it into the earnings ledger.

Example:

```text
01-Jun Attendance
Female + Full Day
Rate at that time = ₹350

Earnings Ledger:
Rate Snapshot = ₹350
Amount = ₹350
```

If the wage master later changes to ₹400, the old ₹350 earning remains ₹350.

This is mandatory for historical payroll correctness.

---

# 15. Earnings Corrections

Financial history should not be silently overwritten after finalization.

Suppose:

```text
01-Sep Full Day = ₹500
```

Later it is discovered the attendance should have been half day ₹250.

Do not simply mutate the historical ledger amount.

Preferred flow:

```text
Original earning   +₹500   APPROVED
        ↓
Reversal           -₹500   REVERSED
        ↓
Correct earning    +₹250   APPROVED
```

This preserves an audit trail.

The corrected entry should reference the original/reversed entry.

---

# 16. Worker Financial Transactions

A worker may receive money before the final payout.

Examples:

- Advance
- Partial payout
- Final payout
- Controlled adjustment

These transactions must be recorded independently from attendance earnings.

---

# 17. Worker Payments Table

## 17.1 Table: `worker_payments`

| Column | Type | Required | Notes |
|---|---|---|---|
| id | UUID | Yes | Primary key |
| organization_id | UUID | Yes | Organization |
| worker_id | UUID | Yes | Worker |
| payment_date | date | Yes | Payment date |
| payment_type | enum | Yes | ADVANCE / PAYOUT / ADJUSTMENT |
| amount | decimal(18,2) | Yes | Positive transaction amount |
| currency_id | UUID | Yes | Payment currency |
| payment_method | enum/FK | Yes | CASH/BANK_TRANSFER/UPI/CHEQUE/OTHER |
| reference_number | varchar | No | Bank/receipt/reference |
| payment_period_from | date | No | Applicable payout period |
| payment_period_to | date | No | Applicable payout period |
| status | enum | Yes | PENDING/COMPLETED/CANCELLED |
| notes | text | No | Notes |
| cancelled_at | timestamp | No | Cancellation |
| cancelled_by | UUID | No | Cancellation |
| cancellation_reason | text | No | Cancellation |
| created_at | timestamp | Yes | Audit |
| created_by | UUID | Yes | Audit |
| updated_at | timestamp | Yes | Audit |
| updated_by | UUID | Yes | Audit |

### Important

A worker does **not** have a single payment status.

Payments are transaction records because one worker may have:

- multiple advances
- multiple partial payouts
- one or more final payouts
- corrections/reversals

---

# 18. Advance Payments

An advance is money paid to a worker before all corresponding earnings are settled.

Example:

```text
Worker earns ₹12,500 during the period.
Worker received ₹3,000 as an advance.

Outstanding after considering advance:
₹12,500 - ₹3,000 = ₹9,500
```

The advance remains a financial debit against the worker until it is absorbed by earnings/payout settlement.

---

# 19. Partial Payouts

A worker may receive part of the amount currently payable.

Example:

```text
Gross Earnings       ₹12,500
Less Advance          ₹3,000
--------------------------------
Net Payable            ₹9,500

Partial payout        ₹5,000
--------------------------------
Remaining              ₹4,500
```

A later final payout of ₹4,500 settles the remaining amount.

---

# 20. Payment Allocation

To maintain a reliable financial audit trail, payments should be allocatable to earnings/settlement balances rather than only storing a worker-level total.

## 20.1 Table: `worker_payment_allocations`

Suggested columns:

| Column | Type | Required |
|---|---|---|
| id | UUID | Yes |
| organization_id | UUID | Yes |
| payment_id | UUID | Yes |
| earnings_ledger_id | UUID | No |
| amount | decimal(18,2) | Yes |
| allocation_type | enum | Yes |
| created_at | timestamp | Yes |
| created_by | UUID | Yes |

Possible allocation types:

- EARNING_SETTLEMENT
- ADVANCE_RECOVERY
- ADJUSTMENT

The exact allocation strategy may be implemented first as oldest-earned-first (FIFO) unless business requirements later demand a different settlement order.

---

# 21. Recommended Financial Calculation

For a worker and selected payout period:

```text
Gross Earned
= Sum of approved earnings ledger entries
```

Then:

```text
Outstanding
= Gross Earned
  - Applicable Advance Balance
  - Previous Payouts
  +/- Valid Adjustments
```

More generally, the worker's financial balance can be represented as:

```text
Worker Balance
= Earnings
  - Advances
  - Payouts
  +/- Adjustments
```

### Interpretation

- Positive balance → amount payable to worker.
- Negative balance → worker has received more than currently earned; balance can carry forward.
- Zero balance → settled.

---

# 22. Carry-Forward Advance Balance

If a worker receives more advance money than the earnings accumulated so far, the remaining amount must carry forward.

Example:

```text
Advance received = ₹5,000
Current approved earnings = ₹3,000

Current balance = -₹2,000
```

The ₹2,000 remains an advance balance and can be recovered from future earnings.

Do not create artificial negative attendance earnings to represent this balance.

---

# 23. Payout Status

A payout period should be able to show:

- NOT_READY
- READY_FOR_PAYOUT
- PARTIALLY_PAID
- PAID

The status is derived from earnings, approved deductions/advances, and completed payout allocations.

The worker-level history should show both the amount earned and the money already received.

---

# 24. Example End-to-End Scenario

Worker:

```text
Name: Ramesh
Gender: Male
```

Wage master:

```text
Male Full Day = ₹500
Male Half Day = ₹275
```

Attendance:

```text
01 Sep Full Day  → ₹500
02 Sep Full Day  → ₹500
03 Sep Half Day  → ₹275
04 Sep Full Day  → ₹500
05 Sep Full Day  → ₹500
```

Total earnings:

```text
₹2,275
```

Worker receives advance:

```text
₹1,000
```

Current payable:

```text
₹2,275 - ₹1,000 = ₹1,275
```

Worker receives partial payout:

```text
₹700
```

Remaining:

```text
₹1,275 - ₹700 = ₹575
```

Final payout:

```text
₹575
```

Final outstanding:

```text
₹0
```

The full financial history remains available.

---

# 25. Labor Activity Separation

Labor Activity is a separate Phase 3 component.

Example:

```text
Worker: Ramesh
Date: 01-Sep
Activity: Pruning
Area: Farm A / Area 3
Duration: 4 hours
```

This records operational work.

It must **not** automatically create:

```text
₹500 earning
```

The ₹500 must come from the worker's attendance for that date.

Therefore:

```text
Attendance → Payroll
Labor Activity → Operational work record
```

This separation is mandatory.

---

# 26. Future Attendance Integration

Phase 3.2 will introduce Labor Attendance.

Expected future flow:

```text
Worker selected
      ↓
Attendance date
      ↓
Attendance type
      ↓
Worker gender resolved
      ↓
Applicable wage rate resolved
      ↓
Wage snapshot
      ↓
Earnings Ledger entry
```

### Attendance types

Initial design supports:

- FULL_DAY
- HALF_DAY
- HOURLY
- ABSENT
- LEAVE

Only earning-producing attendance types create earnings.

ABSENT and LEAVE do not create standard wage earnings unless a future explicit paid-leave rule is added.

---

# 27. Database Relationships

High-level relationship:

```text
Organization
 ├── Workers
 │    ├── Farm Assignments
 │    ├── Attendance (Phase 3.2)
 │    ├── Earnings Ledger
 │    └── Payments
 │         └── Payment Allocations
 │
 ├── Contractors
 ├── Labor Categories
 └── Labor Wage Rates
```

Worker financial relationship:

```text
Worker
 ├── Earnings Ledger
 │      └── Attendance source
 │
 └── Worker Payments
        └── Payment Allocations
```

---

# 28. API Design

Follow existing Clean Architecture and vertical-slice conventions.

## Worker APIs

```text
GET    /api/labor/workers
GET    /api/labor/workers/{id}
POST   /api/labor/workers
PUT    /api/labor/workers/{id}
POST   /api/labor/workers/{id}/activate
POST   /api/labor/workers/{id}/deactivate
```

## Contractor APIs

```text
GET    /api/labor/contractors
GET    /api/labor/contractors/{id}
POST   /api/labor/contractors
PUT    /api/labor/contractors/{id}
POST   /api/labor/contractors/{id}/activate
POST   /api/labor/contractors/{id}/deactivate
```

## Labor Category APIs

```text
GET    /api/labor/categories
POST   /api/labor/categories
PUT    /api/labor/categories/{id}
POST   /api/labor/categories/{id}/activate
POST   /api/labor/categories/{id}/deactivate
```

## Wage Rate APIs

```text
GET    /api/labor/wage-rates
GET    /api/labor/wage-rates/{id}
POST   /api/labor/wage-rates
PUT    /api/labor/wage-rates/{id}
```

Wage history should be queryable by gender and wage type.

## Worker Farm Assignment APIs

```text
GET    /api/labor/workers/{workerId}/farms
POST   /api/labor/workers/{workerId}/farms
PUT    /api/labor/workers/{workerId}/farms/{assignmentId}
POST   /api/labor/workers/{workerId}/farms/{assignmentId}/deactivate
```

## Worker Payment APIs

```text
GET    /api/labor/workers/{workerId}/payments
POST   /api/labor/workers/{workerId}/payments
POST   /api/labor/workers/{workerId}/payments/{paymentId}/cancel
```

## Earnings Ledger APIs

Read/report foundation can be exposed in Phase 3.1, while creation will be driven by attendance in Phase 3.2.

```text
GET    /api/labor/workers/{workerId}/earnings
GET    /api/labor/workers/{workerId}/balance
GET    /api/labor/workers/{workerId}/payout-summary
```

Do not expose unrestricted client-side ledger insertion for normal earning entries. Attendance should be the controlled source of earning entries.

---

# 29. Angular Routes

## Workers

```text
/labor/workers
/labor/workers/new
/labor/workers/:id
/labor/workers/:id/edit
```

## Contractors

```text
/labor/contractors
/labor/contractors/new
/labor/contractors/:id
/labor/contractors/:id/edit
```

## Labor Categories

```text
/labor/categories
```

## Wage Rates

```text
/labor/wage-rates
/labor/wage-rates/new
/labor/wage-rates/:id/edit
```

---

# 30. Worker Details Page

Recommended tabs:

1. Overview
2. Farm Assignments
3. Applicable Wage History
4. Earnings
5. Payments
6. Financial Balance
7. Audit

Future tabs can add:

- Attendance
- Labor Activities

### Important

Because wage rates are organization-level rather than worker-specific, **Applicable Wage History** means the wage rates applicable to the worker's gender and wage types, not a worker-specific wage contract.

---

# 31. Worker List Page

Recommended columns:

- Worker Name
- Gender
- Mobile
- Labor Category
- Employment Type
- Contractor
- Assigned Farms
- Active/Inactive
- Joining Date

Filters:

- Search
- Gender
- Employment Type
- Labor Category
- Contractor
- Farm
- Active/Inactive

Do not add a Worker Code column.

---

# 32. Worker Form

### Basic Details

- First Name
- Last Name
- Display Name
- Gender
- Mobile Number
- Alternate Mobile Number

### Employment Details

- Labor Category
- Employment Type
- Contractor
- Joining Date
- Leaving Date

### Additional

- Notes
- Active

### Validation

- Required fields validated client and server side.
- CONTRACT requires Contractor.
- Leaving Date cannot precede Joining Date.
- Contractor must belong to authenticated organization.
- Category must belong to authenticated organization or be global.

---

# 33. Contractor UI

Contractor list columns:

- Name
- Contact Person
- Phone
- Email
- Worker Count
- Active/Inactive

Contractor detail should show associated workers.

Deleting contractor records is not required; use deactivate/active status.

---

# 34. Wage Rate UI

The wage rate screen should clearly display:

| Gender | Wage Type | Rate | Currency | Effective From | Effective To | Active |
|---|---|---:|---|---|---|---|
| Male | Full Day | ₹500 | INR | 01-Jul | — | Yes |
| Female | Full Day | ₹350 | INR | 01-Jul | — | Yes |

The user must be able to view historical rates.

When creating a new effective rate, the UI/API must prevent overlapping dates.

---

# 35. Payment UI

Worker payment entry should support:

- Payment date
- Payment type
- Amount
- Payment method
- Reference number
- Applicable period
- Notes

Payment type:

```text
ADVANCE
PAYOUT
ADJUSTMENT
```

A worker's balance should be visible while entering the payment.

Example:

```text
Current Earnings       ₹12,500
Existing Advances       ₹3,000
Previous Payouts        ₹2,000
--------------------------------
Outstanding             ₹7,500
```

---

# 36. Permissions

Add these permissions using the existing idempotent permission seeder.

## Worker

```text
Worker.View
Worker.Create
Worker.Update
Worker.Activate
Worker.Deactivate
```

## Contractor

```text
Contractor.View
Contractor.Create
Contractor.Update
Contractor.Activate
Contractor.Deactivate
```

## Labor Category

```text
LaborCategory.View
LaborCategory.Create
LaborCategory.Update
LaborCategory.Activate
LaborCategory.Deactivate
```

## Wage Rate

```text
WorkerWage.View
WorkerWage.Create
WorkerWage.Update
```

## Worker Payments

```text
WorkerPayment.View
WorkerPayment.Create
WorkerPayment.Cancel
```

## Earnings

```text
WorkerEarnings.View
WorkerEarnings.Approve
WorkerEarnings.Reverse
```

Existing permissions must not be deleted or modified by the seeder.

---

# 37. Business Rules Summary

1. Worker belongs to exactly one organization.
2. Worker has no business code.
3. Contractor has no business code.
4. Worker contains gender.
5. Gender may affect wage rate.
6. Wage rates are organization-level.
7. Wage rates are effective-dated.
8. Wage periods cannot overlap for the same organization + gender + wage type.
9. No overtime exists.
10. Worker can belong to multiple farms.
11. Worker-to-farm relationship uses an assignment table.
12. Attendance is the sole source of wage earnings.
13. Labor activities do not generate payroll.
14. Finalized earnings preserve the wage snapshot.
15. Historical earnings must not change when wage masters change.
16. Financial corrections use reversal/correction entries rather than silent mutation.
17. Worker payments are separate transactions.
18. Advances reduce future payable balance.
19. Partial payouts reduce outstanding payable balance.
20. Final payout brings the applicable balance to zero when fully settled.
21. Excess advance creates a carried-forward negative worker balance.
22. Payment allocations provide transaction-level settlement traceability.
23. Historical records are never hard-deleted.
24. Active/inactive flags are used instead of hard deletion.
25. All financial records require audit fields.
26. Organization isolation is enforced server-side.

---

# 38. Indexes and Database Integrity

Recommended indexes:

### workers

```text
(organization_id, display_name)
(organization_id, mobile_number)
(organization_id, is_active)
(organization_id, contractor_id)
```

### worker_farm_assignments

```text
(organization_id, worker_id)
(organization_id, farm_id)
(organization_id, worker_id, is_active)
```

### labor_wage_rates

```text
(organization_id, gender, wage_type, effective_from)
(organization_id, gender, wage_type, is_active)
```

Database/application validation must prevent overlapping effective periods.

### worker_earnings_ledger

```text
(organization_id, worker_id, earnings_date)
(organization_id, worker_id, status)
(organization_id, attendance_id)
```

### worker_payments

```text
(organization_id, worker_id, payment_date)
(organization_id, worker_id, status)
```

---

# 39. Transactional Consistency

Attendance finalization and earnings creation in Phase 3.2 should be transactional.

Example:

```text
Begin transaction
  Finalize attendance
  Resolve wage rate
  Create earnings ledger entry
Commit
```

If earnings creation fails, attendance finalization must not partially commit.

Likewise, payment creation plus payment allocation should be atomic.

---

# 40. Audit Requirements

Audit information must capture:

- Who created the worker
- Who updated the worker
- Who activated/deactivated the worker
- Who changed wage rates
- Who created/cancelled payments
- Who approved earnings
- Who reversed earnings
- When each action occurred

Do not log sensitive authentication secrets.

---

# 41. Error Handling

Follow the existing global exception/error response model.

Expected validation errors include:

```text
Worker not found
Contractor not found
Contractor belongs to another organization
Invalid employment type
Contractor required for contract worker
Invalid joining/leaving dates
Farm assignment invalid
Overlapping wage rate period
Invalid wage rate
Inactive worker cannot be used
Payment amount must be positive
Cannot cancel already cancelled payment
Invalid payment allocation
Insufficient allocatable balance
```

---

# 42. Implementation Order

Implement vertically:

## Step 1 — Backend database/domain foundation

- Worker entity
- Contractor entity
- LaborCategory entity
- WorkerFarmAssignment entity
- LaborWageRate entity
- WorkerPayment entity
- WorkerEarningsLedger entity
- WorkerPaymentAllocation entity
- EF configurations
- migrations
- organization scoping

## Step 2 — Permissions

Add and seed all Phase 3.1 permissions idempotently.

## Step 3 — Contractor page

Complete list/create/edit/detail/activate/deactivate.

## Step 4 — Labor Category page

Complete list/create/edit/activate/deactivate.

## Step 5 — Wage Rate page

Complete effective-dated rate management and overlap validation.

## Step 6 — Worker Master

Complete worker list/create/edit/detail/activation/deactivation.

## Step 7 — Farm Assignments

Add multiple farm assignments from Worker Details.

## Step 8 — Payment foundation

Add advance/payment entry and history.

## Step 9 — Earnings visibility

Implement earnings/balance read models and UI foundation.

Actual attendance-driven earning creation begins in Phase 3.2.

---

# 43. Docker / Deployment Requirements

The implementation must continue to work through the existing Docker Compose environment.

Validate:

```text
PostgreSQL
API
Angular
EF migrations
Seed data
Authentication
Authorization
```

Do not introduce environment-specific manual database changes.

All schema changes must be represented by EF migrations and be reproducible in the container environment.

---

# 44. Testing / Verification Strategy

As already established for the current project phases, do not create unit/integration test suites unless explicitly requested.

Perform manual/API/UI verification for:

### Worker
- Create
- Edit
- Activate/deactivate
- Organization isolation
- Contract worker validation

### Contractor
- CRUD-like workflow
- Activate/deactivate
- Organization isolation

### Wage Rate
- Create new rate
- Effective date validation
- Overlap rejection
- Historical rate display

### Farm assignment
- Multiple farms per worker
- Assignment date validation
- Organization isolation

### Payments
- Record advance
- Record partial payout
- Record final payout
- Cancel payment
- View history

### Earnings
- Verify calculated earnings read model
- Verify worker balance
- Verify historical wage snapshot behavior
- Verify correction/reversal behavior when implemented

---

# 45. Acceptance Criteria

Phase 3.1 is complete when:

- [ ] Worker Master is working end-to-end.
- [ ] Worker has gender.
- [ ] Worker has no code field.
- [ ] Contractor has no code field.
- [ ] Contractors are supported.
- [ ] Labor categories are supported.
- [ ] Worker can be assigned to multiple farms.
- [ ] Organization isolation is enforced.
- [ ] Wage rates are organization-level.
- [ ] Wage rates support gender and wage type.
- [ ] Wage rates have effective dates.
- [ ] Overlapping wage periods are rejected.
- [ ] Historical wage records remain available.
- [ ] No overtime logic exists.
- [ ] Payment/advance transactions are supported.
- [ ] Partial payouts are supported.
- [ ] Final payouts are supported.
- [ ] Payment history is visible.
- [ ] Earnings ledger tables/models exist.
- [ ] Earnings are designed to originate from attendance only.
- [ ] Worker balance can represent earnings minus advances/payouts.
- [ ] Advance balances can carry forward.
- [ ] Payment allocations can trace settlement.
- [ ] Historical earnings retain the wage snapshot.
- [ ] Reversal/correction pattern is defined.
- [ ] Permissions are seeded idempotently.
- [ ] Docker environment remains functional.
- [ ] No hard delete is used for labor business records.

---

# 46. Final Payroll Architecture

The final intended architecture is:

```text
                         ┌──────────────────┐
                         │      WORKER      │
                         └────────┬─────────┘
                                  │
                     ┌────────────┼────────────┐
                     │            │            │
                     ▼            ▼            ▼
                 Farm         Gender       Contractor
              Assignments                    
                     │
                     ▼
               ATTENDANCE
              (Phase 3.2)
                     │
                     │ sole wage source
                     ▼
          ┌──────────────────────┐
          │ WORKER EARNINGS      │
          │ LEDGER               │
          │                      │
          │ + Earnings           │
          │ + Reversals          │
          │ + Adjustments        │
          └──────────┬───────────┘
                     │
            ┌────────┴────────┐
            │                 │
            ▼                 ▼
        Advances          Payouts
            │                 │
            └────────┬────────┘
                     ▼
          Payment Allocations
                     │
                     ▼
          ┌──────────────────────┐
          │ OUTSTANDING BALANCE  │
          │                      │
          │ Gross Earnings       │
          │ - Advances           │
          │ - Previous Payouts   │
          │ +/- Adjustments      │
          └──────────────────────┘
```

## Final rule

**Attendance determines what the worker earned.**

**Advances and payouts determine what the worker has already received.**

**The difference determines what remains payable.**

Labor Activity remains operational data and does not participate in wage calculation.

---

# 47. Future Extension Points

The design intentionally leaves room for future additions without changing the core payroll model:

- Paid leave
- Holiday pay
- Overtime, if ever required
- Bonus
- Deductions
- Statutory deductions
- Contractor-level settlement
- Payroll periods
- Payroll approval batches
- Payslips
- Bank payment files
- Mobile worker self-service
- Attendance device integration
- Biometric integration
- Multi-currency, if business requirements expand

These must be added as explicit financial concepts rather than by weakening the attendance → earnings → settlement separation.

---

## Final Design Decision

The Phase 3 labor model is intentionally built around three independent concerns:

```text
1. Operational Work
   Labor Activity

2. Earned Compensation
   Attendance → Earnings Ledger

3. Money Settlement
   Advances + Payouts + Allocations
```

This separation gives the platform a clear audit trail, supports partial payouts and advances, preserves historical wage calculations, and allows the same payroll foundation to work across farms and future crops.
