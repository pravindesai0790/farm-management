import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatTooltipModule } from "@angular/material/tooltip";
import { CropAllocationSummary } from "../../../../core/farm-management/farm-management.models";

@Component({
  selector: "app-dashboard-crop-distribution",
  standalone: true,
  imports: [
    DecimalPipe,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
  ],
  templateUrl: "./dashboard-crop-distribution.component.html",
  styleUrl: "./dashboard-crop-distribution.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCropDistributionComponent {
  readonly cropAllocations = input.required<readonly CropAllocationSummary[]>();

  private readonly cropColors = [
    "#2f6f4e",
    "#d97706",
    "#2563eb",
    "#7c3aed",
    "#dc2626",
    "#0d9488",
    "#ea580c",
    "#4f46e5",
  ];

  getCropColor(index: number): string {
    return this.cropColors[index % this.cropColors.length];
  }
}
