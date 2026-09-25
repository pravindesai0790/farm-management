# Phase 3.3 — AI Agent Implementation Workflow

## 1. Goal

Implement Phase 3.3 Crop Lifecycle Execution as a sequence of small vertical slices.

The preferred approach is:

```text
Recon → DB/domain → migration → seed → template UI → Crop Cycle lifecycle → progression → plantation behavior → verification
```

Do not implement all APIs first and all UI later.

Each agent task must:
1. inspect existing code first;
2. reuse project conventions;
3. make the smallest required change;
4. build/run the affected area;
5. report changed files and verification;
6. stop at the defined gate.

Do not add unit/integration tests unless explicitly requested. Use manual/API/Docker verification.

---

# 2. Global AI Agent Rules

Use these rules in every prompt:

- Do not invent a new architecture.
- Inspect the repository before modifying it.
- Reuse existing entity, repository, service, controller, authorization, audit, migration and Angular patterns.
- Do not create duplicate abstractions.
- Do not add business code fields such as `stage_code`.
- Do not add variety-specific lifecycle behavior in this phase.
- Do not implement Labor Activity.
- Do not implement payroll logic.
- Preserve organization isolation.
- Never trust organization IDs from the client.
- Preserve historical Crop Cycle data.
- Do not silently change existing behavior outside Phase 3.3.
- Keep changes backward-compatible with completed Phase 1, Phase 2 and Phase 3.1/3.2.
- After each slice, build the affected backend/frontend and fix compile errors before continuing.
- Do not proceed to the next slice if the current gate fails.

---

# 3. Agent 0 — Repository Reconnaissance

## Prompt

```text
You are implementing Phase 3.3 Crop Lifecycle Execution in an existing Angular + ASP.NET Core + PostgreSQL farm-management application.

Before changing any code, inspect the repository.

Find and document:
1. Crop entity/master.
2. Farm, FarmArea, Plantation and CropCycle entities.
3. Existing crop_lifecycle_templates.
4. Existing crop_lifecycle_stages.
5. Existing Crop Cycle screens/routes/services.
6. Existing migration strategy.
7. Existing seed strategy.
8. Existing authorization/permission model.
9. Existing audit implementation.
10. Existing organization isolation implementation.
11. Existing status enum/conventions.
12. Existing Angular CRUD/list/detail page patterns.
13. Existing modal/dialog/form validation patterns.

Important locked requirements:
- Remove stage_code from crop_lifecycle_stages.
- Add expected_duration_days.
- Add lifecycle_template_id to crop_cycles.
- Create crop_cycle_stages.
- Do not add stage_code anywhere.
- Lifecycle stages are generated when a Crop Cycle starts, not when a draft is created.
- Crop Cycle template is locked after start.
- No Labor Activity in Phase 3.3.

Do not modify code yet.

Return:
- relevant files
- relevant classes
- relevant database tables
- relevant routes
- relevant Angular pages/services
- recommended files to change
- any conflicts with the Phase 3.3 requirements
```

## Gate

Agent must return repository facts and proposed files only.

---

# 4. Agent 1 — Database/Domain Model

## Prompt

```text
Implement the Phase 3.3 database/domain model changes using the repository's existing patterns.

Required changes:

1. crop_lifecycle_stages
   - remove stage_code
   - add expected_duration_days INTEGER NULL

2. crop_cycles
   - add lifecycle_template_id UUID NULL
   - FK to crop_lifecycle_templates

3. create crop_cycle_stages:
   - id UUID
   - crop_cycle_id UUID
   - lifecycle_template_stage_id UUID
   - stage_name
   - sequence_number
   - expected_duration_days nullable
   - planned_start_date nullable
   - planned_end_date nullable
   - actual_start_date nullable
   - actual_end_date nullable
   - status
   - notes nullable
   - created_at/created_by
   - updated_at/updated_by

4. Add:
   UNIQUE(crop_cycle_id, lifecycle_template_stage_id)
   UNIQUE(crop_cycle_id, sequence_number)

5. Add required indexes and foreign keys using existing project conventions.

6. Do not add stage_code.

7. Do not add crop_variety_id to lifecycle templates.

8. Do not create a lifecycle status master table.

9. Reuse existing audit/base entity conventions.

10. Preserve existing data during migration.

Inspect existing migrations and entities before implementation.
```

## Gate

- Backend compiles.
- ORM model is valid.
- No `stage_code`.
- Migration is not yet required unless the project requires model-generated migration at this stage.

---

# 5. Agent 2 — Database Migration

## Prompt

```text
Create the PostgreSQL migration for Phase 3.3.

Requirements:
- Safely remove crop_lifecycle_stages.stage_code.
- Add crop_lifecycle_stages.expected_duration_days.
- Add crop_cycles.lifecycle_template_id nullable.
- Add crop_cycle_stages.
- Add foreign keys.
- Add unique constraints.
- Add indexes.
- Follow existing migration naming and structure.
- Do not destroy unrelated data.
- Do not create duplicate tables.
- Do not modify unrelated schema.

Run the migration against the Docker PostgreSQL database if available.

Report:
- migration name
- affected tables
- successful migration result
- rollback/down migration considerations if supported
```

## Gate

Migration applies successfully on the current database.

---

# 6. Agent 3 — Lifecycle Seed Data

## Prompt

```text
Implement idempotent Phase 3.3 lifecycle seed data.

Create:
Grape Standard Lifecycle

Stages:
1. Dormancy — 30 days
2. Pruning — 15 days
3. Bud Break — 10 days
4. Shoot Development — 20 days
5. Flowering — 7 days
6. Fruit Set — 10 days
7. Berry Development — 30 days
8. Ripening — 25 days
9. Harvest — 15 days

Requirements:
- system template
- active
- default for Grape
- idempotent
- never overwrite administrator customizations
- never delete existing templates
- follow existing crop lookup and seed conventions
- do not create stage_code
- if Grape does not exist, follow existing project seeding rules instead of inventing an identifier

Run the seeder and verify the records.
```

## Gate

Seed runs repeatedly without duplicates or overwriting changes.

---

# 7. Agent 4 — Lifecycle Template Backend Vertical Slice

## Prompt

```text
Implement the Crop Lifecycle Template backend vertical slice.

First inspect existing CRUD patterns.

Implement:
- list
- detail
- create
- update
- activate
- deactivate

Include stage management:
- ordered stages
- stage name
- sequence
- expected duration
- description
- active state

Rules:
- organization isolation
- only templates for the authenticated organization
- crop must belong to the organization
- system template protection
- no stage_code
- no variety-specific lifecycle
- default template validation
- prevent invalid duplicate sequence values
- prevent invalid duplicate template/stage relationships according to existing constraints
- use existing validation/error patterns
- use existing audit infrastructure
- use existing authorization.

Follow coding rules: 
- Do reuse code if requirement meet. 
- Maintain code standerd. 
- Create DTO with required properties only.
- Create helper class if and keep commom code if exist. don't duplicate logic.
- If API endpoint request parameter grows more than 4 than create object and use it

Use project route conventions rather than blindly copying the suggested routes.
```

## Gate

Verify CRUD using Swagger/API or existing API tooling.

---

# 8. Agent 5 — Lifecycle Template Angular UI

## Prompt

```text
Implement the Crop Lifecycle Template Angular UI as a complete vertical slice.

First inspect existing Angular master-data CRUD pages and reuse their patterns.

Create:
1. Template list page
2. Template detail page
3. Create page/dialog
4. Edit page/dialog
5. Stage editor
6. Activate/deactivate actions

List columns:
- Crop
- Template Name
- Default
- System
- Active
- Stage Count
- Actions

Stage editor:
- stage name
- sequence
- expected duration
- description
- active
- reorder support

Do not display or accept stage_code.

Respect permissions.

Use existing form validation, dialogs, notifications, routing and API service patterns.

Do not implement unrelated screens.
```

## Gate

Manually create, edit, reorder, activate/deactivate a template.

---

# 9. Agent 6 — Crop Cycle Create/Edit Lifecycle Selection

## Prompt

```text
Add lifecycle template selection to the existing Crop Cycle create/edit flow.

Requirements:
- load active lifecycle templates for the selected crop
- automatically select active default template when available
- allow user to select another active template
- allow draft Crop Cycle to be saved without a template
- lifecycle template can be changed only while draft/not started
- show selected lifecycle template after save
- calculate expected end date from lifecycle duration where possible
- do not manually enter lifecycle stages on Crop Cycle creation
- do not add stage_code
- preserve existing Crop Cycle behavior

On edit:
- if Crop Cycle has started, lifecycle template selector is read-only
- if draft, selector remains editable

Use existing Angular form and API patterns.
```

## Gate

Create a draft, change template, save, reopen, and verify the selection.

---

# 10. Agent 7 — Crop Cycle Start Backend Vertical Slice

## Prompt

```text
Implement Crop Cycle Start with lifecycle stage generation.

The operation must be transactional.

Validation:
1. Crop Cycle exists in current organization.
2. Crop Cycle is eligible to start.
3. lifecycle_template_id exists.
4. template belongs to same organization.
5. template crop matches Crop Cycle crop.
6. template is active.
7. template has valid active stages.
8. sequence numbers are valid.

On successful start:
1. load active template stages ordered by sequence_number
2. create one crop_cycle_stage per template stage
3. snapshot stage_name
4. snapshot sequence_number
5. snapshot expected_duration_days
6. calculate planned dates
7. first stage = IN_PROGRESS
8. first stage actual_start_date = Crop Cycle start date
9. update Crop Cycle started/active status
10. update actual_start_date
11. persist all changes transactionally
12. create audit entries

Do not generate stages during draft creation.

If anything fails, roll back the entire operation.

Reuse existing transaction, service, status and audit patterns.
```

## Gate

Start one Crop Cycle and verify all stages were generated exactly once.

---

# 11. Agent 8 — Crop Cycle Lifecycle Read UI

## Prompt

```text
Add the Lifecycle tab/section to Crop Cycle Details.

Display:
- lifecycle template
- current stage
- progress percentage/count
- overall Crop Cycle status
- all stages ordered by sequence
- stage status
- expected duration
- planned start/end
- actual start/end
- notes

Use existing detail-page layout and component conventions.

Make the UI permission-aware.

Do not add Labor Activity.
```

## Gate

Open an active Crop Cycle and verify lifecycle data visually.

---

# 12. Agent 9 — Stage Progression Backend

## Prompt

```text
Implement Crop Cycle Stage progression.

Actions:
- complete
- skip
- reopen
- override

Complete:
- only current IN_PROGRESS stage may normally be completed
- set actual_end_date
- set COMPLETED
- start next stage
- next stage becomes IN_PROGRESS
- next stage actual_start_date is set
- perform transactionally

Skip:
- mandatory reason
- permission required
- stage becomes SKIPPED
- advance to next stage
- audit reason

Reopen:
- mandatory reason
- permission required
- reconcile later stages so lifecycle has one valid active stage
- audit reason

Override:
- permission required
- mandatory reason
- explicit target state/date
- audit
- preserve valid lifecycle state

Never auto-progress based only on planned dates.

Never allow multiple IN_PROGRESS stages.

Use existing service, validation, authorization and audit patterns.
```

## Gate

Manually execute complete, skip, reopen and override scenarios.

---

# 13. Agent 10 — Stage Actions Angular UI

## Prompt

```text
Implement stage lifecycle actions in the Crop Cycle Lifecycle UI.

Actions:
- Complete
- Skip
- Reopen
- Override

Rules:
- only show actions allowed by permission and current state
- Skip requires reason dialog
- Reopen requires reason dialog
- Override requires reason plus explicit values according to backend contract
- show validation errors returned by API
- refresh lifecycle state after successful action
- disable actions while request is processing
- use existing confirmation/dialog/toast patterns

Do not bypass backend authorization.
```

## Gate

Test every action from the UI, including validation failures.

---

# 14. Agent 11 — Planned Date Editing

## Prompt

```text
Implement controlled editing of Crop Cycle Stage planned dates.

Rules:
- planned dates are separate from actual dates
- editing planned dates does not alter actual dates
- do not automatically complete stages
- maintain valid date relationships
- respect permissions
- audit changes using existing infrastructure
- do not alter historical actual dates

Use existing date controls and validation conventions.
```

## Gate

Change planned dates on an active cycle and verify actual dates remain unchanged.

---

# 15. Agent 12 — Perennial/Non-Perennial Plantation Behavior

## Prompt

```text
Implement Phase 3.3 plantation behavior at Crop Cycle completion.

Use Crop Master duration_type.

Perennial:
- completing a Crop Cycle does not terminate Plantation
- Plantation remains available for a future Crop Cycle

Non-perennial/annual/seasonal:
- after Crop Cycle completion, prompt the user whether the Plantation should be terminated
- do not automatically terminate without user confirmation
- if confirmed, use the existing plantation termination flow/reasons
- Farm Area remains reusable
- next planting must use a NEW Plantation and NEW Crop Cycle

Do not duplicate plantation termination logic.
Reuse existing services and UI.

Do not implement harvest in this slice.
```

## Gate

Verify one perennial and one non-perennial scenario.

---

# 16. Agent 13 — Historical Integrity Verification

## Prompt

```text
Verify lifecycle snapshot behavior.

Scenario:
1. Start Crop Cycle using Template A.
2. Confirm crop_cycle_stages were generated.
3. Modify Template A stage name and duration.
4. Reload the existing Crop Cycle.
5. Confirm its crop_cycle_stages did not change.
6. Create/start another Crop Cycle using the modified template.
7. Confirm the new cycle receives the new template values.

Also verify:
- started Crop Cycle cannot change lifecycle_template_id
- draft Crop Cycle can change it
```

## Gate

Historical and locking behavior is correct.

---

# 17. Agent 14 — Authorization and Organization Isolation

## Prompt

```text
Perform a security review of Phase 3.3.

Verify:
- lifecycle template permissions
- Crop Cycle lifecycle permissions
- organization isolation
- cross-organization template IDs
- cross-organization Crop Cycle IDs
- cross-organization crop/plantation references
- system template protection
- started-cycle template locking
- unauthorized stage actions
- server-side validation

Fix only Phase 3.3 issues found.

Do not weaken existing authorization behavior.
```

## Gate

No cross-organization access or unauthorized mutation is possible.

---

# 18. Agent 15 — Audit Verification

## Prompt

```text
Verify Phase 3.3 audit behavior.

Confirm audit records exist for:
- template creation/update/activation/deactivation
- lifecycle template selection where supported
- Crop Cycle start
- stage start
- stage completion
- stage skip
- stage reopen
- stage override
- planned date changes where supported

Use the existing audit infrastructure.

Do not create a second audit framework.
```

## Gate

Audit records are visible through the existing audit mechanism.

---

# 19. Agent 16 — Docker and End-to-End Manual Verification

## Prompt

```text
Perform final Phase 3.3 verification.

1. Start PostgreSQL using Docker.
2. Apply migrations.
3. Run seeders.
4. Start ASP.NET Core API.
5. Start Angular application.
6. Verify:
   - login
   - lifecycle template list
   - create/edit template
   - stage configuration
   - Crop Cycle creation
   - template selection
   - Crop Cycle start
   - generated stages
   - stage progression
   - skip/reopen/override
   - planned vs actual dates
   - perennial behavior
   - non-perennial termination prompt
   - permissions
   - organization isolation
   - audit
7. Check browser console for new errors.
8. Check API logs for exceptions.
9. Check database for duplicate lifecycle stages.
10. Confirm no stage_code remains in the new lifecycle implementation.

Do not implement unrelated fixes.
```

## Gate

Phase 3.3 acceptance criteria are all satisfied.

---

# 20. Final AI Agent Review Prompt

```text
Review the complete Phase 3.3 implementation against the agreed specification.

Check every requirement:

- lifecycle template master
- crop lifecycle stages
- no stage_code
- expected_duration_days
- crop_cycles.lifecycle_template_id
- crop_cycle_stages snapshot
- transactional Crop Cycle start
- planned dates
- actual dates
- sequential progression
- complete
- skip with reason
- reopen with reason
- override with reason
- lifecycle template locking
- historical integrity
- perennial behavior
- non-perennial behavior
- user-confirmed plantation termination
- organization isolation
- authorization
- audit
- Angular UI
- seed data
- Docker migration
- manual verification
- future crop_cycle_stage_id support

Do not add Labor Activity.

For each requirement report:
PASS / FAIL / NOT APPLICABLE

For every FAIL:
- identify exact file
- identify issue
- propose smallest correction

Do not make broad refactoring.
```

# 21. Definition of Done

Phase 3.3 is complete only when:

- database migration succeeds;
- seed is idempotent;
- lifecycle templates can be managed;
- Crop Cycles can select a lifecycle;
- draft cycles can change lifecycle;
- started cycles cannot change lifecycle;
- stages are generated only on start;
- generated stages contain historical snapshots;
- planned dates are calculated;
- actual dates are independent;
- progression is sequential;
- skip/reopen/override are controlled and audited;
- perennial/non-perennial behavior works;
- organization isolation works;
- Angular UI works;
- Docker verification succeeds;
- no unrelated modules were implemented.

## 22. Future Phase Boundary

The next operational phase can build Labor Activity using:

```text
Farm
  ↓
Farm Area
  ↓
Plantation
  ↓
Crop Cycle
  ↓
Crop Cycle Stage
  ↓
Labor Activity
```

Labor attendance/payroll remains separate:

```text
Worker
  ↓
Attendance
  ↓
Earnings Ledger
```

Do not merge these flows.
