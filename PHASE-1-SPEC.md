# Farm Management Platform
# Phase 1 – Identity, Authentication and Administration Specification

**Document:** `PHASE-1-SPEC.md`  
**Version:** 1.0  
**Status:** Ready for Implementation  
**Prerequisite:** Phase 0 completed  
**Frontend:** Angular  
**Backend:** ASP.NET Core Web API  
**Database:** PostgreSQL  
**Infrastructure:** Docker Compose  

---

# 1. Phase 1 Purpose

Phase 1 establishes the security and administration foundation for the Farm Management Platform.

The platform is designed for:

- Multiple organizations
- Multiple farms per organization
- Multiple users
- Role-based access
- Permission-based authorization
- Web administration using Angular
- Future React Native mobile support

Phase 1 must provide a secure authentication and authorization architecture before farm and crop business functionality is introduced.

---

# 2. Confirmed Architecture Decisions

The following decisions are final for Phase 1.

## 2.1 Organization Model

The application must be multi-organization ready.

```text
Organization A
│
├── Organization Admin
├── Farm Manager
├── Supervisor
└── Worker


Organization B
│
├── Organization Admin
├── Farm Manager
└── Worker
```

Organization data must be isolated.

A user belonging to Organization A must not access Organization B data.

---

## 2.2 User Creation

User creation is performed by:

```text
SuperAdmin
      │
      ▼
Can manage all organizations


OrganizationAdmin
      │
      ▼
Can manage users only in their organization
```

Public registration is not allowed.

---

## 2.3 Roles

The initial roles are:

```text
SuperAdmin

OrganizationAdmin

FarmManager

Supervisor

Worker
```

Roles are configurable in the database.

Do not hardcode authorization decisions based only on role names.

Permissions must control access.

---

## 2.4 Authorization Model

The system uses:

```text
User
  │
  ▼
User Roles
  │
  ▼
Roles
  │
  ▼
Role Permissions
  │
  ▼
Permissions
```

Authorization must support granular permissions.

Example:

```text
Users.Create

Users.View

Users.Update

Users.ManageRoles
```

---

## 2.5 Authentication Model

### Angular Web

```text
Access Token
      │
      ▼
Angular Memory


Refresh Token
      │
      ▼
HttpOnly Cookie
```

Angular JavaScript must not access the refresh token.

---

## 2.6 Future React Native Mobile

Future mobile architecture:

```text
Access Token
      │
      ▼
Secure Storage


Refresh Token
      │
      ▼
Secure Storage
```

The backend authentication architecture must support both web and mobile clients.

---

## 2.7 Password Reset

Phase 1 includes:

```text
Change Password
```

Phase 1 does not include:

```text
Forgot Password

Email Password Reset
```

These may be added in a future phase.

---

## 2.8 User Deletion

Users must not be hard deleted.

Use:

```text
is_active
```

Example:

```text
Active User

↓

Organization Admin disables user

↓

is_active = false

↓

User cannot log in
```

Historical data remains available.

---

# 3. Phase 1 Scope

## Included

```text
Organization Foundation

User Management

Roles

Permissions

JWT Authentication

Refresh Tokens

Login

Logout

Change Password

Account Lockout

Angular Login

Auth Guard

Permission Guard

HTTP Auth Interceptor

HTTP Error Interceptor

Global Error Middleware

Audit Logging

Administration UI
```

---

## Excluded

```text
Farm CRUD

Field CRUD

Crop CRUD

Activity Management

Inventory

Labour

Expenses

Harvest

Forgot Password

Email Service

React Native Application
```

---

# 4. Database Architecture

The following tables are required.

```text
organizations

users

roles

permissions

user_roles

role_permissions

refresh_tokens

audit_logs
```

Relationships:

```text
Organization
     │
     │ 1
     │
     ▼
Users
     │
     │ Many-to-Many
     ▼
Roles
     │
     │ Many-to-Many
     ▼
Permissions


User
 │
 └──── Refresh Tokens


User
 │
 └──── Audit Logs
```

---

# 5. Database Schema

## 5.1 organizations

```sql
CREATE TABLE organizations
(
    id UUID PRIMARY KEY,

    name VARCHAR(200) NOT NULL,

    code VARCHAR(50) NOT NULL UNIQUE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL,

    updated_at TIMESTAMPTZ NULL
);
```

### Fields

| Field | Description |
|---|---|
| id | Primary key |
| name | Organization name |
| code | Unique organization identifier |
| is_active | Organization status |
| created_at | Creation timestamp |
| updated_at | Last update timestamp |

---

# 5.2 users

```sql
CREATE TABLE users
(
    id UUID PRIMARY KEY,

    organization_id UUID NOT NULL,

    first_name VARCHAR(100) NOT NULL,

    last_name VARCHAR(100) NOT NULL,

    email VARCHAR(255) NOT NULL,

    password_hash VARCHAR(500) NOT NULL,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    failed_login_count INTEGER NOT NULL DEFAULT 0,

    lockout_end TIMESTAMPTZ NULL,

    last_login_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL,

    updated_at TIMESTAMPTZ NULL,

    CONSTRAINT fk_users_organization
        FOREIGN KEY (organization_id)
        REFERENCES organizations(id)
);
```

### Unique Constraint

Email should be unique globally:

```sql
CREATE UNIQUE INDEX ux_users_email
ON users(email);
```

---

# 5.3 roles

```sql
CREATE TABLE roles
(
    id UUID PRIMARY KEY,

    name VARCHAR(100) NOT NULL,

    description VARCHAR(500) NULL,

    is_system_role BOOLEAN NOT NULL DEFAULT FALSE,

    is_active BOOLEAN NOT NULL DEFAULT TRUE,

    created_at TIMESTAMPTZ NOT NULL,

    updated_at TIMESTAMPTZ NULL
);
```

### System Roles

Seed:

```text
SuperAdmin
OrganizationAdmin
FarmManager
Supervisor
Worker
```

System roles:

```text
is_system_role = true
```

System roles must not be deleted.

They may be extended in future phases.

---

# 5.4 permissions

```sql
CREATE TABLE permissions
(
    id UUID PRIMARY KEY,

    name VARCHAR(150) NOT NULL UNIQUE,

    description VARCHAR(500) NULL,

    module VARCHAR(100) NOT NULL,

    created_at TIMESTAMPTZ NOT NULL
);
```

Example:

```text
Users.View
Users.Create
Users.Update
Users.ManageRoles

Roles.View
Roles.Create
Roles.Update

Permissions.View
Permissions.Manage
```

Future permissions:

```text
Farms.View
Farms.Create
Farms.Update

Activities.View
Activities.Create
Activities.Complete
```

These future permissions may be seeded later when their modules are implemented.

---

# 5.5 user_roles

```sql
CREATE TABLE user_roles
(
    user_id UUID NOT NULL,

    role_id UUID NOT NULL,

    assigned_at TIMESTAMPTZ NOT NULL,

    assigned_by UUID NULL,

    PRIMARY KEY (user_id, role_id),

    CONSTRAINT fk_user_roles_user
        FOREIGN KEY (user_id)
        REFERENCES users(id),

    CONSTRAINT fk_user_roles_role
        FOREIGN KEY (role_id)
        REFERENCES roles(id)
);
```

---

# 5.6 role_permissions

```sql
CREATE TABLE role_permissions
(
    role_id UUID NOT NULL,

    permission_id UUID NOT NULL,

    PRIMARY KEY (role_id, permission_id),

    CONSTRAINT fk_role_permissions_role
        FOREIGN KEY (role_id)
        REFERENCES roles(id),

    CONSTRAINT fk_role_permissions_permission
        FOREIGN KEY (permission_id)
        REFERENCES permissions(id)
);
```

---

# 5.7 refresh_tokens

Raw refresh tokens must never be stored.

```sql
CREATE TABLE refresh_tokens
(
    id UUID PRIMARY KEY,

    user_id UUID NOT NULL,

    token_hash VARCHAR(500) NOT NULL,

    client_type VARCHAR(30) NOT NULL,

    device_name VARCHAR(255) NULL,

    expires_at TIMESTAMPTZ NOT NULL,

    created_at TIMESTAMPTZ NOT NULL,

    revoked_at TIMESTAMPTZ NULL,

    replaced_by_token_id UUID NULL,

    created_by_ip VARCHAR(100) NULL,

    revoked_by_ip VARCHAR(100) NULL,

    CONSTRAINT fk_refresh_tokens_user
        FOREIGN KEY (user_id)
        REFERENCES users(id)
);
```

Client types:

```text
Web
Mobile
```

Phase 1 implements:

```text
Web
```

The database architecture prepares for:

```text
Mobile
```

---

# 5.8 audit_logs

```sql
CREATE TABLE audit_logs
(
    id UUID PRIMARY KEY,

    organization_id UUID NULL,

    user_id UUID NULL,

    action VARCHAR(150) NOT NULL,

    entity_type VARCHAR(150) NULL,

    entity_id UUID NULL,

    details JSONB NULL,

    ip_address VARCHAR(100) NULL,

    created_at TIMESTAMPTZ NOT NULL
);
```

Examples:

```text
User.Login

User.Logout

User.Created

User.Updated

User.Disabled

User.PasswordChanged

User.RoleAssigned

Role.Created

Role.PermissionUpdated
```

---

# 6. Entity Relationships

```text
Organization
│
├── Users
│
└── Future Farms


User
│
├── UserRoles
│
├── RefreshTokens
│
└── AuditLogs


Role
│
├── UserRoles
│
└── RolePermissions


Permission
│
└── RolePermissions
```

---

# 7. Backend Project Structure

Update Phase 0 structure.

```text
FarmManagement.Application/

├── Common/
│
│   ├── Exceptions/
│   ├── Models/
│   └── Constants/
│
├── Interfaces/
│   │
│   ├── Authentication/
│   ├── Persistence/
│   └── Services/
│
├── Features/
│
│   ├── Authentication/
│   │
│   ├── Users/
│   │
│   ├── Roles/
│   │
│   └── Permissions/
│
└── Validators/
```

---

# 8. Domain Structure

```text
FarmManagement.Domain/

├── Common/
│
├── Entities/
│   │
│   ├── Organization.cs
│   ├── User.cs
│   ├── Role.cs
│   ├── Permission.cs
│   ├── RefreshToken.cs
│   └── AuditLog.cs
│
└── Enums/
    │
    └── ClientType.cs
```

---

# 9. Infrastructure Structure

```text
FarmManagement.Infrastructure/

├── Authentication/
│
│   ├── JwtTokenService.cs
│   ├── PasswordService.cs
│   └── RefreshTokenService.cs
│
├── Persistence/
│   │
│   ├── ApplicationDbContext.cs
│   │
│   ├── Configurations/
│   │   ├── OrganizationConfiguration.cs
│   │   ├── UserConfiguration.cs
│   │   ├── RoleConfiguration.cs
│   │   ├── PermissionConfiguration.cs
│   │   ├── RefreshTokenConfiguration.cs
│   │   └── AuditLogConfiguration.cs
│   │
│   ├── Migrations/
│   │
│   └── Seed/
│       └── IdentityDataSeeder.cs
│
├── Repositories/
│
└── Services/
    │
    └── AuditService.cs
```

---

# 10. API Structure

```text
FarmManagement.API/

├── Controllers/
│
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── RolesController.cs
│   └── PermissionsController.cs
│
├── Middleware/
│
│   ├── GlobalExceptionMiddleware.cs
│   └── RequestLoggingMiddleware.cs
│
├── Extensions/
│
│   ├── AuthenticationExtensions.cs
│   ├── AuthorizationExtensions.cs
│   └── ServiceCollectionExtensions.cs
│
└── Configuration/
    │
    ├── JwtOptions.cs
    └── CookieOptions.cs
```

---

# 11. Authentication Flow

## 11.1 Login

```text
Angular Login Page
        │
        ▼

POST /api/auth/login
        │
        ▼

Validate Email
        │
        ▼

Validate Password
        │
        ▼

Check User Active
        │
        ▼

Check Lockout
        │
        ▼

Generate Access Token
        │
        ▼

Generate Refresh Token
        │
        ├───────────────┐
        ▼               ▼

JSON Response      HttpOnly Cookie
Access Token       Refresh Token
```

---

# 12. Access Token

JWT contains:

```text
sub = User ID

email

organization_id

roles

permissions

jti
```

Recommended token lifetime:

```text
15 minutes
```

JWT signing algorithm:

```text
HMAC SHA-256
```

Phase 1 should support future migration to asymmetric signing.

---

# 13. Refresh Token

Recommended lifetime:

```text
7 days
```

Refresh token must be:

```text
Cryptographically random
```

The raw token:

```text
NEVER stored in database
```

Store:

```text
Hash(raw refresh token)
```

---

# 14. Web Refresh Token Cookie

Cookie settings:

```text
HttpOnly = true

Secure = true in HTTPS environments

SameSite = Strict or Lax depending on deployment

Path = /api/auth
```

Cookie name:

```text
farm_refresh_token
```

The exact cookie policy must be configurable by environment.

---

# 15. Refresh Flow

```text
Access Token Expires
       │
       ▼
Angular receives 401
       │
       ▼
Angular Auth Interceptor
       │
       ▼
POST /api/auth/refresh
       │
       ▼
Browser sends HttpOnly Cookie
       │
       ▼
API validates Refresh Token
       │
       ▼
Old Refresh Token revoked
       │
       ▼
New Refresh Token created
       │
       ▼
New Cookie issued
       │
       ▼
New Access Token returned
       │
       ▼
Original request retried
```

This is refresh token rotation.

---

# 16. Logout Flow

```text
Angular Logout
      │
      ▼

POST /api/auth/logout
      │
      ▼

API finds refresh token
      │
      ▼

Refresh token revoked
      │
      ▼

Cookie deleted
      │
      ▼

Angular clears access token
      │
      ▼

Redirect Login
```

---

# 17. Change Password

Endpoint:

```text
POST /api/auth/change-password
```

Request:

```json
{
  "currentPassword": "CurrentPassword",
  "newPassword": "NewPassword"
}
```

Process:

```text
Validate Current Password

↓

Validate New Password Policy

↓

Hash New Password

↓

Update Password

↓

Revoke Existing Refresh Tokens

↓

Audit Log

↓

Require Re-login
```

---

# 18. Password Security

Use:

```text
ASP.NET Core PasswordHasher<User>
```

Do not implement custom password hashing.

Password policy:

```text
Minimum 12 characters

At least one uppercase letter

At least one lowercase letter

At least one number

At least one special character
```

Reject:

```text
Password equal to email

Password equal to username

Very common passwords
```

Do not log passwords.

Do not return password hashes through APIs.

---

# 19. Account Lockout

Failed login:

```text
Failed Attempt 1
Failed Attempt 2
Failed Attempt 3
Failed Attempt 4
Failed Attempt 5
      │
      ▼
Account Locked
```

Recommended:

```text
Maximum Attempts = 5

Lockout Duration = 15 minutes
```

After successful login:

```text
failed_login_count = 0
```

Organization Admin must be able to:

```text
Unlock User
```

---

# 20. Authorization Architecture

Authorization must use ASP.NET Core policy-based authorization.

Example permission:

```text
Users.Create
```

Create policy:

```text
Permission:Users.Create
```

Controller:

```text
[Authorize(Policy = "Permission:Users.Create")]
```

The permission evaluation must verify user claims or permissions.

Do not write:

```text
if (role == "Admin")
```

throughout business code.

---

# 21. Permission Groups

## Administration

```text
Users.View
Users.Create
Users.Update
Users.Activate
Users.Deactivate
Users.Unlock
Users.ManageRoles
```

## Roles

```text
Roles.View
Roles.Create
Roles.Update
Roles.Activate
Roles.Deactivate
Roles.ManagePermissions
```

## Permissions

```text
Permissions.View
```

Permission creation should normally be system controlled.

Permissions are seeded by application modules.

Do not allow Organization Admins to create arbitrary system permissions.

---

# 22. Initial Role Permissions

## SuperAdmin

```text
All permissions
```

SuperAdmin can manage:

```text
Organizations
Users
Roles
Permissions
```

---

## OrganizationAdmin

```text
Users.View
Users.Create
Users.Update
Users.Activate
Users.Deactivate
Users.Unlock
Users.ManageRoles

Roles.View

Permissions.View
```

OrganizationAdmin cannot access users from another organization.

---

## FarmManager

Initially:

```text
No administration permissions
```

Future:

```text
Farms.*
Fields.*
Crops.*
Activities.*
```

---

## Supervisor

Future:

```text
Activities.View
Activities.Update
Activities.Complete
```

---

## Worker

Future:

```text
Activities.ViewAssigned
Activities.UpdateAssigned
```

---

# 23. API Contracts

## 23.1 Login

```text
POST /api/auth/login
```

Request:

```json
{
  "email": "admin@example.com",
  "password": "Password123!"
}
```

Response:

```json
{
  "accessToken": "JWT_TOKEN",
  "expiresIn": 900,
  "user": {
    "id": "UUID",
    "firstName": "Admin",
    "lastName": "User",
    "email": "admin@example.com",
    "organizationId": "UUID",
    "roles": [
      "OrganizationAdmin"
    ],
    "permissions": [
      "Users.View",
      "Users.Create"
    ]
  }
}
```

Refresh token is not returned in JSON for web clients.

---

# 23.2 Refresh

```text
POST /api/auth/refresh
```

Request:

```text
No request body required.
```

Response:

```json
{
  "accessToken": "NEW_JWT_TOKEN",
  "expiresIn": 900
}
```

New refresh token is returned through HttpOnly cookie.

---

# 23.3 Logout

```text
POST /api/auth/logout
```

Response:

```http
204 No Content
```

---

# 23.4 Current User

```text
GET /api/auth/me
```

Response:

```json
{
  "id": "UUID",
  "firstName": "Admin",
  "lastName": "User",
  "email": "admin@example.com",
  "organizationId": "UUID",
  "roles": [],
  "permissions": []
}
```

---

# 23.5 Change Password

```text
POST /api/auth/change-password
```

Request:

```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!"
}
```

Response:

```http
204 No Content
```

---

# 24. User Management API

## List Users

```text
GET /api/users
```

Support:

```text
?page=1

&pageSize=20

&search=

&isActive=
```

---

## Get User

```text
GET /api/users/{id}
```

---

## Create User

```text
POST /api/users
```

Request:

```json
{
  "firstName": "John",
  "lastName": "Farmer",
  "email": "john@example.com",
  "password": "TemporaryPassword123!",
  "roleIds": [
    "UUID"
  ]
}
```

Organization ID behavior:

```text
OrganizationAdmin

→ Automatically use their organization.


SuperAdmin

→ Organization selection allowed.
```

---

## Update User

```text
PUT /api/users/{id}
```

Request:

```json
{
  "firstName": "John",
  "lastName": "Farmer"
}
```

---

## Activate User

```text
POST /api/users/{id}/activate
```

---

## Deactivate User

```text
POST /api/users/{id}/deactivate
```

Deactivation must:

```text
Prevent login

Revoke active refresh tokens
```

---

## Unlock User

```text
POST /api/users/{id}/unlock
```

---

## Assign Roles

```text
PUT /api/users/{id}/roles
```

Request:

```json
{
  "roleIds": [
    "UUID"
  ]
}
```

---

# 25. Role Management API

## List Roles

```text
GET /api/roles
```

## Get Role

```text
GET /api/roles/{id}
```

## Create Role

```text
POST /api/roles
```

## Update Role

```text
PUT /api/roles/{id}
```

## Activate Role

```text
POST /api/roles/{id}/activate
```

## Deactivate Role

```text
POST /api/roles/{id}/deactivate
```

## Update Role Permissions

```text
PUT /api/roles/{id}/permissions
```

Request:

```json
{
  "permissionIds": [
    "UUID"
  ]
}
```

System roles must not be deleted.

---

# 26. Permission API

## List Permissions

```text
GET /api/permissions
```

Permissions are read-only for normal administrators.

Permissions are application-defined.

---

# 27. Standard API Error Response

All API errors must follow:

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Validation failed",
  "traceId": "TRACE_ID",
  "errors": {
    "email": [
      "Email is required"
    ]
  }
}
```

For unexpected errors:

```json
{
  "success": false,
  "statusCode": 500,
  "message": "An unexpected error occurred",
  "traceId": "TRACE_ID"
}
```

Never return:

```text
Stack traces

Connection strings

Passwords

Internal exception details
```

---

# 28. Global Exception Middleware

Create:

```text
GlobalExceptionMiddleware
```

Responsibilities:

```text
Catch unhandled exceptions

↓

Log exception

↓

Generate Trace ID

↓

Map exception to HTTP status

↓

Return standard error response
```

Exception mapping:

| Exception | HTTP |
|---|---:|
| ValidationException | 400 |
| UnauthorizedAccessException | 401 |
| ForbiddenException | 403 |
| NotFoundException | 404 |
| ConflictException | 409 |
| Unexpected Exception | 500 |

Middleware must be registered early in the pipeline.

---

# 29. Request Logging Middleware

Create:

```text
RequestLoggingMiddleware
```

Log:

```text
HTTP Method

Request Path

Status Code

Duration

Trace ID
```

Do not log:

```text
Passwords

Authorization headers

Cookies

Refresh tokens
```

---

# 30. ASP.NET Core Middleware Pipeline

Recommended order:

```text
Global Exception Handling
        ↓
HTTPS Redirection
        ↓
Request Logging
        ↓
CORS
        ↓
Authentication
        ↓
Authorization
        ↓
Controllers
```

---

# 31. Angular Architecture

Update the Angular application.

```text
src/app/

├── core/
│
│   ├── auth/
│   │   ├── auth.service.ts
│   │   ├── auth.store.ts
│   │   ├── auth.models.ts
│   │   └── permission.service.ts
│   │
│   ├── guards/
│   │   ├── auth.guard.ts
│   │   └── permission.guard.ts
│   │
│   ├── interceptors/
│   │   ├── auth.interceptor.ts
│   │   └── error.interceptor.ts
│   │
│   └── services/
│
├── features/
│
│   ├── auth/
│   │   └── login/
│   │
│   └── administration/
│       │
│       ├── users/
│       ├── roles/
│       └── permissions/
│
├── layouts/
│
│   ├── main-layout/
│   └── auth-layout/
│
└── pages/
    │
    └── forbidden/
```

---

# 32. Angular Authentication Service

Responsibilities:

```text
Login

Refresh Access Token

Logout

Load Current User

Change Password

Authentication State
```

Authentication state should contain:

```typescript
interface AuthState {
  accessToken: string | null;
  user: CurrentUser | null;
  isAuthenticated: boolean;
}
```

The refresh token must never be stored in:

```text
LocalStorage

SessionStorage

Angular State
```

---

# 33. Angular Login Flow

```text
Login Component
      │
      ▼
AuthService.login()
      │
      ▼
POST /api/auth/login
      │
      ▼
Access Token returned
      │
      ▼
Store Access Token in Memory
      │
      ▼
User Information stored in Auth State
      │
      ▼
Navigate Dashboard
```

The browser automatically stores the refresh token cookie.

---

# 34. Angular Auth Interceptor

Responsibilities:

```text
Check if access token exists

↓

Add Authorization Header
```

Example:

```http
Authorization: Bearer ACCESS_TOKEN
```

Do not add the Authorization header to:

```text
/api/auth/login

/api/auth/refresh
```

---

# 35. Angular Error Interceptor

The interceptor handles HTTP errors.

## 401

```text
401 Unauthorized

↓

Attempt token refresh

↓

Retry original request

↓

If refresh fails

↓

Logout

↓

Redirect login
```

Only one refresh request should run at a time.

Other failed requests must wait for the refresh operation.

---

## 403

```text
Redirect /forbidden
```

---

## 400

Display validation errors.

---

## 500

Display:

```text
Something went wrong. Please try again.
```

Do not display raw backend exception messages to normal users.

---

# 36. Angular Auth Guard

Protect routes:

```text
/dashboard

/farms

/crops

/activities

/administration
```

Flow:

```text
User visits route

↓

Authenticated?

YES → Allow

NO → Redirect Login
```

---

# 37. Angular Permission Guard

Example route:

```text
/administration/users
```

Required permission:

```text
Users.View
```

Flow:

```text
User authenticated

↓

Check permission

↓

Allowed?

YES → Allow route

NO → Forbidden Page
```

Route configuration:

```text
data: {
  permission: 'Users.View'
}
```

---

# 38. Administration UI

## User Management

Pages:

```text
Administration
│
└── Users
    │
    ├── User List
    ├── Create User
    └── Edit User
```

Features:

```text
Search

Pagination

Active / Inactive Status

Create User

Edit User

Activate

Deactivate

Unlock

Assign Roles
```

---

## Role Management

```text
Administration
│
└── Roles
    │
    ├── Role List
    ├── Create Role
    ├── Edit Role
    └── Assign Permissions
```

---

## Permission Management

Initially:

```text
Read Only
```

Display permissions grouped by module.

Example:

```text
Users

Users.View
Users.Create
Users.Update


Roles

Roles.View
Roles.Create
Roles.Update
```

---

# 39. Audit Logging

Audit logs are created for:

```text
User.Login

User.Logout

User.Created

User.Updated

User.Activated

User.Deactivated

User.Unlocked

User.PasswordChanged

User.RolesUpdated

Role.Created

Role.Updated

Role.PermissionsUpdated
```

Audit logs must contain:

```text
User

Organization

Action

Entity

Timestamp

IP Address

Details
```

Details should use JSONB.

Do not store:

```text
Passwords

JWT Tokens

Refresh Tokens
```

---

# 40. Docker Configuration

Update `.env`.

```env
POSTGRES_DB=farm_management
POSTGRES_USER=farm_user
POSTGRES_PASSWORD=change_this_password

API_PORT=8080
WEB_PORT=4200

JWT__ISSUER=FarmManagement
JWT__AUDIENCE=FarmManagement.Web
JWT__SECRET=REPLACE_WITH_LONG_RANDOM_SECRET

JWT__ACCESS_TOKEN_MINUTES=15
JWT__REFRESH_TOKEN_DAYS=7
```

Production secrets must use a secure secret manager later.

Do not commit:

```text
JWT secrets

Production passwords
```

---

# 41. Docker Compose Environment

API service:

```yaml
environment:

  ASPNETCORE_ENVIRONMENT: Development

  ASPNETCORE_URLS: http://+:8080

  ConnectionStrings__DefaultConnection: >
    Host=postgres;
    Port=5432;
    Database=${POSTGRES_DB};
    Username=${POSTGRES_USER};
    Password=${POSTGRES_PASSWORD}

  Jwt__Issuer: ${JWT__ISSUER}

  Jwt__Audience: ${JWT__AUDIENCE}

  Jwt__Secret: ${JWT__SECRET}

  Jwt__AccessTokenMinutes: ${JWT__ACCESS_TOKEN_MINUTES}

  Jwt__RefreshTokenDays: ${JWT__REFRESH_TOKEN_DAYS}
```

---

# 42. Development Cookie Configuration

For local HTTP development:

```text
Secure = false
```

For HTTPS:

```text
Secure = true
```

Production:

```text
Secure = true
HttpOnly = true
```

Cookie configuration must depend on environment.

---

# 43. CORS Configuration

Development Angular:

```text
http://localhost:4200
```

CORS must allow credentials:

```text
AllowCredentials()
```

Do not use:

```text
AllowAnyOrigin()
```

together with credentials.

The configuration should explicitly allow the Angular origin.

---

# 44. Required Backend Packages

Add only if not already installed.

```text
Microsoft.AspNetCore.Authentication.JwtBearer
```

Optional supporting packages:

```text
Microsoft.Extensions.Options.ConfigurationExtensions
```

Use existing EF Core packages from Phase 0.

Avoid unnecessary authentication frameworks.

---

# 45. Database Migration

Create migration:

```text
Phase1IdentityAndAdministration
```

Migration must create:

```text
organizations

users

roles

permissions

user_roles

role_permissions

refresh_tokens

audit_logs
```

Also configure indexes for:

```text
users.email

organizations.code

permissions.name

refresh_tokens.user_id

refresh_tokens.expires_at

audit_logs.organization_id

audit_logs.user_id

audit_logs.created_at
```

---

# 46. Initial Data Seeding

Phase 1 must seed:

## Organization

```text
System Organization
```

Or a development organization.

Example:

```text
Demo Farm Organization
```

---

## Roles

```text
SuperAdmin

OrganizationAdmin

FarmManager

Supervisor

Worker
```

---

## Permissions

Seed all Phase 1 administration permissions.

---

## Initial SuperAdmin

The implementation must support creating the first SuperAdmin from environment variables.

Example:

```env
INITIAL_ADMIN_EMAIL=admin@example.com
INITIAL_ADMIN_PASSWORD=ChangeThisImmediately123!
```

The password must be hashed before storage.

The seeder must:

```text
Check if initial admin exists

↓

If exists → Do nothing

If not exists → Create
```

Do not recreate the user every application startup.

---

# 47. Initial Admin Security

After initial setup:

```text
Change password

↓

Remove INITIAL_ADMIN_PASSWORD from deployment configuration
```

Production deployment should not permanently contain the initial password.

---

# 48. AI Agent Implementation Strategy

Phase 1 must be implemented in small tasks.

Do not ask an AI agent to implement the entire phase in one prompt.

---

# Task 1.1 – Database and Domain Entities

Implement:

```text
Organization

User

Role

Permission

RefreshToken

AuditLog
```

Implement EF Core configurations.

Create migration.

Do not implement API endpoints yet.

### AI Agent Prompt

```text
Implement Phase 1 Task 1.1 according to PHASE-1-SPEC.md.

Implement only:

1. Domain entities.
2. Enums.
3. EF Core configurations.
4. DbContext updates.
5. Migration.
6. Required indexes and constraints.

Do not implement authentication or controllers.

Do not implement tests.

Verify the solution builds.

Provide:

Changed files
Migration created
Build result
```

---

# Task 1.2 – Initial Data Seeder

Implement:

```text
Organization Seeder

Role Seeder

Permission Seeder

Initial SuperAdmin Seeder
```

Use environment configuration.

### AI Agent Prompt

```text
Implement Phase 1 Task 1.2 according to PHASE-1-SPEC.md.

Implement only:

1. Identity data seeding.
2. System roles.
3. Phase 1 permissions.
4. Development organization.
5. Initial SuperAdmin configuration.

Ensure:

- Password is hashed.
- Seeder is idempotent.
- Initial admin is not duplicated.
- Secrets are not logged.

Do not implement login yet.

Verify solution builds.
```

---

# Task 1.3 – JWT Authentication Services

Implement:

```text
JwtTokenService

PasswordService

RefreshTokenService
```

Implement:

```text
Access Token Generation

Refresh Token Generation

Refresh Token Hashing

Refresh Token Validation

Refresh Token Rotation

Token Revocation
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.3 according to PHASE-1-SPEC.md.

Implement authentication infrastructure only.

Requirements:

- JWT access token lifetime: 15 minutes.
- Refresh token lifetime: 7 days.
- Refresh token must be cryptographically random.
- Store only refresh token hashes.
- Support Web and Mobile client types.
- Implement refresh token rotation.
- Implement token revocation.

Do not implement controllers yet.

Verify build.
```

---

# Task 1.4 – Authentication API

Implement:

```text
POST /api/auth/login

POST /api/auth/refresh

POST /api/auth/logout

GET /api/auth/me

POST /api/auth/change-password
```

Implement:

```text
Account lockout

Password validation

Cookie refresh tokens

Audit logging
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.4 according to PHASE-1-SPEC.md.

Implement the authentication API.

Requirements:

1. Login.
2. Refresh.
3. Logout.
4. Current user.
5. Change password.
6. Account lockout.
7. HttpOnly refresh token cookie.
8. Refresh token rotation.
9. Refresh token revocation.
10. Audit logs.

Use the API contracts defined in PHASE-1-SPEC.md.

Do not implement user administration endpoints yet.

Verify build and manually validate API behavior.
```

---

# Task 1.5 – Authorization

Implement:

```text
Permission-based policies

Permission authorization handler

Claims generation
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.5 according to PHASE-1-SPEC.md.

Implement:

- Permission-based authorization.
- Dynamic or registered authorization policies.
- Permission authorization handler.
- Required claims generation.

Do not use hardcoded role checks throughout controllers.

Verify build.
```

---

# Task 1.6 – User Administration API

Implement:

```text
GET /api/users

GET /api/users/{id}

POST /api/users

PUT /api/users/{id}

POST /api/users/{id}/activate

POST /api/users/{id}/deactivate

POST /api/users/{id}/unlock

PUT /api/users/{id}/roles
```

Critical requirement:

```text
OrganizationAdmin

↓

Can only manage users inside their organization.
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.6 according to PHASE-1-SPEC.md.

Implement User Administration APIs.

Enforce:

- Organization data isolation.
- SuperAdmin can manage all organizations.
- OrganizationAdmin can manage only their organization.
- User deactivation revokes refresh tokens.
- No hard delete.
- Audit all changes.

Use permission-based authorization.

Verify build.
```

---

# Task 1.7 – Role and Permission APIs

Implement:

```text
GET /api/roles

GET /api/roles/{id}

POST /api/roles

PUT /api/roles/{id}

POST /api/roles/{id}/activate

POST /api/roles/{id}/deactivate

PUT /api/roles/{id}/permissions

GET /api/permissions
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.7 according to PHASE-1-SPEC.md.

Implement Role and Permission APIs.

Requirements:

- Permissions are read-only.
- Roles support create and update.
- System roles cannot be deleted.
- Roles use active/inactive state.
- Permission assignments are audited.
- Use permission authorization.

Verify build.
```

---

# Task 1.8 – Error and Logging Middleware

Implement:

```text
GlobalExceptionMiddleware

RequestLoggingMiddleware
```

Ensure all errors follow the standard API format.

### AI Agent Prompt

```text
Implement Phase 1 Task 1.8 according to PHASE-1-SPEC.md.

Implement:

1. Global exception middleware.
2. Standard API error response.
3. Request logging middleware.
4. Trace ID support.

Never expose:

- Stack traces
- Passwords
- JWT tokens
- Cookies
- Refresh tokens
- Connection strings

Verify build.
```

---

# Task 1.9 – Angular Authentication

Implement:

```text
Login Page

AuthService

Auth State

Auth Guard

Auth Interceptor

Error Interceptor

Forbidden Page
```

### AI Agent Prompt

```text
Implement Phase 1 Task 1.9 according to PHASE-1-SPEC.md.

Implement Angular authentication.

Requirements:

- Access token stored only in memory.
- Refresh token never stored in LocalStorage.
- Login.
- Logout.
- Refresh token flow.
- Auth guard.
- Authorization header interceptor.
- 401 refresh and retry logic.
- 403 forbidden page.
- Single refresh request at a time.

Do not implement user administration pages yet.

Verify Angular build.
```

---

# Task 1.10 – Angular Administration UI

Implement:

```text
User List

Create User

Edit User

Role Assignment

Activate

Deactivate

Unlock

Role List

Create Role

Edit Role

Permission Assignment

Permission List
```

Use Angular Material.

### AI Agent Prompt

```text
Implement Phase 1 Task 1.10 according to PHASE-1-SPEC.md.

Implement Angular Administration UI.

Use:

- Angular Material
- Reactive Forms
- Strong TypeScript models
- Pagination
- Search
- Validation
- Permission-based navigation

Do not use LocalStorage for authentication tokens.

Verify Angular build.
```

---

# Task 1.11 – Docker and Configuration Review

Update:

```text
.env.example

docker-compose.yml

API environment configuration

JWT configuration
```

Verify:

```text
PostgreSQL

API

Angular
```

still start.

### AI Agent Prompt

```text
Implement Phase 1 Task 1.11 according to PHASE-1-SPEC.md.

Review and update Docker and configuration.

Verify:

1. PostgreSQL starts.
2. API starts.
3. Migration works.
4. Seeder works.
5. Angular starts.
6. JWT configuration loads from environment.
7. Secrets are not committed.
8. Cookie configuration works for local development.

Do not introduce unrelated infrastructure.

Provide the final environment variable documentation.
```

---

# 49. Phase 1 Definition of Done

Phase 1 is complete when:

```text
AUTHENTICATION

✓ User can log in.

✓ JWT access token is generated.

✓ Refresh token uses HttpOnly cookie.

✓ Access token refresh works.

✓ Refresh token rotation works.

✓ Logout revokes token.

✓ Change password works.

✓ Password change revokes existing sessions.

✓ Account lockout works.


AUTHORIZATION

✓ Users have roles.

✓ Roles have permissions.

✓ Permission authorization works.

✓ Organization isolation works.


ADMINISTRATION

✓ Users can be created.

✓ Users can be updated.

✓ Users can be activated.

✓ Users can be deactivated.

✓ Users can be unlocked.

✓ Roles can be assigned.

✓ Roles can be managed.

✓ Permissions can be viewed.


ANGULAR

✓ Login page works.

✓ Auth state works.

✓ Auth guard works.

✓ Permission guard works.

✓ Auth interceptor works.

✓ Token refresh works.

✓ 403 redirects to forbidden page.

✓ Logout works.


SECURITY

✓ Password hashes are secure.

✓ Refresh tokens are hashed.

✓ JWT secret comes from environment.

✓ Refresh token not accessible from JavaScript.

✓ Sensitive data is not logged.

✓ Users are never hard deleted.


AUDIT

✓ Login audited.

✓ Logout audited.

✓ User changes audited.

✓ Role changes audited.


INFRASTRUCTURE

✓ Docker works.

✓ PostgreSQL works.

✓ Migration works.

✓ Seeder works.

✓ Angular works.

✓ API works.
```

---

# 50. Phase 1 Completion Architecture

```text
                    ANGULAR WEB

                         │

                Login / Administration

                         │

                         ▼

                  AUTH SERVICE

                         │

              Access Token in Memory

                         │

                         ▼

              HTTP AUTH INTERCEPTOR

                         │

                         ▼

                  ASP.NET API

                         │

       ┌─────────────────┼─────────────────┐
       │                 │                 │

       ▼                 ▼                 ▼

Authentication     Authorization      Administration

       │                 │                 │

       └─────────────────┼─────────────────┘

                         │

                         ▼

                  Infrastructure

                         │

                         ▼

                   PostgreSQL


                HttpOnly Cookie

Angular Browser ───────────────────► API

                  Refresh Token
```

---

# 51. Next Phase

After Phase 1 is stable:

```text
Phase 2

Organization Administration
        ↓
Farm Management
        ↓
Field / Plot Management
```

Phase 2 will introduce the first real farm business entities while preserving the multi-organization security model established in Phase 1.

---

# END OF PHASE-1-SPEC.md