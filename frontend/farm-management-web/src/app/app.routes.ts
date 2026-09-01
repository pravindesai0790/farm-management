import { Routes } from '@angular/router';

import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { ForbiddenPageComponent } from './pages/forbidden/forbidden-page.component';

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
        loadComponent: () => import('./features/settings/settings-page.component').then((module) => module.SettingsPageComponent)
      },
      {
        path: '**',
        redirectTo: 'dashboard'
      }
    ]
  }
];
