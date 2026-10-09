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
];
