import { Injectable, computed, signal } from '@angular/core';

import { AuthState, CurrentUser } from './auth.models';

const initialState: AuthState = {
  accessToken: null,
  user: null,
  isAuthenticated: false
};

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly stateSignal = signal<AuthState>(initialState);

  readonly state = this.stateSignal.asReadonly();
  readonly accessToken = computed(() => this.stateSignal().accessToken);
  readonly user = computed(() => this.stateSignal().user);
  readonly isAuthenticated = computed(() => this.stateSignal().isAuthenticated);

  setSession(accessToken: string, user: CurrentUser): void {
    this.stateSignal.set({
      accessToken,
      user,
      isAuthenticated: true
    });
  }

  setAccessToken(accessToken: string): void {
    const currentState = this.stateSignal();
    this.stateSignal.set({
      ...currentState,
      accessToken,
      isAuthenticated: true
    });
  }

  setUser(user: CurrentUser): void {
    const currentState = this.stateSignal();
    this.stateSignal.set({
      ...currentState,
      user,
      isAuthenticated: currentState.accessToken !== null
    });
  }

  clear(): void {
    this.stateSignal.set(initialState);
  }
}

