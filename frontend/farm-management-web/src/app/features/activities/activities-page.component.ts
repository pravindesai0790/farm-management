import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatChipsModule } from "@angular/material/chips";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PermissionService } from "../../core/auth/permission.service";

export interface FarmActivityCard {
  readonly id: string;
  readonly title: string;
  readonly category: string;
  readonly description: string;
  readonly icon: string;
  readonly route?: string;
  readonly status: "AVAILABLE" | "UPCOMING";
  readonly highlights: readonly string[];
  readonly requiredPermission?: string;
  readonly createPermission?: string;
  readonly createRoute?: string;
}

@Component({
  selector: "app-activities-page",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
  ],
  templateUrl: "./activities-page.component.html",
  styleUrl: "./activities-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivitiesPageComponent {
  readonly permissionService = inject(PermissionService);

  readonly activityCards: readonly FarmActivityCard[] = [
    {
      id: "labor",
      title: "Labor activities",
      category: "Field operations",
      description:
        "Track daily labor work, worker counts, working hours, and operational labor costs across farms and plantations.",
      icon: "assignment",
      route: "/activities/labor-activities",
      status: "AVAILABLE",
      requiredPermission: "LaborActivity.View",
      createPermission: "LaborActivity.Create",
      createRoute: "/activities/labor-activities/new",
      highlights: [
        "Worker logging",
        "Working hours",
        "Cost tracking",
        "Farm & area assignment",
      ],
    },
    {
      id: "spraying",
      title: "Spray applications",
      category: "Crop protection",
      description:
        "Schedule and record chemical, pesticide, and organic spray applications, target pests, and safety intervals.",
      icon: "pest_control",
      status: "UPCOMING",
      highlights: [
        "Chemical & bio sprays",
        "Dosage & dilution",
        "Pre-harvest intervals",
        "Target crop cycle",
      ],
    },
    {
      id: "fertilizer",
      title: "Fertilizer applications",
      category: "Soil & nutrition",
      description:
        "Record fertilizer applications, dosage formulations, and nutritional management across plantations.",
      icon: "science",
      status: "UPCOMING",
      highlights: [
        "Basal & fertigation",
        "NPK formulations",
        "Dosage rates",
        "Nutrient scheduling",
      ],
    },
    {
      id: "irrigation",
      title: "Irrigation management",
      category: "Water management",
      description:
        "Monitor watering sessions, irrigation methods (drip, sprinkler, flood), durations, and water volumes.",
      icon: "water_drop",
      status: "UPCOMING",
      highlights: [
        "Drip & sprinkler methods",
        "Duration & flow rate",
        "Water consumption",
        "Block coverage",
      ],
    },
    {
      id: "disease-pest",
      title: "Disease & pest scouting",
      category: "Field health",
      description:
        "Log scouting observations, pest/disease severity ratings, affected farm areas, and diagnostic notes.",
      icon: "bug_report",
      status: "UPCOMING",
      highlights: [
        "Scouting logs",
        "Infestation severity",
        "Symptoms & notes",
        "Intervention alerts",
      ],
    },
    {
      id: "weather",
      title: "Weather events",
      category: "Climate & risks",
      description:
        "Log severe weather events like heavy rainfall, frost, hail, heatwaves, or drought conditions affecting yield.",
      icon: "thunderstorm",
      status: "UPCOMING",
      highlights: [
        "Heavy rain & flood",
        "Frost & hail logs",
        "Drought monitoring",
        "Insurance impact",
      ],
    },
    {
      id: "harvest",
      title: "Harvest operations",
      category: "Production & yield",
      description:
        "Log crop picking, harvested weights, yield estimates, produce quality grades, and collection batches.",
      icon: "agriculture",
      status: "UPCOMING",
      highlights: [
        "Harvested weight",
        "Quality grading",
        "Yield estimates",
        "Batch tracking",
      ],
    },
    {
      id: "expenses",
      title: "Farm expenses",
      category: "Cost & accounting",
      description:
        "Consolidated operational expenditure linked to farm activities, inputs, contracted machinery, and labor.",
      icon: "receipt_long",
      status: "UPCOMING",
      highlights: [
        "Activity cost links",
        "Input purchases",
        "Cost per hectare",
        "Budget tracking",
      ],
    },
  ];

  canAccess(card: FarmActivityCard): boolean {
    if (!card.requiredPermission) return true;
    return this.permissionService.has(card.requiredPermission);
  }
}
