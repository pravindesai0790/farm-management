import { Injectable, inject } from '@angular/core';

import { AuthStore } from './auth.store';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly authStore = inject(AuthStore);

  has(permission: string): boolean {
    return this.authStore.user()?.permissions.includes(permission) ?? false;
  }

  hasAny(permissions: readonly string[]): boolean {
    return permissions.some((permission) => this.has(permission));
  }
}

