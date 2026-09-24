import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { Farm } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-farm-header",
  standalone: true,
  imports: [
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./farm-header.component.html",
  styleUrl: "./farm-header.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmHeaderComponent {
  readonly permissionService = inject(PermissionService);
  
  readonly farm = input.required<Farm>();
  readonly currentSeason = input<string | null>(null);
}
