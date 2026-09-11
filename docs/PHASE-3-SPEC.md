# Farm Management Application
# Phase 3 – Farm Activity Management

**Document Version:** 1.0  
**Phase:** Phase 3  
**Implementation Strategy:** Vertical Slice / Page-by-Page Development  
**Primary Focus:** Farm Activity Tracking  
**Technology Stack:**

- Frontend: Angular
- Backend: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- Authentication: Phase 1 implementation
- Authorization: Phase 1 + Phase 2 RBAC
- Infrastructure: Docker Compose
- Future Mobile Application: React Native
- AI-Assisted Development: AI Agent Workflow

---

# 1. Phase 3 Objective

Phase 3 introduces the operational layer of the Farm Management application.

Phase 1 implemented:

```text
Authentication
Authorization
Users
Roles
Permissions
Organizations
Audit
```

Phase 2 implemented:

```text
Farm
Farm Areas
Sub Areas
Crops
Varieties
Plantations
Crop Cycles
Units
Lifecycle Templates
```

Phase 3 will implement the daily operational activities performed on farms.

Examples:

```text
Labor Work
Spraying
Fertilizer Application
Irrigation
Disease Observation
Pest Observation
Weather Events
Harvest
Expenses
Other Farm Activities
```

However, Phase 3 will **not implement everything at once**.

The implementation approach will be:

```text
ONE ACTIVITY PAGE
        ↓
Database
        ↓
Backend
        ↓
API
        ↓
Angular Page
        ↓
Validation
        ↓
Permissions
        ↓
Audit
        ↓
Docker Verification
        ↓
FINALIZE
        ↓
NEXT ACTIVITY
```

---

# 2. Phase 3 Core Implementation Principle

## Vertical Slice Development

We will NOT do this:

```text
❌ Create all tables
❌ Create all APIs
❌ Create all services
❌ Then create all Angular pages
```

Instead:

```text
✅ Labor Activity
    Complete End-to-End
        ↓
    Finalize

✅ Spray Activity
    Complete End-to-End
        ↓
    Finalize

✅ Fertilizer Activity
    Complete End-to-End
        ↓
    Finalize
```

Each activity must be independently functional before starting the next one.

---

# 3. Phase 3 Proposed Implementation Order

The recommended implementation order is:

## Phase 3.1 – Labor Activity ⭐ START HERE

```text
Labor Activity
```

Track:

- Labor work
- Workers
- Work date
- Farm
- Farm Area
- Plantation
- Crop Cycle
- Work type
- Number of workers
- Working hours
- Labor cost
- Notes

---

## Phase 3.2 – General Farm Activity Foundation

After Labor Activity is finalized, extract reusable concepts.

```text
Activity Reference Architecture
```

This prevents duplicate logic in future activities.

---

## Phase 3.3 – Spray Activity

Track:

- Spray date
- Chemical/product
- Quantity
- Unit
- Target crop
- Target plantation
- Crop cycle
- Application details
- Labor
- Cost

---

## Phase 3.4 – Fertilizer Activity

Track:

- Fertilizer/product
- Quantity
- Unit
- Application date
- Area
- Plantation
- Crop cycle
- Cost

---

## Phase 3.5 – Irrigation Activity

Track:

- Irrigation date
- Method
- Duration
- Water quantity if applicable
- Area
- Plantation
- Crop cycle

---

## Phase 3.6 – Disease / Pest Observation

Track:

- Observation date
- Disease/pest
- Severity
- Location
- Plantation
- Crop cycle
- Notes
- Future images

---

## Phase 3.7 – Weather Event

Track:

```text
Heavy Rain
Flood
Drought
Storm
Hail
Extreme Heat
Other
```

Future integration:

- Plantation health
- Crop failure
- Insurance
- Reporting

---

## Phase 3.8 – Harvest Activity

Track:

- Harvest date
- Crop
- Variety
- Plantation
- Crop cycle
- Quantity
- Unit
- Quality
- Notes

---

## Phase 3.9 – Expense Integration

Activities may later generate or link to expenses.

Examples:

```text
Labor → Labor Cost

Spray → Chemical Cost + Labor Cost

Fertilizer → Product Cost + Labor Cost

Harvest → Labor Cost + Transport Cost
```

The expense module should be implemented carefully to avoid duplicated financial data.

---

# 4. Phase 3 Architecture Strategy

Phase 3 will introduce a common activity relationship model.

Every activity should be capable of referencing:

```text
Organization
    ↓
Farm
    ↓
Farm Area
    ↓
Plantation
    ↓
Crop Cycle
```

However, not every field must always be required.

Example:

### Farm-Level Activity

```text
Weather Event

Organization
Farm

No Plantation required
```

### Plantation-Level Activity

```text
Spraying

Organization
Farm
Farm Area
Plantation
Crop Cycle
```

### Area-Level Activity

```text
Irrigation

Organization
Farm
Farm Area
```

This design allows Phase 3 activities to remain flexible.

---

# 5. Phase 3.1 – Labor Activity Specification

# 5.1 Objective

Labor Activity tracks work performed by farm workers.

Examples:

```text
Pruning
Weeding
Harvesting
Planting
Cleaning
Spraying Assistance
Fertilizer Application
Irrigation Work
General Maintenance
Other
```

---

# 5.2 Labor Activity Business Model

The first implementation will use:

```text
Labor Activity
```

as the main business record.

A Labor Activity represents:

> Work performed by one or more workers for a specific farm operation.

Example:

```text
Date: 10 September 2026

Farm: Green Valley Farm

Area: Vineyard A

Plantation: Thompson Seedless Grapes

Activity:
Pruning

Workers:
5

Hours:
8

Total Labor Cost:
₹5,000
```

---

# 6. Labor Activity Database Design

## Table: labor_activity_types

This is a master table.

```sql
CREATE TABLE labor_activity_types
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(150) NOT NULL,

    description TEXT NULL,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    display_order INTEGER NOT NULL DEFAULT 0,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL
);
```

---

# 7. Labor Activity Table

```sql
CREATE TABLE labor_activities
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NOT NULL,

    activity_date DATE NOT NULL,

    farm_id UUID NOT NULL,

    farm_area_id UUID NULL,

    plantation_id UUID NULL,

    crop_cycle_id UUID NULL,

    labor_activity_type_id UUID NOT NULL,

    description TEXT NULL,

    worker_count INTEGER NOT NULL,

    total_working_hours NUMERIC(10,2) NULL,

    cost_amount NUMERIC(18,2) NULL,

    currency_id UUID NULL,

    status VARCHAR(30) NOT NULL DEFAULT 'COMPLETED',

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NOT NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_labor_activity_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id),

    CONSTRAINT fk_labor_activity_farm
        FOREIGN KEY (farm_id)
        REFERENCES farms(id),

    CONSTRAINT fk_labor_activity_area
        FOREIGN KEY (farm_area_id)
        REFERENCES farm_areas(id),

    CONSTRAINT fk_labor_activity_plantation
        FOREIGN KEY (plantation_id)
        REFERENCES crop_plantations(id),

    CONSTRAINT fk_labor_activity_cycle
        FOREIGN KEY (crop_cycle_id)
        REFERENCES crop_cycles(id),

    CONSTRAINT fk_labor_activity_type
        FOREIGN KEY (labor_activity_type_id)
        REFERENCES labor_activity_types(id)
);
```

---

# 8. Important Relationship Rules

Labor activity references must follow the existing hierarchy.

Valid:

```text
Farm
```

Valid:

```text
Farm
↓
Farm Area
```

Valid:

```text
Farm
↓
Farm Area
↓
Plantation
```

Valid:

```text
Farm
↓
Farm Area
↓
Plantation
↓
Crop Cycle
```

Invalid:

```text
Farm A
Farm Area belonging to Farm B
```

Invalid:

```text
Plantation belonging to another Organization
```

Invalid:

```text
Crop Cycle belonging to another Plantation
```

All relationships must be validated server-side.

---

# 9. Labor Activity Status

Initial statuses:

```text
DRAFT
COMPLETED
CANCELLED
```

Recommended workflow:

```text
DRAFT
  ↓
COMPLETED
```

or:

```text
DRAFT
  ↓
CANCELLED
```

For the first implementation, direct creation as:

```text
COMPLETED
```

is allowed.

Future mobile implementation may use:

```text
DRAFT
```

for offline activity creation.

---

# 10. Labor Activity Types Seed Data

Initial system seed data:

```text
PLANTING
TRANSPLANTING
PRUNING
WEEDING
HARVESTING
SPRAYING_ASSISTANCE
FERTILIZER_APPLICATION
IRRIGATION_WORK
CLEANING
SOIL_PREPARATION
GENERAL_MAINTENANCE
TRAINING
TYING
OTHER
```

For grapes, these can later be extended with:

```text
CANOPY_MANAGEMENT
SHOOT_THINNING
BUNCH_THINNING
BERRY_THINNING
VINE_TRAINING
PRUNING
HARVEST_PREPARATION
```

Organization-specific labor activity types must also be supported.

---

# 11. Currency Handling

Phase 2 established:

```text
Organization Currency
```

Labor Activity should use the organization's configured currency.

The client should NOT freely choose another currency for normal farm activity entry.

Example:

```text
Organization Currency = INR

Labor Cost = 5000

Currency = INR
```

The backend must resolve the organization's configured currency.

Future multi-currency support can be added without changing the Labor Activity business model.

---

# 12. Labor Activity API

## Get Labor Activities

```http
GET /api/labor-activities
```

Supported filters:

```text
farmId
farmAreaId
plantationId
cropCycleId
activityTypeId
fromDate
toDate
status
page
pageSize
```

---

## Get Single Activity

```http
GET /api/labor-activities/{id}
```

---

## Create Labor Activity

```http
POST /api/labor-activities
```

Request:

```json
{
  "activityDate": "2026-09-10",

  "farmId": "uuid",

  "farmAreaId": "uuid",

  "plantationId": "uuid",

  "cropCycleId": "uuid",

  "laborActivityTypeId": "uuid",

  "description": "Pruning completed in Block A.",

  "workerCount": 5,

  "totalWorkingHours": 40,

  "costAmount": 5000,

  "status": "COMPLETED"
}
```

Important:

```text
OrganizationId is NOT accepted from the client.
CurrencyId is NOT accepted from the client.
```

Both are resolved server-side.

---

## Update Labor Activity

```http
PUT /api/labor-activities/{id}
```

Editable fields depend on status.

Recommended rule:

```text
DRAFT → Fully editable

COMPLETED → Editable with audit

CANCELLED → Not editable
```

---

## Cancel Labor Activity

```http
POST /api/labor-activities/{id}/cancel
```

Request:

```json
{
  "reason": "Incorrect activity entry"
}
```

Cancellation must not hard delete the record.

---

# 13. API Response Contract

```json
{
  "id": "uuid",

  "activityDate": "2026-09-10",

  "farm": {
    "id": "uuid",
    "name": "Green Valley Farm"
  },

  "farmArea": {
    "id": "uuid",
    "name": "Vineyard Block A"
  },

  "plantation": {
    "id": "uuid",
    "name": "Thompson Seedless Plantation"
  },

  "cropCycle": {
    "id": "uuid",
    "name": "2026 Season"
  },

  "activityType": {
    "id": "uuid",
    "name": "Pruning"
  },

  "workerCount": 5,

  "totalWorkingHours": 40,

  "costAmount": 5000,

  "currency": "INR",

  "status": "COMPLETED"
}
```

---

# 14. Backend Architecture

Follow the existing project architecture.

```text
FarmManagement.Application

Features
└── LaborActivities
    ├── Commands
    │   ├── CreateLaborActivity
    │   ├── UpdateLaborActivity
    │   └── CancelLaborActivity
    │
    ├── Queries
    │   ├── GetLaborActivities
    │   └── GetLaborActivityById
    │
    ├── DTOs
    ├── Validators
    └── Services
```

Domain:

```text
FarmManagement.Domain

Entities
├── LaborActivity
└── LaborActivityType

Enums
└── LaborActivityStatus
```

Infrastructure:

```text
FarmManagement.Infrastructure

Persistence
├── Configurations
│   ├── LaborActivityConfiguration
│   └── LaborActivityTypeConfiguration
│
└── Seeders
    └── LaborActivityTypeSeeder
```

API:

```text
FarmManagement.Api

Controllers
└── LaborActivitiesController
```

---

# 15. Permissions

Add new permissions.

## Labor Activity

```text
LaborActivity.View
LaborActivity.Create
LaborActivity.Update
LaborActivity.Cancel
```

## Labor Activity Type

```text
LaborActivityType.View
LaborActivityType.Create
LaborActivityType.Update
LaborActivityType.Activate
LaborActivityType.Deactivate
```

All permissions must be added to the existing permission seeder.

The seeder must remain:

```text
Idempotent
```

No existing Phase 1 or Phase 2 permissions may be modified or deleted.

---

# 16. Angular Implementation

## Feature Structure

```text
features/
└── labor-activities/
    │
    ├── pages/
    │   ├── labor-activity-list/
    │   ├── labor-activity-form/
    │   └── labor-activity-details/
    │
    ├── components/
    │   ├── labor-activity-filters/
    │   └── labor-activity-summary/
    │
    ├── services/
    │   └── labor-activity.service.ts
    │
    ├── models/
    │   └── labor-activity.models.ts
    │
    └── labor-activity.routes.ts
```

---

# 17. Angular Routes

```text
/labor-activities

/labor-activities/new

/labor-activities/:id

/labor-activities/:id/edit
```

Routes must use the existing:

```text
Auth Guard
Permission Guard
```

---

# 18. Labor Activity List Page

Columns:

```text
Date
Farm
Farm Area
Plantation
Activity Type
Workers
Hours
Cost
Status
Actions
```

Filters:

```text
Date Range
Farm
Farm Area
Plantation
Activity Type
Status
```

Actions depend on permissions.

Example:

```text
Create → LaborActivity.Create

Edit → LaborActivity.Update

Cancel → LaborActivity.Cancel

View → LaborActivity.View
```

---

# 19. Labor Activity Form

The form must support dependent dropdowns.

Flow:

```text
Farm
 ↓
Farm Area
 ↓
Plantation
 ↓
Crop Cycle
```

Rules:

### Farm

Required.

### Farm Area

Optional.

Only show areas belonging to selected Farm.

### Plantation

Optional.

Only show plantations belonging to selected Farm/Area.

### Crop Cycle

Optional.

Only show cycles belonging to selected Plantation.

---

## Form Fields

```text
Activity Date *

Farm *

Farm Area

Plantation

Crop Cycle

Labor Activity Type *

Number of Workers *

Total Working Hours

Labor Cost

Description

Status
```

---

# 20. Form Validation

```text
Activity Date → Required

Farm → Required

Labor Activity Type → Required

Worker Count → Required and > 0

Working Hours → > 0

Cost → >= 0

Farm Area → Must belong to Farm

Plantation → Must belong to Farm/Area

Crop Cycle → Must belong to Plantation
```

All critical validation must exist in:

```text
Angular
AND
ASP.NET Core API
```

The backend is the final authority.

---

# 21. Angular Error Handling

Reuse the Phase 1 error architecture.

Expected responses:

```text
400 → Validation Error

401 → Authentication Error

403 → Permission Error

404 → Not Found

409 → Business Conflict
```

Example conflict:

```text
Attempt to edit CANCELLED Labor Activity
```

---

# 22. Audit Requirements

Audit:

```text
Labor Activity Created

Labor Activity Updated

Labor Activity Cancelled
```

Audit data must include:

```text
User
Timestamp
Previous Values
New Values
Organization
```

Cancellation must record:

```text
Cancellation Reason
Cancelled By
Cancelled At
```

---

# 23. Database Indexes

Recommended:

```sql
CREATE INDEX ix_labor_activities_organization
ON labor_activities (organization_id);

CREATE INDEX ix_labor_activities_farm
ON labor_activities (farm_id);

CREATE INDEX ix_labor_activities_activity_date
ON labor_activities (activity_date);

CREATE INDEX ix_labor_activities_plantation
ON labor_activities (plantation_id);

CREATE INDEX ix_labor_activities_crop_cycle
ON labor_activities (crop_cycle_id);
```

---

# 24. Migration Strategy

Create:

```text
Phase3_001_AddLaborActivityTypes

Phase3_002_AddLaborActivities

Phase3_003_AddLaborActivityPermissions
```

Do not modify previous Phase 1 or Phase 2 migrations.

---

# 25. AI Agent Implementation Workflow

## Step 1 – Analyze Existing Architecture

Prompt:

```text
Review the completed Phase 1 and Phase 2 Farm Management application.

Do not modify authentication, authorization, organization isolation, audit infrastructure, Docker architecture, or existing entities.

Identify the established patterns for:
- Entities
- EF Core configurations
- DTOs
- Validation
- Services
- Controllers
- Permissions
- Seeders
- Angular feature architecture

Create a Labor Activity implementation plan before changing code.
```

---

## Step 2 – Implement Database and Domain

Prompt:

```text
Implement LaborActivityType and LaborActivity entities.

Follow existing Phase 1 and Phase 2 entity conventions.

Add PostgreSQL constraints and EF Core configurations.

Create a migration.

Do not implement APIs or Angular yet.
```

---

## Step 3 – Implement Backend Vertical Slice

Prompt:

```text
Implement the complete Labor Activity backend.

Include:
- Create
- List
- Details
- Update
- Cancel

Validate organization isolation.

Validate Farm → Area → Plantation → Crop Cycle hierarchy.

Reuse existing error middleware and audit infrastructure.

Do not change existing Phase 1 or Phase 2 functionality.
```

---

## Step 4 – Implement Permissions

Prompt:

```text
Add Labor Activity permissions to the existing permission architecture.

Create an idempotent permission seeder.

Apply permission authorization to all Labor Activity endpoints.

Do not modify existing permissions.
```

---

## Step 5 – Implement Angular List Page

Prompt:

```text
Implement the Angular Labor Activity List page.

Reuse existing application layout, authentication, authorization, interceptors, error handling, and permission guard.

Add filtering and pagination following existing Angular patterns.
```

---

## Step 6 – Implement Angular Create/Edit Page

Prompt:

```text
Implement the Labor Activity Create/Edit form.

Use dependent dropdowns:

Farm
→ Farm Area
→ Plantation
→ Crop Cycle

Validate relationships both in UI and backend.

Do not duplicate business logic from the backend.
```

---

## Step 7 – Implement Details and Cancel

Prompt:

```text
Implement Labor Activity Details page and Cancel workflow.

Show audit/history information if supported by existing audit architecture.

Require cancellation reason.

Cancelled activities must not be hard deleted.
```

---

# 26. Labor Activity Final Verification Checklist

Before moving to the next activity:

## Backend

```text
✓ Database migration works
✓ Seeder works
✓ Permissions seeded
✓ Organization isolation works
✓ Hierarchy validation works
✓ Create works
✓ Update works
✓ List works
✓ Details works
✓ Cancel works
✓ Audit works
```

## Angular

```text
✓ List page works
✓ Filters work
✓ Pagination works
✓ Create form works
✓ Edit form works
✓ Dependent dropdowns work
✓ Validation works
✓ Permission guard works
✓ Unauthorized actions hidden
✓ Error handling works
```

## Docker

```text
✓ PostgreSQL works
✓ Migration works
✓ Seeder works
✓ API works
✓ Angular works
✓ Existing Phase 1 works
✓ Existing Phase 2 works
✓ Labor Activity works
```

---

# 27. Definition of Done for Labor Activity

Labor Activity is considered finalized only when:

```text
Database
+
Backend
+
Permissions
+
Seeder
+
Audit
+
Angular List
+
Angular Form
+
Details
+
Cancel
+
Docker Verification
```

are all complete and manually verified.

Only after this point should we start the next Phase 3 component.

---

# 28. Phase 3 Development Sequence

```text
PHASE 3

3.1 Labor Activity
     ↓
     FINALIZE
     ↓

3.2 Reusable Activity Concepts
     ↓
     FINALIZE
     ↓

3.3 Spray Activity
     ↓
     FINALIZE
     ↓

3.4 Fertilizer Activity
     ↓
     FINALIZE
     ↓

3.5 Irrigation Activity
     ↓
     FINALIZE
     ↓

3.6 Disease/Pest Observation
     ↓
     FINALIZE
     ↓

3.7 Weather Events
     ↓
     FINALIZE
     ↓

3.8 Harvest Activity
     ↓
     FINALIZE
     ↓

3.9 Expense Integration
```

---

# 29. Important Future Architecture Rule

Do not prematurely create a massive generic table such as:

```text
activities
```

containing hundreds of nullable columns.

For example:

```text
❌
activity_type
chemical_id
fertilizer_id
worker_count
disease_id
water_quantity
harvest_quantity
```

This would become difficult to maintain.

Instead:

```text
LaborActivity

SprayActivity

FertilizerActivity

IrrigationActivity

DiseaseObservation

WeatherEvent

HarvestActivity
```

Each activity owns its specific business data.

Reusable relationships should remain consistent:

```text
Organization
Farm
Farm Area
Plantation
Crop Cycle
Activity Date
Created By
Audit
```

---

# 30. Phase 3 Success Architecture

At the end of Phase 3, the application should support:

```text
Farm
│
├── Structure
│   ├── Farm Areas
│   ├── Plantations
│   └── Crop Cycles
│
└── Operations
    │
    ├── Labor
    ├── Spray
    ├── Fertilizer
    ├── Irrigation
    ├── Disease
    ├── Pest
    ├── Weather
    └── Harvest
```

Each operational record will be historically traceable by:

```text
Farm
Area
Plantation
Crop
Variety
Crop Cycle
Date
Season
Year
```

This provides a strong foundation for future:

```text
Expenses
Reports
Analytics
Profitability
Disease Trends
Spray History
Crop Performance
Grape-Specific Workflows
React Native Mobile Application
```

---

# Phase 3 Final Implementation Strategy

**The first development task is:**

# Phase 3.1 – Labor Activity

We will implement it as a complete vertical slice:

```text
Database
↓
Domain
↓
Migration
↓
Seeder
↓
Permissions
↓
Backend API
↓
Angular List Page
↓
Angular Create/Edit Page
↓
Details Page
↓
Cancel Workflow
↓
Audit
↓
Docker Verification
```

After Labor Activity is fully implemented and finalized, the same page-by-page approach will be used for every subsequent farm activity.