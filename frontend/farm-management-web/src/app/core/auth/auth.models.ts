export interface CurrentUser {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly organizationId: string;
  readonly organizationName: string;
  readonly roles: readonly string[];
  readonly permissions: readonly string[];
}

export interface AuthState {
  readonly accessToken: string | null;
  readonly user: CurrentUser | null;
  readonly isAuthenticated: boolean;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface LoginResponse {
  readonly accessToken: string;
  readonly expiresIn: number;
  readonly user: CurrentUser;
}

export interface RefreshResponse {
  readonly accessToken: string;
  readonly expiresIn: number;
}

export interface ChangePasswordRequest {
  readonly currentPassword: string;
  readonly newPassword: string;
}
