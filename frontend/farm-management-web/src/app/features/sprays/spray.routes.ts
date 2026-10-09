import { Routes } from "@angular/router";
import { permissionGuard } from "../../core/guards/permission.guard";

export const SPRAY_ROUTES: Routes = [
  {
    path: "",
    title: "Spray Applications",
    canActivate: [permissionGuard],
    data: { permission: "Spray.View" },
    loadComponent: () =>
      import("./spray-list/spray-list-page.component").then(
        (m) => m.SprayListPageComponent,
      ),
  },
  {
    path: "new",
    title: "Plan Spray Application",
    canActivate: [permissionGuard],
    data: { permission: "Spray.Create" },
    loadComponent: () =>
      import("./spray-editor/spray-editor-page.component").then(
        (m) => m.SprayEditorPageComponent,
      ),
  },
  {
    path: ":id/edit",
    title: "Edit Spray Application",
    canActivate: [permissionGuard],
    data: { permission: "Spray.Update" },
    loadComponent: () =>
      import("./spray-editor/spray-editor-page.component").then(
        (m) => m.SprayEditorPageComponent,
      ),
  },
  {
    path: ":id/start",
    title: "Start Spray Application",
    canActivate: [permissionGuard],
    data: { permission: "Spray.Start" },
    loadComponent: () =>
      import("./spray-execution/spray-execution-page.component").then(
        (m) => m.SprayExecutionPageComponent,
      ),
  },
  {
    path: ":id/execution",
    title: "Spray Execution Progress",
    canActivate: [permissionGuard],
    data: { permission: "Spray.Start" },
    loadComponent: () =>
      import("./spray-execution/spray-execution-page.component").then(
        (m) => m.SprayExecutionPageComponent,
      ),
  },
  {
    path: ":id",
    title: "Spray Application Details",
    canActivate: [permissionGuard],
    data: { permission: "Spray.View" },
    loadComponent: () =>
      import("./spray-detail/spray-detail-page.component").then(
        (m) => m.SprayDetailPageComponent,
      ),
  },
];
