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
      import('./fertilizer-editor/fertilizer-editor-page.component').then(
        (m) => m.FertilizerEditorPageComponent,
      ),
  },
  {
    path: ':id/edit',
    title: 'Edit Fertilizer Application',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.Update' },
    loadComponent: () =>
      import('./fertilizer-editor/fertilizer-editor-page.component').then(
        (m) => m.FertilizerEditorPageComponent,
      ),
  },
  {
    path: 'record-completed',
    title: 'Record Completed Fertilizer Application',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.RecordCompleted' },
    loadComponent: () =>
      import(
        './fertilizer-record-completed/fertilizer-record-completed-page.component'
      ).then((m) => m.FertilizerRecordCompletedPageComponent),
  },
  {
    path: ':id',
    title: 'Fertilizer Application Details',
    canActivate: [permissionGuard],
    data: { permission: 'Fertilizer.View' },
    loadComponent: () =>
      import('./fertilizer-detail/fertilizer-detail-page.component').then(
        (m) => m.FertilizerDetailPageComponent,
      ),
  },
];
