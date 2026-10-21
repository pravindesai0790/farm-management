import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';

export const FERTILIZER_ROUTES: Routes = [
  {
    path: '',
    title: 'Fertilizer Applications',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.View' },
    loadComponent: () =>
      import('./fertilizer-list/fertilizer-list-page.component').then(
        (m) => m.FertilizerListPageComponent,
      ),
  },
  {
    path: 'new',
    title: 'Plan Fertilizer Application',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.Create' },
    loadComponent: () =>
      import('./fertilizer-list/fertilizer-list-page.component').then(
        (m) => m.FertilizerListPageComponent,
      ),
  },
  {
    path: ':id',
    title: 'Fertilizer Application Details',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.View' },
    loadComponent: () =>
      import('./fertilizer-list/fertilizer-list-page.component').then(
        (m) => m.FertilizerListPageComponent,
      ),
  },
];
