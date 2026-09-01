import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, finalize, map, shareReplay, switchMap, tap } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import {
  AuthState,
  CurrentUser,
  LoginRequest,
  LoginResponse,
  RefreshResponse
} from './auth.models';
import { AuthStore } from './auth.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(AuthStore);
  private readonly authEndpoint = `${environment.apiUrl}/auth`;
  private refreshRequest$: Observable<RefreshResponse> | null = null;
  private logoutRequest$: Observable<void> | null = null;

  readonly state = this.store.state;
  readonly user = this.store.user;
  readonly isAuthenticated = this.store.isAuthenticated;

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.authEndpoint}/login`,
      credentials,
      { withCredentials: true }
    ).pipe(
      tap((response) => this.store.setSession(response.accessToken, response.user))
    );
  }

  refreshAccessToken(): Observable<RefreshResponse> {
    if (this.refreshRequest$ === null) {
      this.refreshRequest$ = this.http.post<RefreshResponse>(
        `${this.authEndpoint}/refresh`,
        null,
        { withCredentials: true }
      ).pipe(
        tap((response) => this.store.setAccessToken(response.accessToken)),
        finalize(() => {
          this.refreshRequest$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return this.refreshRequest$;
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.authEndpoint}/me`).pipe(
      tap((user) => this.store.setUser(user))
    );
  }

  ensureAuthenticated(): Observable<boolean> {
    if (this.store.accessToken() !== null && this.store.user() !== null) {
      return of(true);
    }

    const accessToken$: Observable<RefreshResponse | null> = this.store.accessToken() === null
      ? this.refreshAccessToken()
      : of(null);

    return accessToken$.pipe(
      switchMap(() => {
        const user = this.store.user();
        return user === null ? this.loadCurrentUser() : of(user);
      }),
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      })
    );
  }

  logout(): Observable<void> {
    this.clearSession();

    if (this.logoutRequest$ === null) {
      this.logoutRequest$ = this.http.post<void>(
        `${this.authEndpoint}/logout`,
        null,
        { withCredentials: true }
      ).pipe(
        finalize(() => {
          this.logoutRequest$ = null;
        }),
        shareReplay({ bufferSize: 1, refCount: false })
      );
    }

    return this.logoutRequest$;
  }

  clearSession(): void {
    this.store.clear();
  }

  getAccessToken(): string | null {
    return this.store.accessToken();
  }

  get snapshot(): AuthState {
    return this.store.state();
  }
}
