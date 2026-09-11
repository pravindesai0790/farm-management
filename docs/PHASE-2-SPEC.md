# Farm Management Application
# Phase 2 – Farm, Crop, Plantation and Crop Cycle Management

**Document Version:** 1.0  
**Phase:** Phase 2  
**Technology Stack:**

- Frontend: Angular
- Backend: ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- Authentication: Existing Phase 1 JWT + Refresh Token implementation
- Infrastructure: Docker Compose
- Future Mobile Application: React Native
- AI-assisted implementation: AI coding agent

---

# 1. Phase 2 Objective

Phase 2 introduces the core agricultural domain model for the Farm Management Application.

The system must support:

- General crop farming
- Grape farming as the primary advanced use case
- Multiple farms per organization
- Farm areas and sub-areas
- Multiple simultaneous plantations within a farm area
- Crop and variety management
- Crop lifecycle templates
- Plantations
- Crop cycles/seasons
- Annual crops
- Perennial crops
- Failed crops
- Weather disasters
- Disease and pest-related plantation termination
- Same-season replanting
- Farm area reuse
- Area allocation validation
- Units of measurement
- Farm GPS coordinates
- Farm ownership/lease information
- Organization-specific crops and varieties
- Future activity tracking integration
- Future spray/disease/expense integration
- Future reporting and analytics

This phase must not require a schema redesign when Phase 3 activity tracking or Phase 4 grape-specific workflows are implemented.

---

# 2. Confirmed Architecture Decisions

## 2.1 Multiple plantations within a Farm Area

**Decision: YES**

A farm area can contain multiple active plantations.

Example:

```text
Farm
└── Field A – 10 Acres
    ├── Plantation A – Tomato – 4 Acres
    ├── Plantation B – Chili – 3 Acres
    └── Plantation C – Grapes – 3 Acres
```

The system must validate that:

```text
Total Active Allocated Area <= Farm Area Size
```

---

## 2.2 Farm Area Hierarchy

**Decision: One level hierarchy**

Structure:

```text
Farm
├── Farm Area
│   ├── Sub Area
│   └── Sub Area
│
└── Farm Area
```

Example:

```text
Green Valley Farm
│
├── Vineyard
│   ├── Block A
│   └── Block B
│
└── Open Field
    ├── Section 1
    └── Section 2
```

Only one parent-child level is allowed.

A Sub Area cannot contain another Sub Area.

---

## 2.3 Crop Variety

**Decision: One variety per plantation**

Example:

```text
Plantation
├── Crop: Grapes
└── Variety: Thompson Seedless
```

If another variety exists:

```text
Plantation 1 → Grapes → Thompson Seedless
Plantation 2 → Grapes → Sharad Seedless
```

This supports accurate future reporting for:

- Expenses
- Yield
- Disease
- Spray
- Production
- Crop failure

---

## 2.4 Crop Lifecycle Templates

**Decision: Include extensible schema in Phase 2**

Detailed grape workflow implementation will occur in Phase 4.

Phase 2 creates the architecture.

Example:

```text
Grape Lifecycle Template

Pruning
↓
Bud Break
↓
Flowering
↓
Fruit Set
↓
Berry Development
↓
Harvest
```

---

## 2.5 Farm Ownership

**Decision: YES**

Supported ownership types:

```text
OWNED
LEASED
RENTED
MANAGED
OTHER
```

This supports future:

- Lease expenses
- Farm profitability
- Financial reports

---

## 2.6 Failed Plantation Financial Handling

**Decision: Keep raw financial data**

The system will not automatically convert expenses into financial loss.

Example:

```text
Plantation Expense = ₹50,000

Plantation Status = TERMINATED

Reason = FLOOD
```

Future reports calculate:

```text
Investment
Revenue
Loss
Profitability
```

based on actual activities and financial transactions.

---

## 2.7 Available Area

**Decision: Calculate dynamically**

Do not store:

```text
AvailableArea
```

Instead:

```text
Available Area =
Total Area
-
Active Plantation Allocations
```

---

## 2.8 Restarting Terminated Plantation

**Decision: NOT allowed**

Once:

```text
Plantation = TERMINATED
```

it cannot return to:

```text
ACTIVE
```

If the user needs a correction, a controlled administrative correction process may be added later.

A new plantation must be created.

---

## 2.9 Organization-Specific Crops and Varieties

**Decision: YES**

The system supports:

```text
System Crop
Organization Crop
```

and:

```text
System Variety
Organization Variety
```

Example:

```text
System Crop:
Grapes

Organization Crop:
Experimental Hybrid Crop
```

---

## 2.10 GPS Coordinates

**Decision: YES**

Farm supports optional:

```text
Latitude
Longitude
```

Future uses:

- Mobile GPS
- Weather integration
- Mapping
- IoT
- Activity location

---

## 2.11 Farm Area Unique Code

**Decision: YES**

Code is unique within a farm.

Example:

```text
VINEYARD-A
VINEYARD-B
FIELD-01
BLOCK-A
```

Future uses:

- QR codes
- Mobile identification
- IoT
- Reporting

---

# 3. High-Level Domain Architecture

```text
Organization
│
├── Farms
│
│   └── Farm Areas
│       │
│       └── Sub Areas
│
│           └── Plantations
│               │
│               ├── Crop
│               ├── Variety
│               └── Crop Cycles
│
│                   └── Future Activities
│
├── Crops
├── Crop Varieties
├── Units
├── Lifecycle Templates
└── Plantation End Reasons
```

---

# 4. PostgreSQL Database Schema

All tables must use:

```text
UUID primary keys
TIMESTAMPTZ timestamps
Organization isolation
Audit fields
Soft active/inactive state
No hard delete for business entities
```

---

# 5. Units of Measurement

## 5.1 Table: units

```sql
CREATE TABLE units
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    code VARCHAR(30) NOT NULL,

    name VARCHAR(100) NOT NULL,

    symbol VARCHAR(20) NOT NULL,

    unit_category VARCHAR(50) NOT NULL,

    base_unit_code VARCHAR(30) NULL,

    conversion_factor NUMERIC(18,8) NULL,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    display_order INTEGER NOT NULL DEFAULT 0,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL
);
```

## 5.2 Unit Categories

```text
AREA
WEIGHT
VOLUME
LENGTH
COUNT
TEMPERATURE
TIME
OTHER
```

## 5.3 Example Seed Data

### Area

```text
ACRE
HECTARE
SQUARE_METER
SQUARE_FEET
```

### Weight

```text
KILOGRAM
GRAM
TON
QUINTAL
```

### Volume

```text
LITER
MILLILITER
```

### Length

```text
METER
CENTIMETER
FOOT
```

### Count

```text
NUMBER
PIECE
PLANT
```

---

# 6. Farm Ownership Types

## Table: farm_ownership_types

```sql
CREATE TABLE farm_ownership_types
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    code VARCHAR(30) NOT NULL UNIQUE,

    name VARCHAR(100) NOT NULL,

    is_system BOOLEAN NOT NULL DEFAULT TRUE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
```

Seed:

```text
OWNED
LEASED
RENTED
MANAGED
OTHER
```

---

# 7. Farms

## Table: farms

```sql
CREATE TABLE farms
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NOT NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(200) NOT NULL,

    description TEXT NULL,

    ownership_type_id UUID NOT NULL,

    total_area NUMERIC(18,4) NULL,

    area_unit_id UUID NULL,

    address_line1 VARCHAR(250) NULL,
    address_line2 VARCHAR(250) NULL,

    city VARCHAR(100) NULL,
    district VARCHAR(100) NULL,
    state VARCHAR(100) NULL,

    country VARCHAR(100) NULL,

    postal_code VARCHAR(30) NULL,

    latitude NUMERIC(10,7) NULL,
    longitude NUMERIC(10,7) NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NOT NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_farm_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id),

    CONSTRAINT fk_farm_ownership_type
        FOREIGN KEY (ownership_type_id)
        REFERENCES farm_ownership_types(id),

    CONSTRAINT fk_farm_area_unit
        FOREIGN KEY (area_unit_id)
        REFERENCES units(id),

    CONSTRAINT ux_farm_organization_code
        UNIQUE (organization_id, code)
);
```

---

# 8. Farm Areas

## Table: farm_areas

```sql
CREATE TABLE farm_areas
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NOT NULL,

    farm_id UUID NOT NULL,

    parent_farm_area_id UUID NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(200) NOT NULL,

    description TEXT NULL,

    total_area NUMERIC(18,4) NOT NULL,

    area_unit_id UUID NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NOT NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_farm_area_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id),

    CONSTRAINT fk_farm_area_farm
        FOREIGN KEY (farm_id)
        REFERENCES farms(id),

    CONSTRAINT fk_farm_area_parent
        FOREIGN KEY (parent_farm_area_id)
        REFERENCES farm_areas(id),

    CONSTRAINT fk_farm_area_unit
        FOREIGN KEY (area_unit_id)
        REFERENCES units(id),

    CONSTRAINT ux_farm_area_code
        UNIQUE (farm_id, code)
);
```

---

# 9. Farm Area Hierarchy Validation

Application validation must ensure:

```text
Farm Area
    ↓
Sub Area
```

Allowed.

But:

```text
Farm Area
    ↓
Sub Area
    ↓
Another Sub Area
```

Not allowed.

Rules:

1. Parent area must belong to the same Farm.
2. Parent area must belong to the same Organization.
3. Parent area cannot itself have a parent.
4. A child area cannot exceed its parent's total area.
5. Total active child areas cannot exceed parent area.

---

# 10. Crops

## Table: crops

```sql
CREATE TABLE crops
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(150) NOT NULL,

    scientific_name VARCHAR(200) NULL,

    crop_type VARCHAR(50) NOT NULL,

    crop_duration_type VARCHAR(30) NOT NULL,

    description TEXT NULL,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL
);
```

## Crop Duration Types

```text
ANNUAL
PERENNIAL
SEASONAL
OTHER
```

Examples:

```text
Tomato → ANNUAL
Chili → SEASONAL
Grapes → PERENNIAL
Mango → PERENNIAL
```

---

# 11. Crop Varieties

## Table: crop_varieties

```sql
CREATE TABLE crop_varieties
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    crop_id UUID NOT NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(150) NOT NULL,

    description TEXT NULL,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_crop_variety_crop
        FOREIGN KEY (crop_id)
        REFERENCES crops(id)
);
```

---

# 12. Crop Lifecycle Templates

## Table: crop_lifecycle_templates

```sql
CREATE TABLE crop_lifecycle_templates
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    crop_id UUID NOT NULL,

    name VARCHAR(150) NOT NULL,

    description TEXT NULL,

    is_default BOOLEAN NOT NULL DEFAULT FALSE,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_lifecycle_template_crop
        FOREIGN KEY (crop_id)
        REFERENCES crops(id)
);
```

## Table: crop_lifecycle_stages

```sql
CREATE TABLE crop_lifecycle_stages
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    lifecycle_template_id UUID NOT NULL,

    stage_code VARCHAR(50) NOT NULL,

    stage_name VARCHAR(150) NOT NULL,

    sequence_number INTEGER NOT NULL,

    description TEXT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    CONSTRAINT fk_lifecycle_stage_template
        FOREIGN KEY (lifecycle_template_id)
        REFERENCES crop_lifecycle_templates(id),

    CONSTRAINT ux_lifecycle_stage_sequence
        UNIQUE (lifecycle_template_id, sequence_number)
);
```

Phase 4 will connect actual grape workflow execution to these tables.

---

# 13. Plantation End Reasons

## Table: plantation_end_reasons

```sql
CREATE TABLE plantation_end_reasons
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NULL,

    code VARCHAR(50) NOT NULL,

    name VARCHAR(150) NOT NULL,

    description TEXT NULL,

    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    created_by UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL
);
```

System seed data:

```text
HARVEST_COMPLETED
WEATHER_DISASTER
FLOOD
DROUGHT
CYCLONE
PEST_INFESTATION
DISEASE
CROP_FAILURE
POOR_CROP_HEALTH
SOIL_PROBLEM
REPLANT_REQUIRED
FARMER_DECISION
OTHER
```

---

# 14. Plantations

A Plantation represents a physical crop cultivation instance.

Examples:

```text
Field A → Tomato Plantation
```

or:

```text
Vineyard Block A → Thompson Seedless Grapes
```

## Table: crop_plantations

```sql
CREATE TABLE crop_plantations
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NOT NULL,

    farm_id UUID NOT NULL,

    farm_area_id UUID NOT NULL,

    crop_id UUID NOT NULL,

    variety_id UUID NULL,

    lifecycle_template_id UUID NULL,

    plantation_code VARCHAR(50) NOT NULL,

    plantation_name VARCHAR(200) NOT NULL,

    allocated_area NUMERIC(18,4) NOT NULL,

    area_unit_id UUID NOT NULL,

    planting_date DATE NOT NULL,

    expected_end_date DATE NULL,

    actual_end_date DATE NULL,

    status VARCHAR(30) NOT NULL,

    end_reason_id UUID NULL,

    end_notes TEXT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NOT NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_plantation_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id),

    CONSTRAINT fk_plantation_farm
        FOREIGN KEY (farm_id)
        REFERENCES farms(id),

    CONSTRAINT fk_plantation_area
        FOREIGN KEY (farm_area_id)
        REFERENCES farm_areas(id),

    CONSTRAINT fk_plantation_crop
        FOREIGN KEY (crop_id)
        REFERENCES crops(id),

    CONSTRAINT fk_plantation_variety
        FOREIGN KEY (variety_id)
        REFERENCES crop_varieties(id),

    CONSTRAINT fk_plantation_lifecycle
        FOREIGN KEY (lifecycle_template_id)
        REFERENCES crop_lifecycle_templates(id),

    CONSTRAINT fk_plantation_area_unit
        FOREIGN KEY (area_unit_id)
        REFERENCES units(id),

    CONSTRAINT fk_plantation_end_reason
        FOREIGN KEY (end_reason_id)
        REFERENCES plantation_end_reasons(id),

    CONSTRAINT ux_plantation_code
        UNIQUE (organization_id, plantation_code)
);
```

---

# 15. Plantation Status

```text
PLANNED
ACTIVE
TERMINATED
ARCHIVED
```

## Rules

### PLANNED

Plantation has been registered but cultivation has not started.

### ACTIVE

Crop currently exists and is being cultivated.

### TERMINATED

Crop cultivation has permanently stopped.

Examples:

- Flood
- Disease
- Weather disaster
- Crop failure
- Replanting
- Farmer decision

A terminated plantation cannot be restarted.

### ARCHIVED

Historical administrative state.

---

# 16. Crop Cycles

A Crop Cycle represents a production cycle or season.

This is particularly important for grapes.

## Annual Crop

```text
Plantation
└── Crop Cycle
    └── 2026 Season
```

## Grapes

```text
Grape Plantation
│
├── 2024-25 Cycle
├── 2025-26 Cycle
└── 2026-27 Cycle
```

The same grape plantation remains active while individual cycles are completed.

---

## Table: crop_cycles

```sql
CREATE TABLE crop_cycles
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    organization_id UUID NOT NULL,

    plantation_id UUID NOT NULL,

    cycle_code VARCHAR(50) NOT NULL,

    cycle_name VARCHAR(200) NOT NULL,

    season_year INTEGER NOT NULL,

    season_name VARCHAR(100) NULL,

    planned_start_date DATE NOT NULL,

    actual_start_date DATE NULL,

    expected_end_date DATE NULL,

    actual_end_date DATE NULL,

    status VARCHAR(30) NOT NULL,

    cancellation_reason_id UUID NULL,

    cancellation_notes TEXT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID NOT NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by UUID NULL,

    CONSTRAINT fk_crop_cycle_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id),

    CONSTRAINT fk_crop_cycle_plantation
        FOREIGN KEY (plantation_id)
        REFERENCES crop_plantations(id),

    CONSTRAINT fk_crop_cycle_cancellation_reason
        FOREIGN KEY (cancellation_reason_id)
        REFERENCES plantation_end_reasons(id),

    CONSTRAINT ux_crop_cycle_code
        UNIQUE (organization_id, cycle_code)
);
```

---

# 17. Crop Cycle Status

```text
PLANNED
ACTIVE
HARVESTED
COMPLETED
CANCELLED
```

## Successful Cycle

```text
PLANNED
↓
ACTIVE
↓
HARVESTED
↓
COMPLETED
```

## Failed Cycle

```text
PLANNED
↓
ACTIVE
↓
CANCELLED
```

---

# 18. Farm Area Reuse

Farm Areas are permanent physical locations.

They are never completed after harvest.

Example:

```text
Field A

2024 → Tomato
2025 → Chili
2026 → Grapes
```

Historical plantations remain available.

```text
Farm Area
│
├── Plantation 1 → Tomato → TERMINATED
│
├── Plantation 2 → Chili → TERMINATED
│
└── Plantation 3 → Grapes → ACTIVE
```

---

# 19. Weather Disaster / Crop Failure Flow

Scenario:

```text
Plantation ACTIVE
↓
Flood occurs
↓
Crop cannot recover
```

The user selects:

```text
Terminate Plantation
```

API:

```http
POST /api/plantations/{id}/terminate
```

Request:

```json
{
  "terminationDate": "2026-07-12",
  "endReasonId": "reason-id",
  "notes": "Flood damaged crop beyond recovery.",
  "cancelActiveCycles": true
}
```

Backend transaction:

```text
BEGIN TRANSACTION

1. Validate Organization.
2. Validate Plantation.
3. Validate status = ACTIVE.
4. Update Plantation → TERMINATED.
5. Set ActualEndDate.
6. Store End Reason.
7. Cancel all active cycles if requested.
8. Store audit records.

COMMIT
```

---

# 20. Same Season Replanting

Example:

```text
Farm Area: Field A

Kharif 2026

Plantation #1
Tomato
June 1 → July 12
Status: TERMINATED
Reason: FLOOD

Plantation #2
Tomato
July 20
Status: ACTIVE
```

This must be allowed.

The database must not enforce:

```text
One Plantation Per Area Per Season
```

Instead, area allocation validation is applied dynamically.

---

# 21. Multiple Plantation Area Allocation

Example:

```text
Farm Area = 10 Acres

Plantation A = 4 Acres
Plantation B = 3 Acres
Plantation C = 3 Acres

Total = 10 Acres
```

Allowed.

Attempt:

```text
Plantation D = 1 Acre

Total = 11 Acres
```

Rejected.

The API must convert all area values internally to the configured base unit before calculation.

Recommended internal area base unit:

```text
SQUARE_METER
```

The UI displays the user's selected unit.

---

# 22. Phase 3 Future Activity Integration

Future activities should support:

```text
Organization
Farm
Farm Area
Plantation
Crop Cycle
Activity
```

Recommended future Activity fields:

```text
organization_id
farm_id
farm_area_id
plantation_id
crop_cycle_id
```

This preserves history.

Example:

```text
Spray Activity

Farm Area = Field A
Plantation = Old Tomato Plantation
Crop Cycle = Kharif 2026
```

Even if a new crop is planted later, historical activities remain correctly linked.

---

# 23. Future Reporting Architecture

Phase 2 entities must support future reports by:

```text
Organization
Farm
Farm Area
Crop
Variety
Plantation
Crop Cycle
Year
Season
Month
```

Future reports:

- Crop-wise expense
- Season-wise expense
- Farm-wise expense
- Spray reports
- Disease reports
- Plantation failure reports
- Crop rotation
- Yield
- Profitability
- Weather damage
- Replanting history

---

# 24. Permissions

Phase 2 extends the Phase 1 RBAC architecture.

No RBAC redesign is required.

## Farm

```text
Farm.View
Farm.Create
Farm.Update
Farm.Activate
Farm.Deactivate
```

## Farm Area

```text
FarmArea.View
FarmArea.Create
FarmArea.Update
FarmArea.Activate
FarmArea.Deactivate
```

## Crop

```text
Crop.View
Crop.Create
Crop.Update
Crop.Activate
Crop.Deactivate
```

## Crop Variety

```text
CropVariety.View
CropVariety.Create
CropVariety.Update
CropVariety.Activate
CropVariety.Deactivate
```

## Plantation

```text
Plantation.View
Plantation.Create
Plantation.Update
Plantation.Activate
Plantation.Terminate
```

## Crop Cycle

```text
CropCycle.View
CropCycle.Create
CropCycle.Update
CropCycle.Start
CropCycle.Complete
CropCycle.Cancel
```

## Units

```text
Unit.View
Unit.Create
Unit.Update
Unit.Activate
Unit.Deactivate
```

## Lifecycle Templates

```text
CropLifecycleTemplate.View
CropLifecycleTemplate.Create
CropLifecycleTemplate.Update
CropLifecycleTemplate.Activate
CropLifecycleTemplate.Deactivate
```

## Plantation End Reasons

```text
PlantationEndReason.View
PlantationEndReason.Create
PlantationEndReason.Update
PlantationEndReason.Activate
PlantationEndReason.Deactivate
```

---

# 25. Permission Seeder Requirements

Seeder must:

```text
1. Insert missing permissions.
2. Never delete existing permissions.
3. Use stable permission codes.
4. Be idempotent.
5. Assign defaults to System Administrator.
6. Assign appropriate permissions to Organization Administrator.
7. Preserve custom role assignments.
```

Example:

```csharp
await permissionSeeder.SeedAsync();
```

Repeated execution must not create duplicates.

---

# 26. ASP.NET Core Architecture

Recommended structure:

```text
src/

FarmManagement.Api
│
├── Controllers
├── Middleware
├── Extensions
└── Program.cs

FarmManagement.Application
│
├── Features
│   ├── Farms
│   ├── FarmAreas
│   ├── Crops
│   ├── CropVarieties
│   ├── Plantations
│   ├── CropCycles
│   ├── Units
│   └── LifecycleTemplates
│
├── DTOs
├── Interfaces
├── Validators
└── Authorization

FarmManagement.Domain
│
├── Entities
├── Enums
├── Constants
└── Interfaces

FarmManagement.Infrastructure
│
├── Persistence
│   ├── Configurations
│   ├── Migrations
│   └── Seeders
│
├── Repositories
└── Services
```

Existing Phase 1 architecture should be retained.

---

# 27. API Endpoints

## Farms

```http
GET    /api/farms
GET    /api/farms/{id}
POST   /api/farms
PUT    /api/farms/{id}
PATCH  /api/farms/{id}/activate
PATCH  /api/farms/{id}/deactivate
```

---

## Farm Areas

```http
GET    /api/farms/{farmId}/areas
GET    /api/farm-areas/{id}
POST   /api/farm-areas
PUT    /api/farm-areas/{id}
PATCH  /api/farm-areas/{id}/activate
PATCH  /api/farm-areas/{id}/deactivate
```

Additional:

```http
GET /api/farm-areas/{id}/availability
```

Response:

```json
{
  "farmAreaId": "uuid",
  "totalArea": 10,
  "allocatedArea": 7,
  "availableArea": 3,
  "unit": "ACRE"
}
```

---

## Crops

```http
GET    /api/crops
GET    /api/crops/{id}
POST   /api/crops
PUT    /api/crops/{id}
PATCH  /api/crops/{id}/activate
PATCH  /api/crops/{id}/deactivate
```

---

## Crop Varieties

```http
GET    /api/crops/{cropId}/varieties
GET    /api/crop-varieties/{id}
POST   /api/crop-varieties
PUT    /api/crop-varieties/{id}
PATCH  /api/crop-varieties/{id}/activate
PATCH  /api/crop-varieties/{id}/deactivate
```

---

## Plantations

```http
GET  /api/plantations
GET  /api/plantations/{id}
POST /api/plantations
PUT  /api/plantations/{id}

POST /api/plantations/{id}/activate
POST /api/plantations/{id}/terminate
```

---

## Crop Cycles

```http
GET  /api/crop-cycles
GET  /api/crop-cycles/{id}
POST /api/crop-cycles
PUT  /api/crop-cycles/{id}

POST /api/crop-cycles/{id}/start
POST /api/crop-cycles/{id}/harvest
POST /api/crop-cycles/{id}/complete
POST /api/crop-cycles/{id}/cancel
```

---

# 28. Organization Data Isolation

Every organization-owned query must enforce:

```text
organization_id = CurrentUser.OrganizationId
```

Never trust:

```text
organizationId
```

provided by the Angular client.

The organization is resolved from the authenticated user context.

This applies to:

- Farms
- Areas
- Crops
- Varieties
- Plantations
- Crop Cycles
- Organization master data

---

# 29. Angular Project Structure

```text
src/app/

core/
├── auth/
├── guards/
├── interceptors/
├── services/
└── models/

features/
├── farms/
│   ├── pages/
│   ├── components/
│   ├── services/
│   └── models/
│
├── farm-areas/
├── crops/
├── plantations/
├── crop-cycles/
└── master-data/

shared/
├── components/
├── directives/
├── pipes/
└── utils/
```

---

# 30. Angular Routes

```text
/dashboard

/farms
/farms/new
/farms/:id
/farms/:id/edit

/farm-areas
/farm-areas/new
/farm-areas/:id

/crops
/crops/new
/crops/:id

/plantations
/plantations/new
/plantations/:id

/crop-cycles
/crop-cycles/new
/crop-cycles/:id

/master-data/units
/master-data/crop-lifecycle-templates
/master-data/plantation-end-reasons
```

Routes must use existing Phase 1 permission guards.

---

# 31. Angular Screens

## Farm List

Columns:

```text
Code
Farm Name
Ownership
Location
Total Area
Status
Actions
```

## Farm Details

Tabs:

```text
Overview
Farm Areas
Plantations
History
```

## Farm Area Screen

Display:

```text
Total Area
Allocated Area
Available Area
Active Plantations
Historical Plantations
```

## Plantation Details

Display:

```text
Crop
Variety
Farm
Farm Area
Allocated Area
Planting Date
Status
End Reason
Crop Cycles
```

---

# 32. Validation Rules

## Farm

```text
Name required
Code required
Code unique within Organization
GPS latitude between -90 and +90
GPS longitude between -180 and +180
```

## Farm Area

```text
Code required
Code unique within Farm
Total area > 0
Parent must belong to same Farm
Maximum hierarchy depth = 1
Child area allocation <= Parent area
```

## Plantation

```text
Farm Area required
Crop required
Allocated Area > 0
Allocated Area <= Available Area
Variety must belong to selected Crop
Planting date required
Termination requires reason
Terminated Plantation cannot reactivate
```

## Crop Cycle

```text
Plantation required
Season year required
Start date required
Cancellation requires reason
Cannot start cycle for TERMINATED plantation
```

---

# 33. Audit Requirements

Phase 2 must create audit records for:

```text
Farm Created
Farm Updated
Farm Activated
Farm Deactivated

Farm Area Created
Farm Area Updated

Plantation Created
Plantation Updated
Plantation Activated
Plantation Terminated

Crop Cycle Created
Crop Cycle Started
Crop Cycle Harvested
Crop Cycle Completed
Crop Cycle Cancelled
```

Termination audit must include:

```text
Previous Status
New Status
Termination Date
Reason
Notes
User
Timestamp
```

---

# 34. Docker Configuration

Existing Phase 1 Docker architecture remains.

Recommended services:

```yaml
services:

  postgres:
    image: postgres:16

  api:
    build:
      context: ./backend

  angular:
    build:
      context: ./frontend
```

PostgreSQL environment:

```env
POSTGRES_DB=farmmanagement
POSTGRES_USER=farmuser
POSTGRES_PASSWORD=development_password
```

API:

```env
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=farmmanagement;Username=farmuser;Password=development_password
```

Existing authentication environment variables remain unchanged.

---

# 35. Migration Strategy

Phase 2 must use Entity Framework Core migrations.

Recommended migration sequence:

```text
Phase2_001_AddUnits

Phase2_002_AddFarmOwnershipTypes

Phase2_003_AddFarms

Phase2_004_AddFarmAreas

Phase2_005_AddCropsAndVarieties

Phase2_006_AddLifecycleTemplates

Phase2_007_AddPlantationEndReasons

Phase2_008_AddPlantations

Phase2_009_AddCropCycles

Phase2_010_SeedPhase2PermissionsAndMasterData
```

Migration principles:

```text
Never edit an applied migration.

Create new migrations for changes.

Test migration against clean database.

Test migration against existing Phase 1 database.

Seeder must be idempotent.
```

---

# 36. Phase 2 Seeder Requirements

Seed:

## Units

```text
Acre
Hectare
Square Meter
Kilogram
Gram
Quintal
Ton
Liter
Milliliter
Meter
Centimeter
Plant
Piece
```

## Farm Ownership

```text
OWNED
LEASED
RENTED
MANAGED
OTHER
```

## Crops

At minimum:

```text
Grapes
Tomato
Chili
Mango
Banana
```

## Grape Varieties

Example seed:

```text
Thompson Seedless
Sharad Seedless
Sonaka
Manik Chaman
```

## Plantation End Reasons

Seed all standard reasons defined in this specification.

## Permissions

Seed all Phase 2 permissions.

---

# 37. AI Agent Implementation Workflow

The AI agent must implement Phase 2 incrementally.

## Task 1 – Review Phase 1

Prompt:

```text
Review the existing Phase 1 Farm Management application architecture.

Do not modify existing authentication, authorization, JWT, refresh token, organization isolation, audit logging, Docker configuration, or Angular auth architecture.

Identify the existing entity, repository, service, DTO, permission, authorization, and migration patterns.

Produce a short implementation plan for Phase 2 before changing code.
```

---

## Task 2 – Units and Master Data

```text
Implement the Unit master entity and PostgreSQL configuration.

Support unit categories:
AREA, WEIGHT, VOLUME, LENGTH, COUNT, TEMPERATURE, TIME, OTHER.

Support system-level and organization-level units.

Create EF Core migration and idempotent seed data.

Do not implement UI yet.
```

---

## Task 3 – Farms

```text
Implement Farm entity, EF configuration, repository/service architecture, DTOs, validation, authorization, API endpoints, and audit events.

Use existing Phase 1 organization isolation.

Do not accept OrganizationId from API clients.
```

---

## Task 4 – Farm Areas

```text
Implement Farm Areas with one-level hierarchy.

Validate parent-child relationships.

Prevent nested child areas.

Validate area allocation.

Implement available area calculation dynamically.
```

---

## Task 5 – Crops and Varieties

```text
Implement global and organization-specific Crops and Crop Varieties.

Validate that a selected variety belongs to the selected crop.

Prevent cross-organization data access.
```

---

## Task 6 – Lifecycle Templates

```text
Implement Crop Lifecycle Templates and Lifecycle Stages.

Create extensible schema only.

Do not implement grape workflow automation.

Support future Phase 4 workflow integration.
```

---

## Task 7 – Plantations

```text
Implement Crop Plantations.

Support multiple active plantations within a Farm Area.

Validate allocated area against dynamically available area.

Use database transactions where concurrent updates could cause over-allocation.

Support PLANNED, ACTIVE, TERMINATED and ARCHIVED statuses.
```

---

## Task 8 – Crop Failure

```text
Implement Plantation Termination.

Termination must be irreversible.

Require termination reason and termination date.

Optionally cancel active Crop Cycles in the same database transaction.

Create audit records.
```

---

## Task 9 – Crop Cycles

```text
Implement Crop Cycles.

Support PLANNED, ACTIVE, HARVESTED, COMPLETED and CANCELLED.

Support multiple cycles for perennial crops.

Prevent active cycle creation for terminated plantations.
```

---

## Task 10 – Permissions

```text
Add all Phase 2 permissions to the existing permission architecture.

Create an idempotent permission seeder.

Do not modify or remove Phase 1 permissions.

Apply permission authorization to every Phase 2 endpoint.
```

---

## Task 11 – Angular

```text
Implement Angular Farm Management feature modules/pages following the existing application architecture.

Use existing authentication services, auth interceptor and permission guard.

Do not duplicate authentication logic.

Hide actions when the user lacks permissions.
```

---

## Task 12 – Docker Verification

```text
Verify the complete application using Docker Compose.

Test:
PostgreSQL startup
Migration execution
Seeder execution
API startup
Angular startup
Phase 1 authentication
Phase 2 APIs
Organization isolation
Permission authorization

Do not add unit or integration tests.
```

---

# 38. Phase 2 Acceptance Criteria

Phase 2 is complete when:

### Farm Management

- Farms can be created.
- Farms can be updated.
- Farms can be activated/deactivated.
- GPS coordinates can be stored.
- Ownership type is supported.

### Farm Areas

- Farm Areas can be created.
- Sub Areas are supported.
- More than one hierarchy level is prevented.
- Codes are unique within a Farm.
- Area allocation is validated.

### Crops

- System crops exist.
- Organization crops can be created.
- Varieties belong to crops.

### Plantations

- Multiple active plantations can share an area.
- Area over-allocation is prevented.
- Annual crops are supported.
- Perennial crops are supported.
- Plantations can be terminated.
- Terminated plantations cannot restart.
- Farm Areas can be reused.

### Disaster Scenario

- Failed plantation is preserved.
- Termination reason is stored.
- Active crop cycle can be cancelled.
- A new plantation can start in the same area.
- Same-season replanting is supported.

### Crop Cycles

- Crop cycles can be created.
- Cycles can start.
- Cycles can be harvested.
- Cycles can be completed.
- Failed cycles can be cancelled.
- Perennial plantations support multiple cycles.

### Security

- Existing Phase 1 authentication remains unchanged.
- Existing RBAC remains unchanged.
- Phase 2 permissions are seeded.
- Organization isolation works.
- Unauthorized access returns appropriate errors.

### Infrastructure

- Docker works.
- PostgreSQL works.
- Migrations work.
- Seeders work.
- Angular works.
- API works.

---

# 39. Phase 3 Compatibility

Phase 3 will introduce:

```text
Farm Activities
Expenses
Labor
Sprays
Fertilizer
Disease
Pest Management
Weather Events
Harvest
Inventory Usage
```

All Phase 3 records should integrate using:

```text
Organization
Farm
Farm Area
Plantation
Crop Cycle
```

No redesign of the Phase 2 core schema should be necessary.

---

# 40. Phase 4 Compatibility – Grapes

Phase 4 will build advanced grape workflows using:

```text
Grape Plantation
↓
Crop Cycle
↓
Lifecycle Template
↓
Lifecycle Stage
↓
Activities
↓
Spray
Disease
Pruning
Irrigation
Fertilizer
Harvest
```

The Phase 2 lifecycle template architecture provides the foundation without prematurely implementing grape-specific workflows.

---

# Final Phase 2 Architecture Summary

```text
Organization
│
├── Farms
│   │
│   └── Farm Areas
│       │
│       └── Optional Sub Areas
│           │
│           └── Multiple Plantations
│               │
│               ├── Crop
│               ├── Variety
│               ├── Area Allocation
│               ├── Lifecycle Template
│               │
│               └── Crop Cycles
│                   │
│                   └── Future Activities
│
├── Units
├── Farm Ownership Types
├── Crops
├── Crop Varieties
├── Lifecycle Templates
└── Plantation End Reasons
```

This Phase 2 design supports both:

```text
Simple General Crop Farming
```

and:

```text
Advanced Multi-Year Grape Farm Management
```

while preserving historical data, supporting failed crops and disasters, enabling same-area reuse and same-season replanting, and providing a stable foundation for future activity, expense, disease, spray, harvest, reporting, mobile, and grape-specific workflow development.