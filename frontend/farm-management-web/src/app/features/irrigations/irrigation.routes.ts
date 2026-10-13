import { Routes } from "@angular/router";
import { permissionGuard } from "../../core/guards/permission.guard";

export const IRRIGATION_ROUTES: Routes = [
  {
    path: "",
    title: "Irrigation Management",
    canActivate: [permissionGuard],
    data: { permission: "Irrigation.View" },
    loadComponent: () =>
      import("./irrigation-list/irrigation-list-page.component").then(
        (m) => m.IrrigationListPageComponent,
      ),
  },
  {
    path: "new",
    title: "Plan Irrigation Event",
    canActivate: [permissionGuard],
    data: { permission: "Irrigation.Create" },
    loadComponent: () =>
      import("./irrigation-editor/irrigation-editor-page.component").then(
        (m) => m.IrrigationEditorPageComponent,
      ),
  },
  {
    path: ":id/edit",
    title: "Edit Irrigation Draft",
    canActivate: [permissionGuard],
    data: { permission: "Irrigation.Update" },
    loadComponent: () =>
      import("./irrigation-editor/irrigation-editor-page.component").then(
        (m) => m.IrrigationEditorPageComponent,
      ),
  },
  {
    path: ":id",
    title: "Irrigation Event Details",
    canActivate: [permissionGuard],
    data: { permission: "Irrigation.View" },
    loadComponent: () =>
      import("./irrigation-detail/irrigation-detail-page.component").then(
        (m) => m.IrrigationDetailPageComponent,
      ),
  },
];
