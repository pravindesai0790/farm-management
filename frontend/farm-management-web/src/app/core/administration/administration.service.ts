import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";

import { environment } from "../../../environments/environment";
import {
  AssignUserRolesRequest,
  CreateRoleRequest,
  CreateUserRequest,
  PagedResponse,
  Permission,
  Role,
  UpdateRolePermissionsRequest,
  UpdateRoleRequest,
  UpdateUserRequest,
  User,
} from "./administration.models";

@Injectable({ providedIn: "root" })
export class AdministrationService {
  private readonly http = inject(HttpClient);
  private readonly usersEndpoint = `${environment.apiUrl}/users`;
  private readonly rolesEndpoint = `${environment.apiUrl}/roles`;
  private readonly permissionsEndpoint = `${environment.apiUrl}/permissions`;

  listUsers(
    page: number,
    pageSize: number,
    search: string,
    isActive: boolean | null,
  ): Observable<PagedResponse<User>> {
    let params = new HttpParams().set("page", page).set("pageSize", pageSize);
    if (search.trim().length > 0) {
      params = params.set("search", search.trim());
    }
    if (isActive !== null) {
      params = params.set("isActive", isActive);
    }
    return this.http.get<PagedResponse<User>>(this.usersEndpoint, { params });
  }

  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${this.usersEndpoint}/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>(this.usersEndpoint, request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(`${this.usersEndpoint}/${id}`, request);
  }

  activateUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.usersEndpoint}/${id}/activate`, null);
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.usersEndpoint}/${id}/deactivate`, null);
  }

  unlockUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.usersEndpoint}/${id}/unlock`, null);
  }

  assignUserRoles(
    id: string,
    request: AssignUserRolesRequest,
  ): Observable<User> {
    return this.http.put<User>(`${this.usersEndpoint}/${id}/roles`, request);
  }

  listRoles(isActive: boolean | null = null): Observable<readonly Role[]> {
    let params = new HttpParams();
    if (isActive !== null) {
      params = params.set("isActive", isActive);
    }
    return this.http.get<readonly Role[]>(this.rolesEndpoint, { params });
  }

  getRole(id: string): Observable<Role> {
    return this.http.get<Role>(`${this.rolesEndpoint}/${id}`);
  }

  createRole(request: CreateRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.rolesEndpoint, request);
  }

  updateRole(id: string, request: UpdateRoleRequest): Observable<Role> {
    return this.http.put<Role>(`${this.rolesEndpoint}/${id}`, request);
  }

  activateRole(id: string): Observable<void> {
    return this.http.post<void>(`${this.rolesEndpoint}/${id}/activate`, null);
  }

  deactivateRole(id: string): Observable<void> {
    return this.http.post<void>(`${this.rolesEndpoint}/${id}/deactivate`, null);
  }

  updateRolePermissions(
    id: string,
    request: UpdateRolePermissionsRequest,
  ): Observable<Role> {
    return this.http.put<Role>(
      `${this.rolesEndpoint}/${id}/permissions`,
      request,
    );
  }

  listPermissions(): Observable<readonly Permission[]> {
    return this.http.get<readonly Permission[]>(this.permissionsEndpoint);
  }
}
