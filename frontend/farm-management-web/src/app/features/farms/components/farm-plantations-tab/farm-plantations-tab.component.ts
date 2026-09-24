import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatTableModule } from "@angular/material/table";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { Plantation } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-farm-plantations-tab",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: "./farm-plantations-tab.component.html",
  styleUrl: "./farm-plantations-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmPlantationsTabComponent {
  readonly permissionService = inject(PermissionService);

  readonly farmId = input.required<string>();
  readonly plantations = input<readonly Plantation[]>([]);

  readonly displayedColumns: string[] = [
    "plantation",
    "crop",
    "area",
    "allocatedArea",
    "plantingDate",
    "status",
    "actions",
  ];
}
