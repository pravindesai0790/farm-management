export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface UserRole {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
}

export interface User {
  readonly id: string;
  readonly organizationId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly isActive: boolean;
  readonly failedLoginCount: number;
  readonly lockoutEnd: string | null;
  readonly lastLoginAt: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
  readonly roles: readonly UserRole[];
}

export interface Permission {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly module: string;
  readonly createdAt: string;
}

export interface Role {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly isSystemRole: boolean;
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly updatedAt: string | null;
  readonly permissions: readonly Permission[];
}

export interface CreateUserRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
  readonly organizationId?: string;
  readonly roleIds: readonly string[];
}

export interface UpdateUserRequest {
  readonly firstName: string;
  readonly lastName: string;
}

export interface AssignUserRolesRequest {
  readonly roleIds: readonly string[];
}

export interface CreateRoleRequest {
  readonly name: string;
  readonly description: string | null;
}

export interface UpdateRoleRequest {
  readonly name: string;
  readonly description: string | null;
}

export interface UpdateRolePermissionsRequest {
  readonly permissionIds: readonly string[];
}

