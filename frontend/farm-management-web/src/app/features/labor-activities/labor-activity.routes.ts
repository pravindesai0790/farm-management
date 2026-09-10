import { Routes } from "@angular/router";
import { permissionGuard } from "../../core/guards/permission.guard";

export const LABOR_ACTIVITY_ROUTES: Routes = [
  {
    path: "",
    title: "Labor activities",
    canActivate: [permissionGuard],
    data: { permission: "LaborActivity.View" },
    loadComponent: () =>
      import(
        "./pages/labor-activity-list/labor-activity-list-page.component"
      ).then((m) => m.LaborActivityListPageComponent),
  },
];
