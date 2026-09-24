import { ChangeDetectionStrategy, Component, inject, input } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink } from "@angular/router";
import { PermissionService } from "../../../../core/auth/permission.service";
import {
  ActiveCycleSummary,
  FarmArea,
  Plantation,
} from "../../../../core/farm-management/farm-management.models";

export interface PlotCardData {
  area: FarmArea;
  plantations: Plantation[];
  activeCycle?: ActiveCycleSummary;
  isFallow: boolean;
}

@Component({
  selector: "app-farm-areas-tab",
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, RouterLink],
  templateUrl: "./farm-areas-tab.component.html",
  styleUrl: "./farm-areas-tab.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmAreasTabComponent {
  readonly permissionService = inject(PermissionService);

  readonly farmId = input.required<string>();
  readonly areas = input<readonly FarmArea[]>([]);
  readonly plantations = input<readonly Plantation[]>([]);
  readonly activeCycles = input<readonly ActiveCycleSummary[]>([]);

  getAreaPlantations(areaId: string): Plantation[] {
    return this.plantations().filter((p) => p.farmAreaId === areaId);
  }

  getAreaActiveCycle(areaName: string): ActiveCycleSummary | undefined {
    return this.activeCycles().find((c) => c.farmAreaName === areaName);
  }
}
