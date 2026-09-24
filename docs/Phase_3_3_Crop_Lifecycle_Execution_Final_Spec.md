# Phase 3.3 — Crop Lifecycle Execution Final Specification

## 1. Purpose

Phase 3.3 introduces configurable crop lifecycle templates and executes those templates against Crop Cycles.

The lifecycle provides the planned and actual progression of a Crop Cycle through agricultural stages while preserving historical data. It is designed for all crops, with an initial seeded grape lifecycle.

This phase does **not** implement Labor Activity. It only prepares the Crop Cycle Stage reference needed by future activity modules.

## 2. Scope

### In scope
- Crop Lifecycle Template master
- Crop Lifecycle Stage master
- Crop Cycle lifecycle template selection
- Crop Cycle start
- Generation of Crop Cycle Stage snapshots
- Planned stage dates
- Actual stage dates
- Stage progression
- Complete, skip, reopen and override actions
- Lifecycle progress UI
- Perennial/non-perennial plantation behavior
- Lifecycle permissions and audit
- Database migration and seed data
- Organization isolation
- API and Angular UI

### Out of scope
- Labor Activity
- Spray/Fertilizer/Irrigation/Disease modules
- Harvest implementation
- Payroll calculation
- Automatic stage completion from dates
- Variety-specific lifecycle templates
- Stage dependency tables

## 3. Existing Architecture Context

The application uses:
- Angular web client
- ASP.NET Core/.NET API
- PostgreSQL
- Docker for local development/testing

Existing Farm → Farm Area → Plantation → Crop Cycle hierarchy remains unchanged except for the lifecycle additions described below.

Organization is always derived from the authenticated context on the server. Client-supplied organization IDs must not be trusted.

## 4. Crop Lifecycle Template

A lifecycle template belongs to an organization and a crop.

Existing fields:

| Field | Notes |
|---|---|
| id | UUID |
| organization_id | Organization owner |
| crop_id | Crop master reference |
| name | Template name |
| description | Optional |
| is_default | Default template for crop |
| is_system | System-provided/protected template |
| is_active | Available for selection |
| created_at | Audit |
| created_by | Audit |
| updated_at | Audit |
| updated_by | Audit |

Multiple templates are allowed for the same crop.

An active default template is automatically selected when creating a Crop Cycle, but the user can select another active template.

System templates cannot be deleted or modified in ways that violate existing project rules.

## 5. Crop Lifecycle Stage

Existing `crop_lifecycle_stages` is retained as the template-stage definition.

### Required change

Remove:

```text
stage_code
```

No replacement business code is required.

Add:

```text
expected_duration_days INTEGER NULL
```

Core fields:

| Field | Notes |
|---|---|
| id | UUID |
| lifecycle_template_id | FK |
| stage_name | Required |
| sequence_number | Required ordering |
| description | Optional |
| expected_duration_days | Optional planning duration |
| is_active | Active template stage |
| created_at/created_by | Audit |
| updated_at/updated_by | Audit |

`sequence_number` determines lifecycle order.

## 6. Crop Cycle Changes

Existing `crop_cycles` does not currently contain a lifecycle template reference.

Add:

```text
lifecycle_template_id UUID NULL
```

Foreign key:

```text
crop_lifecycle_templates.id
```

A draft Crop Cycle may exist without a lifecycle template.

A Crop Cycle cannot be started without an active lifecycle template.

Once the Crop Cycle has started, its lifecycle template is locked.

## 7. Crop Cycle Stage Snapshot

Create:

```text
crop_cycle_stages
```

Fields:

| Field | Type | Purpose |
|---|---|---|
| id | UUID | Primary key |
| crop_cycle_id | UUID | Crop Cycle |
| lifecycle_template_stage_id | UUID | Source template stage |
| stage_name | string | Historical snapshot |
| sequence_number | integer | Historical order |
| expected_duration_days | integer nullable | Historical planning duration |
| planned_start_date | date nullable | Planned start |
| planned_end_date | date nullable | Planned end |
| actual_start_date | date nullable | Actual start |
| actual_end_date | date nullable | Actual completion |
| status | string/enum | Lifecycle status |
| notes | text nullable | Stage notes |
| created_at/created_by | audit | Audit |
| updated_at/updated_by | audit | Audit |

Do not add `stage_code`.

### Constraints

Recommended:

```text
UNIQUE(crop_cycle_id, lifecycle_template_stage_id)
UNIQUE(crop_cycle_id, sequence_number)
```

Foreign keys and indexes must follow the project's existing conventions.

## 8. Stage Status

Supported statuses:

```text
NOT_STARTED
IN_PROGRESS
COMPLETED
SKIPPED
CANCELLED
```

No separate status master table is required.

## 9. Lifecycle Template Selection

During Crop Cycle creation:

1. User selects Plantation.
2. Crop and relevant plantation context are displayed read-only.
3. User selects season/year and planned start date.
4. System loads active lifecycle templates for the Crop.
5. Default active template is preselected when available.
6. User may select another active template.
7. Expected cycle end is calculated from lifecycle duration.
8. User can save the Crop Cycle as draft.

The lifecycle template may be changed while the Crop Cycle is still draft/not started.

Once started, the lifecycle template is locked.

## 10. Starting a Crop Cycle

Stages are **not** generated when a draft Crop Cycle is created.

Stages are generated only when the user starts the Crop Cycle.

Start operation must be transactional:

1. Validate Crop Cycle.
2. Validate lifecycle template.
3. Validate template belongs to the correct organization and crop.
4. Validate template is active.
5. Validate Crop Cycle is eligible to start.
6. Read active template stages ordered by `sequence_number`.
7. Create `crop_cycle_stages`.
8. Snapshot stage name, sequence and expected duration.
9. Calculate planned dates.
10. Set first stage to `IN_PROGRESS`.
11. Set its actual start date.
12. Set Crop Cycle to its active/started status.
13. Persist everything in one transaction.

If any step fails, no partial lifecycle start is committed.

## 11. Planned Date Calculation

For a lifecycle starting on date `D`:

- First stage planned start = `D`
- Stage planned end is calculated from `expected_duration_days`
- Next stage planned start follows the previous planned end
- Total expected duration is the sum of stage durations

If a duration is null, the system must follow a clearly defined project convention. Recommended behavior: do not invent duration; leave dependent planned dates unresolved or require duration before start if the project requires complete planning.

Planned dates are planning values only.

Changing planned dates does not automatically change actual dates.

The expected cycle end does not automatically complete the Crop Cycle.

## 12. Stage Progression

Stages progress sequentially by `sequence_number`.

When the current stage is completed:

1. Set actual end date.
2. Set status to `COMPLETED`.
3. Locate the next active stage.
4. Set next stage to `IN_PROGRESS`.
5. Set next stage actual start date.
6. Persist transactionally.

Dates alone never auto-progress a stage.

Explicit user action is required.

## 13. Stage Completion

Completing a stage requires:

- Stage is `IN_PROGRESS`
- User has required permission
- Actual completion date is valid
- Current stage rules are satisfied

The completion date defaults to today but can follow existing project date-entry conventions.

Future stages cannot normally be completed directly.

## 14. Skip Stage

A stage can be skipped because real farm conditions can differ from the template.

Skip requires:

- Permission
- Mandatory reason
- Audit record

Recommended behavior:

- current stage becomes `SKIPPED`
- next stage becomes `IN_PROGRESS`
- actual dates are recorded according to project convention
- skipped stage remains visible historically

## 15. Reopen Stage

A completed stage may be reopened through a controlled action.

Requirements:

- Permission
- Mandatory reason
- Audit

The implementation must define how the current active stage is handled so that there is never an invalid state with multiple active stages.

Recommended approach:
- reopen the selected stage as `IN_PROGRESS`
- preserve historical completion information through audit
- move/reconcile the later stage according to the existing lifecycle service rules

## 16. Override

A controlled override is allowed for exceptional agricultural situations.

Override requires:

- Permission
- Mandatory reason
- Audit
- Explicit target stage/status/date values

The system must never silently bypass normal sequential rules.

## 17. Planned vs Actual Dates

These are independent.

### Planned
Used for planning and expected timeline.

### Actual
Used for what really happened.

Changing planned dates must not rewrite actual dates.

Late or early execution is valid.

## 18. Crop Lifecycle Template Updates

Updating a lifecycle template must never mutate existing Crop Cycle history.

Existing Crop Cycles use `crop_cycle_stages` snapshots.

Therefore:

```text
Template change
    ↓
future Crop Cycles use new definition
    ↓
existing Crop Cycles remain unchanged
```

A started Crop Cycle cannot switch templates.

## 19. Perennial vs Non-Perennial

Crop Master `duration_type` controls the plantation lifecycle behavior.

### Perennial

Example: grapes.

Flow:

```text
Plantation
  ↓
Crop Cycle 2026-27
  ↓
Lifecycle stages
  ↓
Harvest
  ↓
Crop Cycle completed
  ↓
Plantation remains ACTIVE
  ↓
New Crop Cycle on same Plantation
```

A perennial Plantation can have multiple Crop Cycles over time.

### Non-perennial / Annual / Seasonal

Example: tomato.

Normal flow:

```text
Plantation
  ↓
Crop Cycle
  ↓
Lifecycle
  ↓
Harvest
  ↓
Crop Cycle completed
  ↓
User is prompted to terminate Plantation
```

If the user confirms termination:

```text
Old Plantation → TERMINATED
Farm Area remains reusable
Next planting → NEW Plantation → NEW Crop Cycle
```

Do not automatically terminate the Plantation without user confirmation.

Do not reuse the old non-perennial Plantation for the next planting because historical cost, yield, disease, spray and other operational records must remain separated.

Existing plantation termination reasons are reused.

## 20. Expected Cycle End

`crop_cycles.expected_end_date` should be calculated from the lifecycle's expected duration where sufficient duration data exists.

It is a planning value.

It must not automatically complete the Crop Cycle.

## 21. Seed Data

Seed an idempotent system/default active lifecycle:

```text
Grape Standard Lifecycle
```

Suggested stages:

| Sequence | Stage | Days |
|---:|---|---:|
| 1 | Dormancy | 30 |
| 2 | Pruning | 15 |
| 3 | Bud Break | 10 |
| 4 | Shoot Development | 20 |
| 5 | Flowering | 7 |
| 6 | Fruit Set | 10 |
| 7 | Berry Development | 30 |
| 8 | Ripening | 25 |
| 9 | Harvest | 15 |

These values are configurable seed defaults, not fixed agronomic rules.

If other crops already exist and project seed conventions support it, additional templates may be seeded without inventing unnecessary agricultural assumptions.

Seeder requirements:
- idempotent
- never delete admin changes
- never overwrite customized templates

## 22. Permissions

Lifecycle template permissions:

```text
CropLifecycleTemplate.View
CropLifecycleTemplate.Create
CropLifecycleTemplate.Update
CropLifecycleTemplate.Activate
CropLifecycleTemplate.Deactivate
```

Crop Cycle lifecycle permissions:

```text
CropCycleLifecycle.View
CropCycleLifecycle.Start
CropCycleLifecycle.UpdateStage
CropCycleLifecycle.SkipStage
CropCycleLifecycle.OverrideStage
```

Use the actual permission naming convention already present in the project if it differs.

System templates must be protected according to existing authorization rules.

## 23. Audit

Audit is required for:

- template selection
- Crop Cycle start
- stage start
- stage completion
- stage skip
- stage reopen
- stage override
- template create/update/activation/deactivation
- planned date changes where the existing audit model supports them

Use the existing audit infrastructure. Do not create a parallel audit framework.

## 24. Organization Isolation

Every query and mutation must enforce organization ownership.

Examples:

- Template belongs to authenticated organization.
- Crop belongs to authenticated organization.
- Crop Cycle belongs to authenticated organization.
- Plantation belongs to authenticated organization.
- Template selected for a Crop Cycle must belong to the same organization.
- Cross-organization IDs must return the project's standard not-found/forbidden behavior.

Never trust `organization_id` sent from Angular.

## 25. API Contracts

Routes should follow existing project conventions. Suggested conceptual endpoints:

```text
GET    /api/crop-lifecycle-templates
GET    /api/crop-lifecycle-templates/{id}
POST   /api/crop-lifecycle-templates
PUT    /api/crop-lifecycle-templates/{id}
POST   /api/crop-lifecycle-templates/{id}/activate
POST   /api/crop-lifecycle-templates/{id}/deactivate

GET    /api/crop-cycles/{id}/lifecycle
POST   /api/crop-cycles/{id}/start

GET    /api/crop-cycle-stages/{id}
POST   /api/crop-cycle-stages/{id}/complete
POST   /api/crop-cycle-stages/{id}/skip
POST   /api/crop-cycle-stages/{id}/reopen
POST   /api/crop-cycle-stages/{id}/override

PUT    /api/crop-cycle-stages/{id}/planned-dates
```

Do not create duplicate controller/service patterns if equivalent project infrastructure already exists.

## 26. Angular UI

### Lifecycle Template List
Show:
- Crop
- Template name
- Default
- System
- Active
- Number of stages
- Actions

### Lifecycle Template Detail
Show:
- Crop
- Name
- Description
- Status
- Default/system indicators
- Ordered stage list
- Stage name
- Sequence
- Expected duration
- Active flag

Actions:
- Create
- Edit
- Activate
- Deactivate

### Lifecycle Template Create/Edit
Allow:
- Crop
- Name
- Description
- Default
- Active
- Ordered stages
- Expected duration
- Stage descriptions

Do not expose `stage_code`.

### Crop Cycle Create/Edit
Show:
- Plantation
- Crop/variety context
- Season
- Season year
- Planned start date
- Lifecycle Template
- Expected End Date

Lifecycle template is selectable while draft and locked after start.

### Crop Cycle Lifecycle Tab
Show:
- Template
- Current stage
- Progress
- Overall status
- Planned timeline
- Actual timeline
- Stage list
- Sequence
- Status
- Planned start/end
- Actual start/end
- Expected duration
- Notes

Actions:
- Start
- Complete
- Skip
- Reopen
- Override

Only show actions allowed by permissions and current stage state.

## 27. Future Activity Integration

Phase 3.3 does not implement Labor Activity.

The lifecycle model must, however, make this future relationship possible:

```text
Crop Cycle
   ↓
Crop Cycle Stage
   ↓
Labor Activity / Spray / Fertilizer / Irrigation / Disease / Harvest
```

Lifecycle stages must not dictate or forbid activity types.

Future modules should reference `crop_cycle_stage_id` where appropriate.

Payroll remains independent:

```text
Attendance → Earnings Ledger
Lifecycle Stage → Operations
```

Labor Activity must not calculate payroll.

## 28. Acceptance Criteria

Phase 3.3 is complete when:

1. Lifecycle template master works end-to-end.
2. `stage_code` is removed.
3. `expected_duration_days` exists.
4. Crop Cycle can select an active template.
5. Draft Crop Cycle can exist without a template.
6. Started Crop Cycle always has a valid template.
7. Template is locked after start.
8. Starting a Crop Cycle creates snapshot stages transactionally.
9. First stage becomes `IN_PROGRESS`.
10. Planned dates are calculated.
11. Actual dates remain independent.
12. Stage completion advances to the next stage.
13. Skip requires a reason.
14. Reopen requires a reason.
15. Override requires a reason and permission.
16. Existing Crop Cycles are unaffected by template edits.
17. Perennial Plantation remains reusable across cycles.
18. Non-perennial Plantation termination is user-confirmed.
19. New non-perennial planting uses a new Plantation.
20. Organization isolation is enforced.
21. Audit is recorded.
22. Angular lifecycle screens are permission-aware.
23. Docker migration/seeding works.
24. No Labor Activity implementation is included in this phase.

## 29. Manual Verification

At minimum verify:

### Template
- Create template.
- Add/reorder stages.
- Edit expected durations.
- Activate/deactivate.
- Set default.

### Crop Cycle
- Create draft without template.
- Select template.
- Change template while draft.
- Start cycle.
- Verify stage snapshots.
- Verify first stage is in progress.
- Verify planned dates.

### Progression
- Complete current stage.
- Verify next stage starts.
- Skip stage with/without reason.
- Reopen with/without reason.
- Override with/without permission/reason.

### Historical integrity
- Edit template after cycle start.
- Verify existing cycle stages do not change.

### Plantation behavior
- Complete perennial cycle and verify plantation remains active.
- Complete non-perennial cycle and verify termination prompt.
- Terminate and create new plantation on same Farm Area.

### Security
- Try cross-organization IDs.
- Try unauthorized lifecycle actions.
- Verify audit.

### Docker
- Run migration.
- Run seed.
- Start API and Angular.
- Verify the application manually.

