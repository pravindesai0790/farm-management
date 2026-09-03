import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";

import { PermissionService } from "../../core/auth/permission.service";

@Component({
  selector: "app-administration-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: "./administration-page.component.html",
  styleUrl: "./administration-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdministrationPageComponent {
  readonly permissionService = inject(PermissionService);
}
