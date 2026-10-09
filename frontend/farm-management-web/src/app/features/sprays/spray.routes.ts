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
];
