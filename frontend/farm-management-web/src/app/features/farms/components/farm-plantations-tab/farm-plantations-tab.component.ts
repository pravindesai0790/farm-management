import { DatePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import { ActiveCycleSummary, Plantation } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-farm-plantations-tab",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatTableModule,
    MatTooltipModule,
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
  readonly activeCycles = input<readonly ActiveCycleSummary[]>([]);

  readonly displayedColumns: string[] = [
    "plantation",
    "crop",
    "area",
    "allocatedArea",
    "currentCycle",
    "stageProgress",
    "plantingDate",
    "status",
    "actions",
  ];

  getPlantationCycle(plantationId: string): ActiveCycleSummary | undefined {
    return this.activeCycles().find((c) => c.plantationId === plantationId);
  }
}
