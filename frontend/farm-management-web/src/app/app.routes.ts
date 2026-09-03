import { Routes } from "@angular/router";

import { MainLayoutComponent } from "./layouts/main-layout/main-layout.component";
import { authGuard } from "./core/guards/auth.guard";
import { ForbiddenPageComponent } from "./pages/forbidden/forbidden-page.component";
import { permissionGuard } from "./core/guards/permission.guard";
import { AdministrationPageComponent } from "./features/administration/administration-page.component";

export const routes: Routes = [
  {
    path: "login",
    title: "Sign in",
    loadComponent: () =>
      import("./features/auth/login/login-page.component").then(
        (module) => module.LoginPageComponent,
      ),
  },
  {
    path: "forbidden",
    title: "Forbidden",
    component: ForbiddenPageComponent,
  },
  {
    path: "",
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: "",
        pathMatch: "full",
        redirectTo: "dashboard",
      },
      {
        path: "administration",
        title: "Administration",
        component: AdministrationPageComponent,
        canActivate: [permissionGuard],
        data: { permissions: ["Users.View", "Roles.View", "Permissions.View"] },
        children: [
          {
            path: "",
            loadComponent: () =>
              import("./features/administration/admin-home-page.component").then(
                (module) => module.AdminHomePageComponent,
              ),
          },
          {
            path: "users",
            title: "Users",
            canActivate: [permissionGuard],
            data: { permission: "Users.View" },
            loadComponent: () =>
              import("./features/administration/users/users-page.component").then(
                (module) => module.UsersPageComponent,
              ),
          },
          {
            path: "users/new",
            title: "Create user",
            canActivate: [permissionGuard],
            data: { permission: "Users.Create" },
            loadComponent: () =>
              import("./features/administration/users/user-editor-page.component").then(
                (module) => module.UserEditorPageComponent,
              ),
          },
          {
            path: "users/:id/edit",
            title: "Edit user",
            canActivate: [permissionGuard],
            data: { permission: "Users.Update" },
            loadComponent: () =>
              import("./features/administration/users/user-editor-page.component").then(
                (module) => module.UserEditorPageComponent,
              ),
          },
          {
            path: "roles",
            title: "Roles",
            canActivate: [permissionGuard],
            data: { permission: "Roles.View" },
            loadComponent: () =>
              import("./features/administration/roles/roles-page.component").then(
                (module) => module.RolesPageComponent,
              ),
          },
          {
            path: "roles/new",
            title: "Create role",
            canActivate: [permissionGuard],
            data: { permission: "Roles.Create" },
            loadComponent: () =>
              import("./features/administration/roles/role-editor-page.component").then(
                (module) => module.RoleEditorPageComponent,
              ),
          },
          {
            path: "roles/:id/edit",
            title: "Edit role",
            canActivate: [permissionGuard],
            data: { permission: "Roles.Update" },
            loadComponent: () =>
              import("./features/administration/roles/role-editor-page.component").then(
                (module) => module.RoleEditorPageComponent,
              ),
          },
          {
            path: "permissions",
            title: "Permissions",
            canActivate: [permissionGuard],
            data: { permission: "Permissions.View" },
            loadComponent: () =>
              import("./features/administration/permissions/permissions-page.component").then(
                (module) => module.PermissionsPageComponent,
              ),
          },
        ],
      },
      {
        path: "dashboard",
        title: "Dashboard",
        loadComponent: () =>
          import("./features/dashboard/dashboard-page.component").then(
            (module) => module.DashboardPageComponent,
          ),
      },
      {
        path: "farms",
        title: "Farms",
        canActivate: [permissionGuard],
        data: { permission: "Farm.View" },
        loadComponent: () =>
          import("./features/farms/farms-page.component").then(
            (module) => module.FarmsPageComponent,
          ),
      },
      {
        path: "farm-areas",
        title: "Farm areas",
        canActivate: [permissionGuard],
        data: { permission: "FarmArea.View" },
        loadComponent: () =>
          import("./features/farm-areas/farm-areas-page.component").then(
            (module) => module.FarmAreasPageComponent,
          ),
      },
      {
        path: "farms/new",
        title: "Create farm",
        canActivate: [permissionGuard],
        data: { permission: "Farm.Create" },
        loadComponent: () =>
          import("./features/farms/farm-editor-page.component").then(
            (module) => module.FarmEditorPageComponent,
          ),
      },
      {
        path: "farms/:id/edit",
        title: "Edit farm",
        canActivate: [permissionGuard],
        data: { permission: "Farm.Update" },
        loadComponent: () =>
          import("./features/farms/farm-editor-page.component").then(
            (module) => module.FarmEditorPageComponent,
          ),
      },
      {
        path: "farms/:id",
        title: "Farm details",
        canActivate: [permissionGuard],
        data: { permission: "Farm.View" },
        loadComponent: () =>
          import("./features/farms/farm-detail-page.component").then(
            (module) => module.FarmDetailPageComponent,
          ),
      },
      {
        path: "farm-areas/new",
        title: "Create farm area",
        canActivate: [permissionGuard],
        data: { permission: "FarmArea.Create" },
        loadComponent: () =>
          import("./features/farm-areas/farm-area-editor-page.component").then(
            (module) => module.FarmAreaEditorPageComponent,
          ),
      },
      {
        path: "farm-areas/:id/edit",
        title: "Edit farm area",
        canActivate: [permissionGuard],
        data: { permission: "FarmArea.Update" },
        loadComponent: () =>
          import("./features/farm-areas/farm-area-editor-page.component").then(
            (module) => module.FarmAreaEditorPageComponent,
          ),
      },
      {
        path: "farm-areas/:id",
        title: "Farm area details",
        canActivate: [permissionGuard],
        data: { permission: "FarmArea.View" },
        loadComponent: () =>
          import("./features/farm-areas/farm-area-detail-page.component").then(
            (module) => module.FarmAreaDetailPageComponent,
          ),
      },
      {
        path: "crops",
        title: "Crops",
        canActivate: [permissionGuard],
        data: { permission: "Crop.View" },
        loadComponent: () =>
          import("./features/crops/crops-page.component").then(
            (module) => module.CropsPageComponent,
          ),
      },
      {
        path: "crops/new",
        title: "Create crop",
        canActivate: [permissionGuard],
        data: { permission: "Crop.Create" },
        loadComponent: () =>
          import("./features/crops/crop-editor-page.component").then(
            (module) => module.CropEditorPageComponent,
          ),
      },
      {
        path: "crops/:id/edit",
        title: "Edit crop",
        canActivate: [permissionGuard],
        data: { permission: "Crop.Update" },
        loadComponent: () =>
          import("./features/crops/crop-editor-page.component").then(
            (module) => module.CropEditorPageComponent,
          ),
      },
      {
        path: "crops/:id",
        title: "Crop details",
        canActivate: [permissionGuard],
        data: { permission: "Crop.View" },
        loadComponent: () =>
          import("./features/crops/crop-detail-page.component").then(
            (module) => module.CropDetailPageComponent,
          ),
      },
      {
        path: "plantations",
        title: "Plantations",
        canActivate: [permissionGuard],
        data: { permission: "Plantation.View" },
        loadComponent: () =>
          import("./features/plantations/plantations-page.component").then(
            (module) => module.PlantationsPageComponent,
          ),
      },
      {
        path: "plantations/new",
        title: "Create plantation",
        canActivate: [permissionGuard],
        data: { permission: "Plantation.Create" },
        loadComponent: () =>
          import("./features/plantations/plantation-editor-page.component").then(
            (module) => module.PlantationEditorPageComponent,
          ),
      },
      {
        path: "plantations/:id/edit",
        title: "Edit plantation",
        canActivate: [permissionGuard],
        data: { permission: "Plantation.Update" },
        loadComponent: () =>
          import("./features/plantations/plantation-editor-page.component").then(
            (module) => module.PlantationEditorPageComponent,
          ),
      },
      {
        path: "plantations/:id",
        title: "Plantation details",
        canActivate: [permissionGuard],
        data: { permission: "Plantation.View" },
        loadComponent: () =>
          import("./features/plantations/plantation-detail-page.component").then(
            (module) => module.PlantationDetailPageComponent,
          ),
      },
      {
        path: "crop-cycles",
        title: "Crop cycles",
        canActivate: [permissionGuard],
        data: { permission: "CropCycle.View" },
        loadComponent: () =>
          import("./features/crop-cycles/crop-cycles-page.component").then(
            (module) => module.CropCyclesPageComponent,
          ),
      },
      {
        path: "crop-cycles/new",
        title: "Create crop cycle",
        canActivate: [permissionGuard],
        data: { permission: "CropCycle.Create" },
        loadComponent: () =>
          import("./features/crop-cycles/crop-cycle-editor-page.component").then(
            (module) => module.CropCycleEditorPageComponent,
          ),
      },
      {
        path: "crop-cycles/:id/edit",
        title: "Edit crop cycle",
        canActivate: [permissionGuard],
        data: { permission: "CropCycle.Update" },
        loadComponent: () =>
          import("./features/crop-cycles/crop-cycle-editor-page.component").then(
            (module) => module.CropCycleEditorPageComponent,
          ),
      },
      {
        path: "crop-cycles/:id",
        title: "Crop cycle details",
        canActivate: [permissionGuard],
        data: { permission: "CropCycle.View" },
        loadComponent: () =>
          import("./features/crop-cycles/crop-cycle-detail-page.component").then(
            (module) => module.CropCycleDetailPageComponent,
          ),
      },
      {
        path: "organization",
        title: "Organization",
        canActivate: [permissionGuard],
        data: { permission: "Organization.View" },
        loadComponent: () =>
          import("./features/organization/organization-page.component").then(
            (module) => module.OrganizationPageComponent,
          ),
      },
      {
        path: "organization/new",
        title: "Create organization",
        canActivate: [permissionGuard],
        data: { permission: "Organization.Create", role: "SuperAdmin" },
        loadComponent: () =>
          import("./features/organization/organization-page.component").then(
            (module) => module.OrganizationPageComponent,
          ),
      },
      {
        path: "activities",
        title: "Activities",
        loadComponent: () =>
          import("./features/activities/activities-page.component").then(
            (module) => module.ActivitiesPageComponent,
          ),
      },
      {
        path: "settings",
        title: "Settings",
        loadComponent: () =>
          import("./features/settings/settings-page.component").then(
            (module) => module.SettingsPageComponent,
          ),
        children: [
          {
            path: "change-password",
            title: "Change password",
            loadComponent: () =>
              import("./features/settings/change-password/change-password-page.component").then(
                (module) => module.ChangePasswordPageComponent,
              ),
          },
        ],
      },
      {
        path: "**",
        redirectTo: "dashboard",
      },
    ],
  },
];
