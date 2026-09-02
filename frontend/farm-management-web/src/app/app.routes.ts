import { Routes } from '@angular/router';

import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { ForbiddenPageComponent } from './pages/forbidden/forbidden-page.component';
import { permissionGuard } from './core/guards/permission.guard';
import { AdministrationPageComponent } from './features/administration/administration-page.component';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in',
    loadComponent: () => import('./features/auth/login/login-page.component').then((module) => module.LoginPageComponent)
  },
  {
    path: 'forbidden',
    title: 'Forbidden',
    component: ForbiddenPageComponent
  },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'administration',
        title: 'Administration',
        component: AdministrationPageComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['Users.View', 'Roles.View', 'Permissions.View'] },
        children: [
          {
            path: '',
            loadComponent: () => import('./features/administration/admin-home-page.component').then((module) => module.AdminHomePageComponent)
          },
          {
            path: 'users',
            title: 'Users',
            canActivate: [permissionGuard],
            data: { permission: 'Users.View' },
            loadComponent: () => import('./features/administration/users/users-page.component').then((module) => module.UsersPageComponent)
          },
          {
            path: 'users/new',
            title: 'Create user',
            canActivate: [permissionGuard],
            data: { permission: 'Users.Create' },
            loadComponent: () => import('./features/administration/users/user-editor-page.component').then((module) => module.UserEditorPageComponent)
          },
          {
            path: 'users/:id/edit',
            title: 'Edit user',
            canActivate: [permissionGuard],
            data: { permission: 'Users.Update' },
            loadComponent: () => import('./features/administration/users/user-editor-page.component').then((module) => module.UserEditorPageComponent)
          },
          {
            path: 'roles',
            title: 'Roles',
            canActivate: [permissionGuard],
            data: { permission: 'Roles.View' },
            loadComponent: () => import('./features/administration/roles/roles-page.component').then((module) => module.RolesPageComponent)
          },
          {
            path: 'roles/new',
            title: 'Create role',
            canActivate: [permissionGuard],
            data: { permission: 'Roles.Create' },
            loadComponent: () => import('./features/administration/roles/role-editor-page.component').then((module) => module.RoleEditorPageComponent)
          },
          {
            path: 'roles/:id/edit',
            title: 'Edit role',
            canActivate: [permissionGuard],
            data: { permission: 'Roles.Update' },
            loadComponent: () => import('./features/administration/roles/role-editor-page.component').then((module) => module.RoleEditorPageComponent)
          },
          {
            path: 'permissions',
            title: 'Permissions',
            canActivate: [permissionGuard],
            data: { permission: 'Permissions.View' },
            loadComponent: () => import('./features/administration/permissions/permissions-page.component').then((module) => module.PermissionsPageComponent)
          }
        ]
      },
      {
        path: 'dashboard',
        title: 'Dashboard',
        loadComponent: () => import('./features/dashboard/dashboard-page.component').then((module) => module.DashboardPageComponent)
      },
      {
        path: 'farms',
        title: 'Farms',
        loadComponent: () => import('./features/farms/farms-page.component').then((module) => module.FarmsPageComponent)
      },
      {
        path: 'crops',
        title: 'Crops',
        loadComponent: () => import('./features/crops/crops-page.component').then((module) => module.CropsPageComponent)
      },
      {
        path: 'activities',
        title: 'Activities',
        loadComponent: () => import('./features/activities/activities-page.component').then((module) => module.ActivitiesPageComponent)
      },
      {
        path: 'settings',
        title: 'Settings',
        loadComponent: () => import('./features/settings/settings-page.component').then((module) => module.SettingsPageComponent),
        children: [
          {
            path: 'change-password',
            title: 'Change password',
            loadComponent: () => import('./features/settings/change-password/change-password-page.component').then((module) => module.ChangePasswordPageComponent)
          }
        ]
      },
      {
        path: '**',
        redirectTo: 'dashboard'
      }
    ]
  }
];
