# SuperAdmin User Workflow & QA Verification Guide

**Document:** `SUPERADMIN-WORKFLOW-AND-QA-TEST-GUIDE.md`  
**Location:** `docs/SUPERADMIN-WORKFLOW-AND-QA-TEST-GUIDE.md`  
**Version:** 1.0  
**Snapshot Date:** September 2026  
**Target Audience:** QA Engineers, Test Automation Agents, Core Developers  
**Reference Specifications:**
- `PHASE-0-SPEC.md` (Core Platform & Foundation)
- `PHASE-1-SPEC.md` (Identity, Authentication & Administration)
- `PHASE-2-SPEC.md` (Farms, Areas, Crops, Plantations & Crop Cycles)
- `docs/IMPLEMENTATION-STATUS.md` (Source Code Implementation Status)

---

## 1. Executive Summary & Purpose

This document provides an end-to-end testing and verification guide for the **SuperAdmin** role in the Farm Management Platform. It is designed to allow QA engineers and autonomous QA testing agents to execute real-world test scenarios on a running instance of the application (Angular Web + ASP.NET Core API + PostgreSQL).

This guide documents:
1. **SuperAdmin Persona & Special Powers:** Token claims, permissions, and cross-organization capabilities.
2. **Setup & Bootstrapping:** How the application seeds the initial SuperAdmin account.
3. **Step-by-Step Test Workflows:** Explicit, reproducible user journeys with UI interactions, API endpoints, request/response structures, and assertion criteria.
4. **Complete Page Access & Permission Matrix:** Route-by-route and endpoint-by-endpoint RBAC mappings.
5. **Implementation Gap Analysis:** Clear categorization of what is fully implemented, what is partial, and what is planned for future phases (to prevent filing false bug reports).

---

## 2. SuperAdmin Persona & Architecture

### 2.1 Role Definition & Capabilities

The **SuperAdmin** is the highest-privilege platform administrator. Unlike tenant-level administrators (`OrganizationAdmin`), the SuperAdmin has cross-tenant visibility and authority:

| Capability | SuperAdmin | OrganizationAdmin | FarmManager / Supervisor |
|---|:---:|:---:|:---:|
| **Scope Claim** | `organization_scope: "all"` | Single Tenant Org ID | Single Tenant Org ID |
| **Manage Organizations** | Create, view all, update, activate/deactivate | View own org, update profile | View only (if granted) |
| **User Management** | View all users across all tenants, create user for any org | View & manage users in own org only | No access |
| **Assign SuperAdmin Role** | Yes | No (strictly forbidden) | No |
| **Role & Permission Management** | Create/edit custom roles & assign permissions | View roles & permissions only | No access |
| **Farm & Crop Domain Access** | Full view, create, edit, activate/deactivate | Full within own organization | Operational permissions |

### 2.2 JWT Token Claims & Identity Profile

When a SuperAdmin successfully logs in via `POST /api/auth/login`, the API issues:
- An **Access Token (JWT)** stored in frontend memory with a short expiry (default: 15–60 minutes).
- A **Refresh Token** stored securely in an `HttpOnly` cookie named `fm_refresh_token` (or configured name).

#### Decoded SuperAdmin JWT Claims Payload:
```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "email": "admin@example.com",
  "organization_id": "<GUID_OF_DEFAULT_ORG>",
  "jti": "d3b07384d113edec49eaa6238ad5ff00",
  "roles": [
    "SuperAdmin"
  ],
  "organization_scope": "all",
  "permissions": [
    "Users.View", "Users.Create", "Users.Update", "Users.Activate", "Users.Deactivate", "Users.Unlock", "Users.ManageRoles",
    "Roles.View", "Roles.Create", "Roles.Update", "Roles.Activate", "Roles.Deactivate", "Roles.ManagePermissions",
    "Permissions.View",
    "Organization.Create", "Organization.View", "Organization.Update", "Organization.Activate", "Organization.Deactivate",
    "Farm.View", "Farm.Create", "Farm.Update", "Farm.Activate", "Farm.Deactivate",
    "FarmArea.View", "FarmArea.Create", "FarmArea.Update", "FarmArea.Activate", "FarmArea.Deactivate",
    "Crop.View", "Crop.Create", "Crop.Update", "Crop.Activate", "Crop.Deactivate",
    "CropVariety.View", "CropVariety.Create", "CropVariety.Update", "CropVariety.Activate", "CropVariety.Deactivate",
    "CropLifecycleTemplate.View", "CropLifecycleTemplate.Create", "CropLifecycleTemplate.Update", "CropLifecycleTemplate.Activate", "CropLifecycleTemplate.Deactivate",
    "Plantation.View", "Plantation.Create", "Plantation.Update", "Plantation.Activate", "Plantation.Terminate",
    "CropCycle.View", "CropCycle.Create", "CropCycle.Update", "CropCycle.Start", "CropCycle.Complete", "CropCycle.Cancel",
    "Unit.View", "Unit.Create", "Unit.Update", "Unit.Activate", "Unit.Deactivate",
    "PlantationEndReason.View", "PlantationEndReason.Create", "PlantationEndReason.Update", "PlantationEndReason.Activate", "PlantationEndReason.Deactivate"
  ],
  "exp": 1788500000,
  "iss": "FarmManagement.API",
  "aud": "FarmManagement.Web"
}
```

> [!IMPORTANT]
> The presence of `organization_scope: "all"` is what authorizes the backend (`UserAdministrationService`, `OrganizationService`) to bypass single-organization row-level filters.

---

## 3. Environment Setup & Application Startup

Before executing the tests, verify the test environment is running and seeded.

### 3.1 Initial Configuration (`.env` or `appsettings.json`)
Verify the following variables are configured in the environment:
```env
POSTGRES_DB=farm_management
POSTGRES_USER=farm_user
POSTGRES_PASSWORD=your_password_here
API_PORT=8080
WEB_PORT=4200

# Initial Admin Seeding
INITIAL_ADMIN_EMAIL=admin@example.com
INITIAL_ADMIN_PASSWORD=Pa$$w0rd@123
```

### 3.2 Launching the Application Stack

#### Option A: Docker Compose
```powershell
docker compose up --build -d
```

#### Option B: Local Development
```powershell
# 1. Start PostgreSQL (e.g., in Docker or local service)
# 2. Run Backend API (from backend/)
dotnet run --project src/FarmManagement.API/FarmManagement.API.csproj

# 3. Run Angular Web Client (from frontend/farm-management-web/)
npm start
```

### 3.3 Sanity Verification Endpoints
Verify services are responsive before starting UI/API tests:

| Endpoint | Method | Expected Status | Expected Output |
|---|:---:|:---:|---|
| `http://localhost:8080/health` | GET | `200 OK` | `Healthy` |
| `http://localhost:8080/api/system/ping` | GET | `200 OK` | `{"message":"pong","timestamp":"..."}` |
| `http://localhost:8080/openapi/v1.json` | GET | `200 OK` | OpenAPI 3.0 Document |
| `http://localhost:4200/` | GET | `200 OK` | Angular App (Redirects to `/login`) |

---

## 4. Step-by-Step QA Workflows

Each workflow includes UI actions, API requests, expected status codes, and verification assertions.

```
+-----------------------------------------------------------------------------------+
|                           SUPERADMIN WORKFLOW JOURNEY                             |
+-----------------------------------------------------------------------------------+
  [ 1. Login & Token Bootstrap ]  -->  [ 2. Shell & Initials Check ]
             |
             v
  [ 3. Organization Management ]  -->  Create New Tenant Org & Verify Listing
             |
             v
  [ 4. Roles & Permissions ]     -->  Create Custom Role, Assign Permissions
             |
             v
  [ 5. User Administration ]     -->  Create User in Chosen Org + Assign Roles
             |
             v
  [ 6. Farms & Farm Areas ]       -->  Create Farm, Ownership Type, Sub-areas
             |
             v
  [ 7. Crops & Crop Varieties ]  -->  Create Crop, Add Variety
             |
             v
  [ 8. Plantations ]             -->  Allocate Area, Plant Variety, Terminate
             |
             v
  [ 9. Crop Cycles ]             -->  Create Cycle, Start, Harvest/Complete
             |
             v
  [ 10. Account Security ]       -->  Change Password & Logout
+-----------------------------------------------------------------------------------+
```

---

### Workflow 1: Authentication & Token Bootstrap

#### Objective
Validate SuperAdmin login, credential verification, JWT token issuance, HttpOnly cookie generation, and current profile hydration.

#### Step-by-Step Test Procedure:
1. Open browser and navigate to `http://localhost:4200/login`.
2. Inspect the page: Verify **Email** and **Password** inputs and "Sign In" button are rendered.
3. Enter SuperAdmin credentials:
   - **Email:** `admin@example.com`
   - **Password:** `Pa$$w0rd@123`
4. Click **Sign in**.
5. Observe network activity in browser DevTools:
   - Request: `POST http://localhost:8080/api/auth/login`
   - Payload: `{"email": "admin@example.com", "password": "Pa$$w0rd@123"}`
   - Status Code: `200 OK`
   - Response Payload: Contains `accessToken`, `expiresIn`, and `user` object.
   - Response Headers: Contains `Set-Cookie: fm_refresh_token=...; HttpOnly; SameSite=Lax; Path=/api/auth`.
6. Inspect the user object returned:
   - `roles` contains `"SuperAdmin"`.
   - `permissions` contains all 62 system permissions.
   - `organizationId` matches the seeded Demo Farm Organization GUID.
7. Verify immediate automatic redirect to `http://localhost:4200/dashboard`.
8. Verify current user profile endpoint:
   - Request: `GET http://localhost:8080/api/auth/me`
   - Headers: `Authorization: Bearer <accessToken>`
   - Status Code: `200 OK`

#### Negative Test Cases:
- **Invalid Password:** Enter correct email with wrong password. Verify HTTP `401 Unauthorized` with message `"Invalid email or password."`.
- **Account Lockout:** Enter wrong password 5 consecutive times. Verify user account locks for 15 minutes, returning HTTP `401 Unauthorized` indicating account lockout.

---

### Workflow 2: Shell Layout & Navigation Verification

#### Objective
Verify that SuperAdmin is granted access to all application modules in the main navigation sidebar.

#### Step-by-Step Test Procedure:
1. On `http://localhost:4200/dashboard`, inspect the side navigation menu.
2. Verify the following navigation items are visible:
   - **Dashboard** (`/dashboard`)
   - **Farms** (`/farms`)
   - **Crops** (`/crops`)
   - **Plantations** (`/plantations`)
   - **Crop cycles** (`/crop-cycles`)
   - **Activities** (`/activities`)
   - **Organization** (`/organization`)
   - **Administration** (`/administration`)
   - **Settings** (`/settings`)
3. Inspect top toolbar:
   - Verify SuperAdmin initials avatar displays `"IA"` (Initial Administrator).
   - Verify Logout button is present.
4. Click **Dashboard**:
   - Verify dashboard ping button triggers `GET /api/system/ping` and displays `"Backend is alive"`.

---

### Workflow 3: Multi-Organization Management

#### Objective
Validate that SuperAdmin can view all tenant organizations, create a new tenant organization, update an organization, and toggle active status.

#### Step-by-Step Test Procedure:

##### 3.1 List Organizations
1. Click **Organization** in the sidebar (Navigates to `/organization`).
2. Verify API call: `GET /api/organizations` (Permission: `Organization.View`).
3. Verify status `200 OK`.
4. Verify table shows seeded default organization:
   - Name: `Demo Farm Organization`
   - Code: `DEMO`
   - Status: `Active`

##### 3.2 Create a New Organization (SuperAdmin Exclusive)
1. Navigate to `http://localhost:4200/organization/new` (or click "Add Organization" / navigate directly).
   - *Note: Route guarded by `permissionGuard` with `permission: "Organization.Create"` and `role: "SuperAdmin"`.*
2. Fill the form:
   - **Name:** `Sunrise Vineyard Estates`
   - **Code:** `SUNRISE`
3. Click **Save** / **Submit**.
4. Verify API call:
   - Endpoint: `POST /api/organizations`
   - Request Body: `{"name": "Sunrise Vineyard Estates", "code": "SUNRISE"}`
   - Status Code: `201 Created`
5. Verify success toast: `"Organization created."`.
6. Verify redirected back to `/organization`, and the new organization `Sunrise Vineyard Estates` appears in the list.

##### 3.3 Update Organization Profile
1. On `/organization`, select the organization form.
2. Update **Name** to `Sunrise Agricultural Ventures`.
3. Click **Update Organization**.
4. Verify API call:
   - Endpoint: `PUT /api/organization`
   - Request Body: `{"name": "Sunrise Agricultural Ventures", "code": "SUNRISE"}`
   - Status Code: `200 OK`
5. Verify success snackbar notification.

##### 3.4 Deactivate & Activate Organization
1. On `/organization`, click **Deactivate Organization**.
2. Verify API call: `PATCH /api/organization/deactivate`.
3. Verify Status: `204 NoContent`.
4. Verify status badge changes to `Inactive`.
5. Click **Activate Organization**.
6. Verify API call: `PATCH /api/organization/activate`.
7. Verify Status: `204 NoContent`.
8. Status badge returns to `Active`.

#### Negative Test Cases:
- **Duplicate Organization Code:** Attempt to create an organization with code `DEMO`. Verify HTTP `409 Conflict` with message `"An organization with this code already exists."`.
- **Empty Organization Name:** Submit empty name. Verify client-side Angular validation and backend HTTP `400 Bad Request`.

---

### Workflow 4: Roles & Permissions Administration

#### Objective
Validate that SuperAdmin can list roles, inspect permissions, create a custom role, assign specific permissions, and toggle role activation.

#### Step-by-Step Test Procedure:

##### 4.1 View Roles
1. Navigate to `/administration` -> Click **Roles** (or URL `/administration/roles`).
2. Verify API call: `GET /api/roles?isActive=` (Permission: `Roles.View`).
3. Verify the 5 system roles are returned:
   - `SuperAdmin` (System Role)
   - `OrganizationAdmin` (System Role)
   - `FarmManager` (System Role)
   - `Supervisor` (System Role)
   - `Worker` (System Role)

##### 4.2 Create a Custom Role
1. Click **Create Role** button (Navigates to `/administration/roles/new`).
   - *Guarded by `permissionGuard` with `Roles.Create`.*
2. Fill form:
   - **Role Name:** `Agronomist`
   - **Description:** `Field agronomist responsible for crop inspection and cycles.`
3. Click **Save**.
4. Verify API call:
   - Endpoint: `POST /api/roles`
   - Payload: `{"name": "Agronomist", "description": "Field agronomist..."}`
   - Status Code: `201 Created`
5. Verify redirected to `/administration/roles`. Role `Agronomist` is visible with `Active` chip.

##### 4.3 Assign Permissions to Role
1. On `/administration/roles`, click **Edit** on `Agronomist` (Navigates to `/administration/roles/:id/edit`).
2. Scroll to permissions section / permission picker.
3. Select permissions:
   - `Crop.View`
   - `CropVariety.View`
   - `Plantation.View`
   - `CropCycle.View`
   - `CropCycle.Start`
   - `CropCycle.Complete`
4. Click **Update Role / Save Permissions**.
5. Verify API call:
   - Endpoint: `PUT /api/roles/{id}/permissions`
   - Request Body: `{"permissionIds": ["<GUID1>", "<GUID2>", ...]}`
   - Status Code: `200 OK`
6. Verify audit log entry created in backend.

##### 4.4 View System Permissions Catalog
1. Navigate to `/administration/permissions`.
2. Verify API call: `GET /api/permissions` (Permission: `Permissions.View`).
3. Verify all 62 permissions are displayed, grouped by module (`Administration`, `Roles`, `Permissions`, `Organization`, `Farms`, `Farm Areas`, `Crops`, `Plantations`, `Crop Cycles`, `Units`, etc.).

---

### Workflow 5: User Administration Across Organizations

#### Objective
Validate that SuperAdmin has global visibility over users across all organizations, can create users for any organization, assign any role (including SuperAdmin), and manage account status.

#### Step-by-Step Test Procedure:

##### 5.1 Global User Listing & Filtering
1. Navigate to `/administration/users`.
2. Verify API call: `GET /api/users?page=1&pageSize=20&search=&isActive=` (Permission: `Users.View`).
3. Note that SuperAdmin passes `organization_scope: "all"`, so backend sets `organizationId = null` in query, returning users across ALL organizations.
4. Test Search filter: Enter `Admin` in search box. Verify debounced API call: `GET /api/users?search=Admin`.
5. Test Status filter: Change dropdown to `Active`. Verify API call with `isActive=true`.

##### 5.2 Create User with Organization Selection
1. Click **Create User** button (Navigates to `/administration/users/new`).
   - *Guarded by `permissionGuard` with `Users.Create`.*
2. Check SuperAdmin-specific UI element:
   - Because `isGlobalAdministrator = true`, the **Organization** select dropdown is rendered and marked required!
   - Verify API call was made to load organizations: `GET /api/organizations`.
3. Fill form:
   - **First Name:** `Rajesh`
   - **Last Name:** `Patil`
   - **Email:** `rajesh.patil@example.com`
   - **Password:** `StrongPass2026!#`
   - **Confirm Password:** `StrongPass2026!#`
   - **Organization:** Select `Sunrise Agricultural Ventures`
   - **Roles:** Check `OrganizationAdmin` and `FarmManager`
4. Click **Save User**.
5. Verify API call:
   - Endpoint: `POST /api/users`
   - Request Body:
     ```json
     {
       "firstName": "Rajesh",
       "lastName": "Patil",
       "email": "rajesh.patil@example.com",
       "password": "StrongPass2026!#",
       "organizationId": "<SUNRISE_ORG_GUID>",
       "roleIds": ["<ROLE_GUID_1>", "<ROLE_GUID_2>"]
     }
     ```
   - Status Code: `201 Created`
6. Verify redirect to `/administration/users` and the new user appears in list.

##### 5.3 Edit User Details & Role Reassignment
1. In the users table, click **Edit** next to `Rajesh Patil` (URL `/administration/users/:id/edit`).
2. Update **Last Name** to `Patil-Deshmukh`.
3. In Roles multi-select, add the custom role `Agronomist`.
4. Click **Save**.
5. Verify API calls:
   - `PUT /api/users/{id}` (`Users.Update`) -> `200 OK`
   - `PUT /api/users/{id}/roles` (`Users.ManageRoles`) -> `200 OK`

##### 5.4 Deactivate, Activate & Unlock User
1. In user table action column:
2. Click **Deactivate**:
   - Verify API call: `POST /api/users/{id}/deactivate` (Permission: `Users.Deactivate`) -> `204 NoContent`.
   - Verify user status chip changes to `Inactive`.
3. Click **Activate**:
   - Verify API call: `POST /api/users/{id}/activate` (Permission: `Users.Activate`) -> `204 NoContent`.
   - Verify user status chip changes to `Active`.
4. Test Unlock (for locked accounts):
   - Trigger account lockout on a test user.
   - Click **Unlock**:
   - Verify API call: `POST /api/users/{id}/unlock` (Permission: `Users.Unlock`) -> `204 NoContent`.
   - Verify `failed_login_count` is reset to 0 and `lockout_end` is cleared.

#### Negative Test Cases:
- **Weak Password:** Submit password `weak` (fails min length, regex). Verify validation errors under password input.
- **Duplicate Email:** Submit existing email `admin@example.com`. Verify HTTP `409 Conflict` with message `"A user with this email address already exists."`.
- **Non-SuperAdmin Assigning SuperAdmin:** Log in as `OrganizationAdmin` and attempt to assign `SuperAdmin` role via `PUT /api/users/:id/roles`. Verify HTTP `403 Forbidden` with `"Only a SuperAdmin can assign the SuperAdmin role."`.

---

### Workflow 6: Farm Management

#### Objective
Validate that SuperAdmin can list, create, view details, edit, and activate/deactivate farms.

#### Step-by-Step Test Procedure:

##### 6.1 List Farms
1. Click **Farms** in sidebar (Navigates to `/farms`).
2. Verify API call: `GET /api/farms?page=1&pageSize=20` (Permission: `Farm.View`).
3. Verify table displays columns: Farm Name/Code, Ownership, Location, Area, Status, Actions.

##### 6.2 Create Farm
1. Click **Create Farm** (Navigates to `/farms/new`).
   - *Guarded by `permissionGuard` with `Farm.Create`.*
2. Form fields verification:
   - Verify dropdown for **Ownership Type** loads from `GET /api/master-data/farm-ownership-types` (`OWNED`, `LEASED`, `RENTED`, `MANAGED`, `OTHER`).
   - Verify dropdown for **Area Unit** loads from `GET /api/master-data/units?category=Area` (`ACRE`, `HECTARE`, etc.).
3. Enter values:
   - **Name:** `Nashik Valley Vineyard`
   - **Code:** `NVV-01`
   - **Ownership Type:** `OWNED`
   - **Total Area:** `25.5`
   - **Area Unit:** `Acre`
   - **Address Line 1:** `Plot 42, Dindori Road`
   - **City:** `Nashik`
   - **State:** `Maharashtra`
   - **Country:** `India`
   - **Latitude:** `20.0059`
   - **Longitude:** `73.7898`
4. Click **Save Farm**.
5. Verify API call:
   - Endpoint: `POST /api/farms`
   - Status Code: `201 Created`
6. Verify redirect to `/farms` with success message.

##### 6.3 Farm Details & Areas Navigation
1. Click on `Nashik Valley Vineyard` (Navigates to `/farms/:id`).
2. Verify API call: `GET /api/farms/{id}` (Permission: `Farm.View`).
3. Verify Farm Overview cards display: Status (Active), Ownership (Owned), Total Area (25.5 ac), Location (Nashik).
4. Verify the nested section **Farm areas** is displayed with an **Add Area** button.

##### 6.4 Edit & Deactivate Farm
1. Click **Edit Farm** button (`/farms/:id/edit`).
2. Update **Total Area** to `30.0`. Click **Save**.
3. Verify API call: `PUT /api/farms/{id}` (Permission: `Farm.Update`) -> `200 OK`.
4. In farms list, click **Deactivate**:
   - Verify API call: `PATCH /api/farms/{id}/deactivate` (Permission: `Farm.Deactivate`) -> `204 NoContent`.
5. Click **Activate**:
   - Verify API call: `PATCH /api/farms/{id}/activate` (Permission: `Farm.Activate`) -> `204 NoContent`.

---

### Workflow 7: Farm Area & Sub-Area Management

#### Objective
Validate farm area creation, the 1-level hierarchy rule (Area -> Sub Area), area allocation rules, and area detail viewing.

#### Step-by-Step Test Procedure:

##### 7.1 Create Top-Level Farm Area
1. From Farm Details `/farms/:id` (or directly at `/farm-areas/new`), click **Add Area**.
2. Fill form:
   - **Farm:** `Nashik Valley Vineyard`
   - **Area Name:** `North Block`
   - **Area Code:** `NB-01`
   - **Parent Area:** None (leave empty for top-level area)
   - **Size:** `15.0`
   - **Area Unit:** `Acre`
3. Click **Save Area**.
4. Verify API call:
   - Endpoint: `POST /api/farm-areas`
   - Request Body: `{"farmId": "...", "parentAreaId": null, "name": "North Block", "code": "NB-01", "size": 15.0, "areaUnitId": "..."}`
   - Status Code: `201 Created`

##### 7.2 Create Sub-Area (1-Level Hierarchy Rule)
1. Navigate to `/farm-areas/new`.
2. Fill form:
   - **Farm:** `Nashik Valley Vineyard`
   - **Area Name:** `Section 1A`
   - **Area Code:** `NB-SEC-1A`
   - **Parent Area:** Select `North Block`
   - **Size:** `8.0`
   - **Area Unit:** `Acre`
3. Click **Save Area**.
4. Verify API call: `POST /api/farm-areas` -> `201 Created`.

##### 7.3 Area Hierarchy Rule & Allocation Boundary Tests:
- **Rule 1: Sub-Area cannot have another sub-area (Max 1 level):**
  Attempt to create an area with `parentAreaId` pointing to `Section 1A`.
  Verify API returns HTTP `400 Bad Request` or `ValidationException` stating `"A sub-area cannot be parent to another area."`.
- **Rule 2: Child Area size cannot exceed parent area size:**
  Attempt to create a sub-area under `North Block` (size 15 ac) with size `16.0` ac.
  Verify API returns HTTP `400 Bad Request` with allocation validation error.

##### 7.4 Farm Area Details & Status
1. Navigate to `/farm-areas/:id`.
2. Verify API call: `GET /api/farm-areas/{id}` (Permission: `FarmArea.View`).
3. Toggle status: `PATCH /api/farm-areas/{id}/deactivate` and `PATCH /api/farm-areas/{id}/activate`.

---

### Workflow 8: Crop & Variety Catalog Management

#### Objective
Validate crop registration, crop types (Fruit/Vegetable/Perennial/Annual), crop varieties, and variety lifecycle.

#### Step-by-Step Test Procedure:

##### 8.1 View Seeded Crops
1. Click **Crops** in sidebar (Navigates to `/crops`).
2. Verify API call: `GET /api/crops?page=1&pageSize=20` (Permission: `Crop.View`).
3. Verify seeded system crops are displayed:
   - `GRAPES` (Fruit, Perennial)
   - `TOMATO` (Vegetable, Annual)
   - `CHILI` (Vegetable, Seasonal)
   - `MANGO` (Fruit, Perennial)
   - `BANANA` (Fruit, Perennial)

##### 8.2 Create a New Crop
1. Click **Create Crop** (Navigates to `/crops/new`).
   - *Guarded by `Crop.Create`.*
2. Fill form:
   - **Crop Name:** `Pomegranate`
   - **Crop Code:** `POMEGRANATE`
   - **Category:** `Fruit`
   - **Growth Habit:** `Perennial`
3. Click **Save Crop**.
4. Verify API call: `POST /api/crops` -> `201 Created`.

##### 8.3 Manage Crop Varieties
1. Click on `GRAPES` in the crops list (Navigates to `/crops/:id`).
2. Verify API call: `GET /api/crops/{cropId}/varieties` (Permission: `CropVariety.View`).
3. Verify seeded varieties are shown:
   - `Thompson Seedless` (`THOMPSON_SEEDLESS`)
   - `Sharad Seedless` (`SHARAD_SEEDLESS`)
   - `Sonaka` (`SONAKA`)
   - `Manik Chaman` (`MANIK_CHAMAN`)
4. Add a new variety:
   - Click **Add Variety** / fill variety form.
   - **Name:** `Crimson Seedless`
   - **Code:** `CRIMSON_SEEDLESS`
5. Verify API call:
   - Endpoint: `POST /api/crop-varieties`
   - Request Body: `{"cropId": "<GRAPES_GUID>", "name": "Crimson Seedless", "code": "CRIMSON_SEEDLESS"}`
   - Status Code: `201 Created`
6. Toggle variety status:
   - Call `PATCH /api/crop-varieties/{id}/deactivate` -> `204 NoContent`.
   - Call `PATCH /api/crop-varieties/{id}/activate` -> `204 NoContent`.

---

### Workflow 9: Plantation Management

#### Objective
Validate plantation creation on a farm area with a single variety, area allocation validation, and plantation termination with end reasons.

#### Step-by-Step Test Procedure:

##### 9.1 View Plantations List
1. Click **Plantations** in sidebar (Navigates to `/plantations`).
2. Verify API call: `GET /api/plantations` (Permission: `Plantation.View`).

##### 9.2 Create Plantation
1. Click **Create Plantation** (Navigates to `/plantations/new`).
   - *Guarded by `Plantation.Create`.*
2. Fill form:
   - **Farm:** Select `Nashik Valley Vineyard`
   - **Farm Area:** Select `Section 1A` (Size: 8.0 ac)
   - **Crop:** Select `Grapes`
   - **Variety:** Select `Thompson Seedless` (One variety per plantation rule)
   - **Planted Area:** `5.0` (Unit: Acre)
   - **Planting Date:** Current date
   - **Plant Count / Vine Count:** `2500`
   - **Row Spacing:** `9 ft`
   - **Plant Spacing:** `5 ft`
3. Click **Save Plantation**.
4. Verify API call:
   - Endpoint: `POST /api/plantations`
   - Status Code: `201 Created`

##### 9.3 Area Allocation Validation (Phase 2 Rule)
1. Attempt to create a second plantation on `Section 1A` (Remaining area: 8.0 - 5.0 = 3.0 ac) with Planted Area = `4.0` ac.
2. Verify API returns HTTP `400 Bad Request` with message: `"Total active allocated area (9.0) exceeds farm area size (8.0)."`.
3. Change Planted Area to `3.0` ac and submit -> Verify HTTP `201 Created` (Total active allocation is now exactly 8.0 ac).

##### 9.4 Terminate Plantation (with Mandatory End Reason)
1. Navigate to `/plantations/:id`.
2. Click **Terminate Plantation**.
3. Select End Reason from dropdown:
   - Options loaded from `GET /api/master-data/plantation-end-reasons`:
     - `HARVEST_COMPLETED`
     - `WEATHER_DISASTER`
     - `PEST_INFESTATION`
     - `DISEASE`
     - `CROP_FAILURE`
     - `REPLANT_REQUIRED`, etc.
   - Select `WEATHER_DISASTER`.
4. Enter termination notes: `"Severe unseasonal hailstorm destroyed vines."`.
5. Click **Confirm Termination**.
6. Verify API call:
   - Endpoint: `POST /api/plantations/{id}/terminate`
   - Payload: `{"reasonCode": "WEATHER_DISASTER", "notes": "Severe unseasonal hailstorm..."}`
   - Status Code: `204 NoContent`
7. Verify plantation status changes to `TERMINATED`.
8. Verify area is freed up: Re-check `Section 1A` allocation capacity.

---

### Workflow 10: Crop Cycle / Season Management

#### Objective
Validate crop cycle creation for a plantation, lifecycle transitions (Draft -> InProgress -> Harvested -> Completed / Cancelled).

#### Step-by-Step Test Procedure:

##### 10.1 View Crop Cycles
1. Click **Crop cycles** in sidebar (Navigates to `/crop-cycles`).
2. Verify API call: `GET /api/crop-cycles` (Permission: `CropCycle.View`).

##### 10.2 Create Crop Cycle
1. Click **Create Crop Cycle** (Navigates to `/crop-cycles/new`).
   - *Guarded by `CropCycle.Create`.*
2. Fill form:
   - **Plantation:** Select active Grape plantation
   - **Cycle Name / Code:** `NVV-2026-SEASON-1`
   - **Season Year:** `2026`
   - **Target Yield:** `12000`
   - **Yield Unit:** `KILOGRAM`
   - **Planned Start Date:** `2026-10-01`
   - **Planned End Date:** `2027-03-31`
3. Click **Save Crop Cycle**.
4. Verify API call: `POST /api/crop-cycles` -> `201 Created`.
5. Initial cycle status is `Draft` or `Planned`.

##### 10.3 Start Crop Cycle
1. Navigate to `/crop-cycles/:id`.
2. Click **Start Cycle**.
3. Enter **Actual Start Date**: `2026-10-01`.
4. Verify API call:
   - Endpoint: `POST /api/crop-cycles/{id}/start`
   - Payload: `{"actualStartDate": "2026-10-01T00:00:00Z"}`
   - Status Code: `204 NoContent`
5. Status updates to `Active` / `InProgress`.

##### 10.4 Record Harvest & Complete Cycle
1. On active crop cycle `/crop-cycles/:id`, click **Record Harvest**.
2. Enter **Harvest Date**: `2027-03-15`, **Actual Yield**: `11500`, **Yield Unit**: `KILOGRAM`.
3. Verify API call:
   - Endpoint: `POST /api/crop-cycles/{id}/harvest`
   - Status Code: `204 NoContent`
4. Click **Complete Cycle**:
   - Verify API call: `POST /api/crop-cycles/{id}/complete` -> `204 NoContent`.
   - Cycle status transitions to `Completed`.

##### 10.5 Cancel Cycle (Alternative Path)
1. For a planned/active cycle that suffered disaster, click **Cancel Cycle**.
2. Enter cancellation reason: `"Frost damage destroyed budding."`.
3. Verify API call:
   - Endpoint: `POST /api/crop-cycles/{id}/cancel`
   - Payload: `{"reason": "Frost damage destroyed budding."}`
   - Status Code: `204 NoContent`
4. Cycle status transitions to `Cancelled`.

---

### Workflow 11: Master Data Verification

#### Objective
Verify that master data tables are seeded and can be queried by the application for dropdowns.

#### Step-by-Step Test Procedure:
Execute API GET calls directly or through UI forms:

1. **Units of Measurement:**
   - Call `GET http://localhost:8080/api/master-data/units` (Permission: `Unit.View`).
   - Verify categories returned: `Area` (Acre, Hectare, m²), `Weight` (kg, g, ton, quintal), `Volume` (L, mL), `Length` (m, cm, ft), `Count` (number, piece, plant).
   - Filter by category: `GET /api/master-data/units?category=Area`.
2. **Farm Ownership Types:**
   - Call `GET http://localhost:8080/api/master-data/farm-ownership-types` (Permission: `Farm.View`).
   - Verify codes: `OWNED`, `LEASED`, `RENTED`, `MANAGED`, `OTHER`.
3. **Plantation End Reasons:**
   - Call `GET http://localhost:8080/api/master-data/plantation-end-reasons` (Permission: `PlantationEndReason.View`).
   - Verify all 13 standard reasons are present: `HARVEST_COMPLETED`, `WEATHER_DISASTER`, `FLOOD`, `DROUGHT`, `CYCLONE`, `PEST_INFESTATION`, `DISEASE`, `CROP_FAILURE`, `POOR_CROP_HEALTH`, `SOIL_PROBLEM`, `REPLANT_REQUIRED`, `FARMER_DECISION`, `OTHER`.

---

### Workflow 12: Account Settings & Session Termination

#### Objective
Validate SuperAdmin password self-service, password validation rules, and secure session logout.

#### Step-by-Step Test Procedure:

##### 12.1 Change Password
1. In sidebar, click **Settings** -> **Change password** (URL `/settings/change-password`).
2. Test Password Policy:
   - Enter weak password (e.g., `pass123`). Verify validation errors (min 12 chars, uppercase, lowercase, digit, special char).
3. Enter valid change password details:
   - **Current Password:** `Pa$$w0rd@123`
   - **New Password:** `SuperSecure2026!#`
   - **Confirm Password:** `SuperSecure2026!#`
4. Click **Change Password**.
5. Verify API call:
   - Endpoint: `POST /api/auth/change-password`
   - Payload: `{"currentPassword": "...", "newPassword": "..."}`
   - Status Code: `204 NoContent`
6. Verify refresh token cookie is deleted / rotated.

##### 12.2 Session Logout
1. Click **Logout** button in top bar.
2. Verify API call: `POST /api/auth/logout`.
3. Verify Status: `204 NoContent`.
4. Verify `fm_refresh_token` cookie is deleted (`Max-Age=0` or expired).
5. Verify Angular memory access token is cleared.
6. Verify redirect to `/login`.
7. Attempting to navigate back to `/dashboard` immediately redirects back to `/login` via `authGuard`.

---

## 5. Complete Page Access & Permission Mapping Matrix

| Route Path | Page Component | Angular Guard & Requirements | Visible UI Actions | Backend API Endpoint & Method | Backend Policy Required | Expected Status Codes |
|---|---|---|---|---|---|:---:|
| `/login` | `LoginPageComponent` | Public (`AllowAnonymous`) | Submit login | `POST /api/auth/login` | None | `200`, `400`, `401` |
| `/forbidden` | `ForbiddenPageComponent` | Public | Back to home link | None (static screen) | None | `200` |
| `/dashboard` | `DashboardPageComponent` | `authGuard` | View stats, Backend ping | `GET /api/system/ping` | None (Anonymous) | `200` |
| `/organization` | `OrganizationPageComponent` | `permissionGuard` (`Organization.View`) | List orgs, view details, edit profile, toggle status | `GET /api/organizations`<br>`GET /api/organization`<br>`PUT /api/organization`<br>`PATCH /api/organization/activate`<br>`PATCH /api/organization/deactivate` | `Organization.View`<br>`Organization.View`<br>`Organization.Update`<br>`Organization.Activate`<br>`Organization.Deactivate` | `200`, `204`, `400`, `403` |
| `/organization/new` | `OrganizationPageComponent` | `permissionGuard` (`Organization.Create`, Role: `SuperAdmin`) | Create new tenant organization | `POST /api/organizations` | `Organization.Create` | `201`, `400`, `403`, `409` |
| `/administration` | `AdministrationPageComponent` | `permissionGuard` (`Users.View` or `Roles.View` or `Permissions.View`) | Nav cards to Users, Roles, Permissions | None (Routing shell) | None | `200` |
| `/administration/users` | `UsersPageComponent` | `permissionGuard` (`Users.View`) | Search, filter, pagination, activate, deactivate, unlock | `GET /api/users`<br>`POST /api/users/{id}/activate`<br>`POST /api/users/{id}/deactivate`<br>`POST /api/users/{id}/unlock` | `Users.View`<br>`Users.Activate`<br>`Users.Deactivate`<br>`Users.Unlock` | `200`, `204`, `403`, `404` |
| `/administration/users/new` | `UserEditorPageComponent` | `permissionGuard` (`Users.Create`) | Organization dropdown, assign roles, create user | `POST /api/users`<br>`GET /api/roles`<br>`GET /api/organizations` | `Users.Create`<br>`Roles.View`<br>`Organization.View` | `201`, `400`, `403`, `409` |
| `/administration/users/:id/edit` | `UserEditorPageComponent` | `permissionGuard` (`Users.Update`) | Edit name, reassign roles | `GET /api/users/{id}`<br>`PUT /api/users/{id}`<br>`PUT /api/users/{id}/roles` | `Users.View`<br>`Users.Update`<br>`Users.ManageRoles` | `200`, `400`, `403`, `404` |
| `/administration/roles` | `RolesPageComponent` | `permissionGuard` (`Roles.View`) | List roles, activate/deactivate role | `GET /api/roles`<br>`POST /api/roles/{id}/activate`<br>`POST /api/roles/{id}/deactivate` | `Roles.View`<br>`Roles.Activate`<br>`Roles.Deactivate` | `200`, `204`, `403` |
| `/administration/roles/new` | `RoleEditorPageComponent` | `permissionGuard` (`Roles.Create`) | Role name, description | `POST /api/roles` | `Roles.Create` | `201`, `400`, `403`, `409` |
| `/administration/roles/:id/edit` | `RoleEditorPageComponent` | `permissionGuard` (`Roles.Update`) | Edit role name/desc, assign permissions | `GET /api/roles/{id}`<br>`PUT /api/roles/{id}`<br>`PUT /api/roles/{id}/permissions` | `Roles.View`<br>`Roles.Update`<br>`Roles.ManagePermissions` | `200`, `400`, `403`, `404` |
| `/administration/permissions` | `PermissionsPageComponent` | `permissionGuard` (`Permissions.View`) | View system permissions grouped by module | `GET /api/permissions` | `Permissions.View` | `200`, `403` |
| `/farms` | `FarmsPageComponent` | `permissionGuard` (`Farm.View`) | Search, filter, pagination, activate/deactivate | `GET /api/farms`<br>`PATCH /api/farms/{id}/activate`<br>`PATCH /api/farms/{id}/deactivate` | `Farm.View`<br>`Farm.Activate`<br>`Farm.Deactivate` | `200`, `204`, `403` |
| `/farms/new` | `FarmEditorPageComponent` | `permissionGuard` (`Farm.Create`) | Farm name, code, ownership, area, coordinates | `POST /api/farms`<br>`GET /api/master-data/units`<br>`GET /api/master-data/farm-ownership-types` | `Farm.Create`<br>`Unit.View`<br>`Farm.View` | `201`, `400`, `403`, `409` |
| `/farms/:id` | `FarmDetailPageComponent` | `permissionGuard` (`Farm.View`) | Overview stats, list farm areas, link to add area | `GET /api/farms/{id}`<br>`GET /api/farms/{id}/areas` | `Farm.View`<br>`FarmArea.View` | `200`, `404` |
| `/farms/:id/edit` | `FarmEditorPageComponent` | `permissionGuard` (`Farm.Update`) | Edit farm details | `PUT /api/farms/{id}` | `Farm.Update` | `200`, `400`, `403`, `404` |
| `/farm-areas` | `FarmAreasPageComponent` | `permissionGuard` (`FarmArea.View`) | List all areas across farms, filter, toggle status | `GET /api/farms`<br>`GET /api/farms/{farmId}/areas` | `Farm.View`<br>`FarmArea.View` | `200`, `403` |
| `/farm-areas/new` | `FarmAreaEditorPageComponent` | `permissionGuard` (`FarmArea.Create`) | Farm selector, parent area, code, size, unit | `POST /api/farm-areas` | `FarmArea.Create` | `201`, `400`, `403`, `409` |
| `/farm-areas/:id` | `FarmAreaDetailPageComponent` | `permissionGuard` (`FarmArea.View`) | View area details, sub-areas | `GET /api/farm-areas/{id}` | `FarmArea.View` | `200`, `404` |
| `/farm-areas/:id/edit` | `FarmAreaEditorPageComponent` | `permissionGuard` (`FarmArea.Update`) | Update area size/details | `PUT /api/farm-areas/{id}` | `FarmArea.Update` | `200`, `400`, `403`, `404` |
| `/crops` | `CropsPageComponent` | `permissionGuard` (`Crop.View`) | List crops, search, filter, activate/deactivate | `GET /api/crops`<br>`PATCH /api/crops/{id}/activate`<br>`PATCH /api/crops/{id}/deactivate` | `Crop.View`<br>`Crop.Activate`<br>`Crop.Deactivate` | `200`, `204`, `403` |
| `/crops/new` | `CropEditorPageComponent` | `permissionGuard` (`Crop.Create`) | Crop name, code, category, growth habit | `POST /api/crops` | `Crop.Create` | `201`, `400`, `403`, `409` |
| `/crops/:id` | `CropDetailPageComponent` | `permissionGuard` (`Crop.View`) | Crop details, list varieties, add variety | `GET /api/crops/{id}`<br>`GET /api/crops/{id}/varieties`<br>`POST /api/crop-varieties` | `Crop.View`<br>`CropVariety.View`<br>`CropVariety.Create` | `200`, `201`, `404` |
| `/crops/:id/edit` | `CropEditorPageComponent` | `permissionGuard` (`Crop.Update`) | Edit crop metadata | `PUT /api/crops/{id}` | `Crop.Update` | `200`, `400`, `403`, `404` |
| `/plantations` | `PlantationsPageComponent` | `permissionGuard` (`Plantation.View`) | List plantations, filter by status | `GET /api/plantations` | `Plantation.View` | `200`, `403` |
| `/plantations/new` | `PlantationEditorPageComponent` | `permissionGuard` (`Plantation.Create`) | Farm, area, crop, variety, size, planting date | `POST /api/plantations` | `Plantation.Create` | `201`, `400`, `403`, `409` |
| `/plantations/:id` | `PlantationDetailPageComponent` | `permissionGuard` (`Plantation.View`) | Overview, terminate with reason, archive | `GET /api/plantations/{id}`<br>`POST /api/plantations/{id}/terminate`<br>`POST /api/plantations/{id}/archive` | `Plantation.View`<br>`Plantation.Terminate`<br>`Plantation.Update` | `200`, `204`, `400`, `404` |
| `/crop-cycles` | `CropCyclesPageComponent` | `permissionGuard` (`CropCycle.View`) | List cycles, filter by status / year | `GET /api/crop-cycles` | `CropCycle.View` | `200`, `403` |
| `/crop-cycles/new` | `CropCycleEditorPageComponent` | `permissionGuard` (`CropCycle.Create`) | Plantation selector, season year, dates, target yield | `POST /api/crop-cycles` | `CropCycle.Create` | `201`, `400`, `403` |
| `/crop-cycles/:id` | `CropCycleDetailPageComponent` | `permissionGuard` (`CropCycle.View`) | Details, Start, Harvest, Complete, Cancel | `GET /api/crop-cycles/{id}`<br>`POST /api/crop-cycles/{id}/start`<br>`POST /api/crop-cycles/{id}/harvest`<br>`POST /api/crop-cycles/{id}/complete`<br>`POST /api/crop-cycles/{id}/cancel` | `CropCycle.View`<br>`CropCycle.Start`<br>`CropCycle.Complete`<br>`CropCycle.Complete`<br>`CropCycle.Cancel` | `200`, `204`, `400`, `404` |
| `/activities` | `ActivitiesPageComponent` | `authGuard` | **UI Placeholder** | None (Phase 3) | None | `200` |
| `/settings/change-password` | `ChangePasswordPageComponent` | `authGuard` | Current password, new password, confirm | `POST /api/auth/change-password` | Authorized User | `204`, `400`, `401` |

---

## 6. Implementation Status & Gap Analysis for QA

This section is vital for QA agents so that known architectural decisions or deferred phase items are not logged as defects.

### 6.1 Status Summary Table

| Functional Area | Implementation Status | QA Testing Guidance |
|---|:---:|---|
| **Authentication & Refresh Tokens** | **Complete** | Test login, refresh token rotation, HttpOnly cookie, profile lookup. |
| **Password Hashing & Lockout** | **Complete** | Test lockout after 5 attempts, lockout expiry, and unlock endpoint. |
| **Global Organization Management** | **Complete** | SuperAdmin can create organizations and list all organizations. |
| **User Administration** | **Complete** | SuperAdmin can select tenant org and assign any role (including SuperAdmin). |
| **Role & Permission Management** | **Complete** | Custom roles can be created, edited, deactivated, and assigned permissions. |
| **Farms & Farm Areas** | **Complete** | Full CRUD, area allocation checks, 1-level hierarchy validation. |
| **Crops & Varieties** | **Complete** | Full CRUD, type categorization, variety management. |
| **Plantations & Allocations** | **Complete** | Area allocation constraint (`Allocated <= Total Area`), single variety rule, termination reasons. |
| **Crop Cycles** | **Complete** | Lifecycle state machine (Draft -> Active -> Harvested -> Completed / Cancelled). |
| **Crop Lifecycle Templates (Backend)** | **Complete** | Backend controllers and services are fully built (`/api/crop-lifecycle-templates`). |
| **Crop Lifecycle Templates (UI)** | **Missing** | **No frontend UI yet.** Angular routes and screens for templates/stages have not been built. |
| **Activities Management** | **Placeholder** | Route `/activities` displays `PagePlaceholderComponent`. Backend is Phase 3. |
| **Tenant Switching Context for SuperAdmin** | **Partial** | While SuperAdmin has global user/org permissions, agricultural domain services (`FarmService`, `CropService`) scope by `actor.OrganizationId`. SuperAdmin currently acts within their home tenant (`Demo Farm Organization`) unless tenant-switching UI/claims are added. |
| **Master Data Management UI** | **Read-Only / Form Bound** | Master data (Units, End Reasons) can be listed by API, but no dedicated management screen exists (values are seeded in DB). |
| **Audit Log Viewing UI** | **Missing** | Audit logs are recorded in the `audit_logs` database table, but no API endpoint or UI exists to view audit records. |
| **Automated Test Projects** | **Missing** | Solution currently has no `tests/` directory or automated unit/integration test projects. |

### 6.2 Known Nuances & Expected Behaviors

1. **SuperAdmin Tenant Scoping in Farm Domain:**
   - In `UserAdministrationService` and `OrganizationService`, the backend explicitly checks `actor.CanManageAllOrganizations` (`organization_scope: "all"`).
   - In `FarmService`, `FarmAreaService`, `PlantationService`, and `CropCycleService`, the query is scoped to `actor.OrganizationId`. Therefore, farms created by SuperAdmin belong to the organization tied to their user account (`Demo Farm Organization`). A global organization switcher dropdown is not yet implemented.
2. **Missing Phase 2 Migration Files:**
   - As noted in `IMPLEMENTATION-STATUS.md`, migration numbering contains jumps (`Phase2_001`, `Phase2_003`, `Phase2_004`, `Phase2_005`, `Phase2_006`, `Phase2_008`, `Phase2_009`).
   - `Phase2_002`, `Phase2_007`, and `Phase2_010` were merged directly into earlier migrations or seeders. The database schema in `ApplicationDbContextModelSnapshot.cs` is complete and functional.
3. **Activities Module:**
   - Clicking **Activities** in the sidebar intentionally renders a placeholder: `"Activities: Plan and track farming operations across plots."`. This is expected behavior for Phase 1 & 2.

---

## 7. QA Test Sign-Off Checklist

Use this checklist during QA execution to record pass/fail results:

| Test ID | Test Scenario Description | Expected Result | Pass / Fail | Notes |
|:---:|---|---|:---:|---|
| **TC-SA-01** | SuperAdmin login with initial seeded credentials | HTTP 200, JWT issued, HttpOnly refresh cookie set, redirect to `/dashboard` | [ ] | |
| **TC-SA-02** | Token claims inspection (`sub`, `roles: ["SuperAdmin"]`, `organization_scope: "all"`) | All 62 permissions present, org scope is `"all"` | [ ] | |
| **TC-SA-03** | Sidebar navigation items check | All 9 menu items visible, user initials display `"IA"` | [ ] | |
| **TC-SA-04** | View all organizations on `/organization` | Displays `Demo Farm Organization` | [ ] | |
| **TC-SA-05** | Create new tenant organization (`/organization/new`) | HTTP 201 Created, appears in organization list | [ ] | |
| **TC-SA-06** | Update organization name and toggle active/inactive | HTTP 200 / 204, status badge updates in real time | [ ] | |
| **TC-SA-07** | List system roles on `/administration/roles` | 5 seeded system roles displayed | [ ] | |
| **TC-SA-08** | Create custom role `Agronomist` and assign permissions | Role created, permissions saved via `PUT /api/roles/:id/permissions` | [ ] | |
| **TC-SA-09** | Global user listing on `/administration/users` | Users from all organizations are listed | [ ] | |
| **TC-SA-10** | Create user under newly created tenant organization | Organization dropdown visible, user successfully created with assigned roles | [ ] | |
| **TC-SA-11** | User deactivation, activation, and lockout unlock | Status toggles correctly, failed count resets to 0 | [ ] | |
| **TC-SA-12** | Create farm with area unit and ownership type | HTTP 201 Created, overview cards update | [ ] | |
| **TC-SA-13** | Create top-level farm area and 1-level sub-area | Areas created successfully under farm | [ ] | |
| **TC-SA-14** | Enforce 1-level hierarchy rule on sub-area | API rejects 2nd-level sub-area with HTTP 400 | [ ] | |
| **TC-SA-15** | Enforce farm area allocation capacity check | Area creation/plantation allocation exceeding capacity rejected | [ ] | |
| **TC-SA-16** | Create crop and add variety | Crop created, variety listed under crop | [ ] | |
| **TC-SA-17** | Create plantation with variety and allocated acreage | Plantation active, area allocated | [ ] | |
| **TC-SA-18** | Terminate plantation with mandatory reason code | Status changed to `TERMINATED`, reason recorded, area capacity freed | [ ] | |
| **TC-SA-19** | Crop cycle lifecycle: Create -> Start -> Harvest -> Complete | Status transitions from Draft -> Active -> Harvested -> Completed | [ ] | |
| **TC-SA-20** | Change password self-service & Logout | Password updated, refresh cookie invalidated, redirect to `/login` | [ ] | |
