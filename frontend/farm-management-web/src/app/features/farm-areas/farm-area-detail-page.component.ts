import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { forkJoin } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  FarmArea,
  FarmAreaAvailability,
} from "../../core/farm-management/farm-management.models";
@Component({
  selector: "app-farm-area-detail-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: "./farm-area-detail-page.component.html",
  styleUrl: "./farm-area-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmAreaDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);
  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly area = signal<FarmArea | null>(null);
  readonly availability = signal<FarmAreaAvailability | null>(null);
  readonly isLoading = signal(true);
  ngOnInit(): void {
    forkJoin({
      area: this.service.getArea(this.id),
      availability: this.service.getAreaAvailability(this.id),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.area.set(r.area);
          this.availability.set(r.availability);
          this.breadcrumbService.setEntityName(r.area.id, r.area.name);
          const cachedFarmName = this.breadcrumbService.getEntityName(r.area.farmId);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Farms", route: "/farms" },
            { label: cachedFarmName ?? "Farm", route: ["/farms", r.area.farmId] },
            { label: r.area.name },
          ]);
          if (!cachedFarmName && r.area.farmId) {
            this.service.getFarm(r.area.farmId).subscribe({
              next: (farm) => {
                this.breadcrumbService.setEntityName(farm.id, farm.name);
                this.breadcrumbService.setTrail([
                  { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
                  { label: "Farms", route: "/farms" },
                  { label: farm.name, route: ["/farms", farm.id] },
                  { label: r.area.name },
                ]);
              },
            });
          }
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
  }
}
