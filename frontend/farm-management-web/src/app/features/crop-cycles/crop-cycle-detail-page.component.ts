import { DatePipe } from "@angular/common";
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
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { CropCycle } from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
import { CropCycleCancelDialogComponent } from "./crop-cycle-cancel-dialog.component";
@Component({
  selector: "app-crop-cycle-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: "./crop-cycle-detail-page.component.html",
  styleUrl: "./crop-cycle-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCycleDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly permissionService = inject(PermissionService);
  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly cycle = signal<CropCycle | null>(null);
  readonly isLoading = signal(true);
  ngOnInit(): void {
    this.load();
  }
  load(): void {
    this.service
      .getCycle(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.cycle.set(r);
          this.breadcrumbService.setEntityName(r.id, r.cycleName);
          this.service.getPlantation(r.plantationId).subscribe({
            next: (p) => {
              const cachedFarmName = this.breadcrumbService.getEntityName(p.farmId);
              this.breadcrumbService.setTrail([
                { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
                { label: "Farms", route: "/farms" },
                { label: cachedFarmName ?? "Farm", route: ["/farms", p.farmId] },
                { label: p.farmAreaName || "Farm Area", route: ["/farm-areas", p.farmAreaId] },
                { label: p.plantationName, route: ["/plantations", p.id] },
                { label: r.cycleName },
              ]);
              if (!cachedFarmName && p.farmId) {
                this.service.getFarm(p.farmId).subscribe({
                  next: (farm) => {
                    this.breadcrumbService.setEntityName(farm.id, farm.name);
                    this.breadcrumbService.setTrail([
                      { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
                      { label: "Farms", route: "/farms" },
                      { label: farm.name, route: ["/farms", farm.id] },
                      { label: p.farmAreaName || "Farm Area", route: ["/farm-areas", p.farmAreaId] },
                      { label: p.plantationName, route: ["/plantations", p.id] },
                      { label: r.cycleName },
                    ]);
                  },
                });
              }
            },
            error: () => {
              this.breadcrumbService.setTrail([
                { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
                { label: "Farms", route: "/farms" },
                { label: "Plantations", route: "/plantations" },
                { label: r.plantationName || "Plantation", route: ["/plantations", r.plantationId] },
                { label: r.cycleName },
              ]);
            },
          });
          this.isLoading.set(false);
        },
        error: (e) => {
          this.isLoading.set(false);
          this.snack.open(
            getApiErrorMessage(e, "Cycle could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }
  today(): string {
    return new Date().toISOString().slice(0, 10);
  }
  run(action: "start" | "harvest" | "complete"): void {
    const request =
      action === "start"
        ? this.service.startCycle(this.id, this.today())
        : action === "harvest"
          ? this.service.harvestCycle(this.id, this.today())
          : this.service.completeCycle(this.id, this.today());
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.open(`Cycle ${action}ed.`, "Dismiss", { duration: 3000 });
        this.load();
      },
      error: (e) =>
        this.snack.open(
          getApiErrorMessage(e, "Cycle action could not be completed."),
          "Dismiss",
          { duration: 5000 },
        ),
    });
  }
  start(): void {
    this.run("start");
  }
  harvest(): void {
    this.run("harvest");
  }
  complete(): void {
    this.run("complete");
  }
  cancel(): void {
    const cycle = this.cycle();
    if (!cycle) return;
    const dialogRef = this.dialog.open(CropCycleCancelDialogComponent, {
      data: {
        cycleId: cycle.id,
        cycleCode: cycle.cycleCode,
        cycleName: cycle.cycleName,
      },
      width: "480px",
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.service
          .cancelCycle(this.id, result)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.snack.open("Cycle cancelled.", "Dismiss", { duration: 3000 });
              this.load();
            },
            error: (e) =>
              this.snack.open(
                getApiErrorMessage(e, "Cycle could not be cancelled."),
                "Dismiss",
                { duration: 5000 },
              ),
          });
      });
  }
}
