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
];
