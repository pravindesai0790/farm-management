import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink } from "@angular/router";

import { PermissionService } from "../../core/auth/permission.service";

@Component({
  selector: "app-admin-home-page",
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  templateUrl: "./admin-home-page.component.html",
  styleUrl: "./admin-home-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminHomePageComponent {
  readonly permissionService = inject(PermissionService);
}
