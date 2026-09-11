import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatChipsModule } from "@angular/material/chips";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PermissionService } from "../../core/auth/permission.service";

export interface LaborCard {
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
  selector: "app-labor-page",
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
  templateUrl: "./labor-page.component.html",
  styleUrl: "./labor-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LaborPageComponent {
  readonly permissionService = inject(PermissionService);

  readonly cards: readonly LaborCard[] = [
    {
      id: "workers",
      title: "Workers",
      category: "Workforce",
      description:
        "Maintain worker master profiles, employment details, gender wage classification, and farm assignments.",
      icon: "badge",
      route: "/labor/workers",
      status: "AVAILABLE",
      requiredPermission: "Worker.View",
      createPermission: "Worker.Create",
      createRoute: "/labor/workers/new",
      highlights: [
        "Worker profiles",
        "Employment types",
        "Farm assignments",
        "Active roster",
      ],
    },
    {
      id: "contractors",
      title: "Contractors",
      category: "Workforce",
      description:
        "Manage third-party labor contractors, contact persons, and contracted farm workforce.",
      icon: "business_center",
      route: "/labor/contractors",
      status: "UPCOMING",
      requiredPermission: "Contractor.View",
      highlights: [
        "Labor contracting",
        "Contact persons",
        "Worker links",
        "Contracted workforce",
      ],
    },
    {
      id: "categories",
      title: "Labor categories",
      category: "Master data",
      description:
        "Configure skilled, semi-skilled, supervisor, and operator workforce categories.",
      icon: "category",
      status: "UPCOMING",
      highlights: [
        "Skill levels",
        "Supervisory roles",
        "Global & org categories",
      ],
    },
    {
      id: "wage-rates",
      title: "Wage rates",
      category: "Payroll foundation",
      description:
        "Define organization-level gender and wage-type rates with effective date preservation.",
      icon: "payments",
      status: "UPCOMING",
      highlights: [
        "Gender-based rates",
        "Effective dating",
        "Daily & hourly rates",
      ],
    },
  ];

  canAccess(card: LaborCard): boolean {
    if (!card.requiredPermission) return true;
    return this.permissionService.has(card.requiredPermission);
  }
}
