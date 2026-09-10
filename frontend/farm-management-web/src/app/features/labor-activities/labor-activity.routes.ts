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
  {
    path: "new",
    title: "Record labor activity",
    canActivate: [permissionGuard],
    data: { permission: "LaborActivity.Create" },
    loadComponent: () =>
      import(
        "./pages/labor-activity-editor/labor-activity-editor-page.component"
      ).then((m) => m.LaborActivityEditorPageComponent),
  },
  {
    path: ":id/edit",
    title: "Edit labor activity",
    canActivate: [permissionGuard],
    data: { permission: "LaborActivity.Update" },
    loadComponent: () =>
      import(
        "./pages/labor-activity-editor/labor-activity-editor-page.component"
      ).then((m) => m.LaborActivityEditorPageComponent),
  },
  {
    path: ":id",
    title: "Labor activity details",
    canActivate: [permissionGuard],
    data: { permission: "LaborActivity.View" },
    loadComponent: () =>
      import(
        "./pages/labor-activity-detail/labor-activity-detail-page.component"
      ).then((m) => m.LaborActivityDetailPageComponent),
  },
];
