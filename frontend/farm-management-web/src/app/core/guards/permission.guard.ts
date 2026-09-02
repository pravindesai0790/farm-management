import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { PermissionService } from '../auth/permission.service';

export const permissionGuard: CanActivateFn = (route) => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);
  const requiredPermissions = route.data['permissions'] as readonly string[] | undefined;
  const singlePermission = route.data['permission'] as string | undefined;
  const permissions = requiredPermissions ?? (singlePermission === undefined ? [] : [singlePermission]);

  return permissions.length === 0 || permissionService.hasAny(permissions)
    ? true
    : router.createUrlTree(['/forbidden']);
};

