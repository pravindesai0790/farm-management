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
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { catchError, forkJoin, of } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import { CropCycle, CropCycleLifecycle } from "../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
import { CropCycleInputsTabComponent } from "./components/crop-cycle-inputs-tab/crop-cycle-inputs-tab.component";
import { CropCycleLifecycleTabComponent } from "./components/crop-cycle-lifecycle-tab/crop-cycle-lifecycle-tab.component";
import { CropCycleCancelDialogComponent } from "./crop-cycle-cancel-dialog.component";
import { CropCyclePlantationPromptDialogComponent } from "./dialogs/crop-cycle-plantation-prompt-dialog.component";
import { PlantationTerminateDialogComponent } from "../plantations/plantation-terminate-dialog.component";

@Component({
  selector: "app-crop-cycle-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    RouterLink,
    CropCycleLifecycleTabComponent,
    CropCycleInputsTabComponent,
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
  readonly lifecycle = signal<CropCycleLifecycle | null>(null);
  readonly isLoading = signal(true);
  readonly isLifecycleLoading = signal(false);
  readonly selectedTabIndex = signal<number>(0);

  ngOnInit(): void {
    this.load(false);
  }

  load(silent = false): void {
    if (!silent) {
      this.isLoading.set(true);
    }
    this.isLifecycleLoading.set(true);

    forkJoin({
      cycle: this.service.getCycle(this.id),
      lifecycle: this.service.getCycleLifecycle(this.id).pipe(catchError(() => of(null))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ cycle: r, lifecycle: lc }) => {
          this.cycle.set(r);
          this.lifecycle.set(lc);
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
          this.isLifecycleLoading.set(false);
        },
        error: (e) => {
          this.isLoading.set(false);
          this.isLifecycleLoading.set(false);
          this.snack.open(
            getApiErrorMessage(e, "Cycle could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }

  refreshLifecycle(): void {
    this.load(true);
  }

  onTabChange(index: number): void {
    this.selectedTabIndex.set(index);
  }

  today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  run(action: "start" | "harvest"): void {
    const request =
      action === "start"
        ? this.service.startCycle(this.id, this.today())
        : this.service.harvestCycle(this.id, this.today());
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.open(`Cycle ${action}ed.`, "Dismiss", { duration: 3000 });
        this.refreshLifecycle();
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
    const cycle = this.cycle();
    if (!cycle) return;

    this.service
      .completeCycle(this.id, this.today())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.refreshLifecycle();

          if (!response?.requiresPlantationTerminationPrompt) {
            this.snack.open(
              "Cycle completed. Plantation remains active for future crop cycles.",
              "Dismiss",
              { duration: 4000 },
            );
            return;
          }

          const promptRef = this.dialog.open(CropCyclePlantationPromptDialogComponent, {
            data: {
              cycleName: cycle.cycleName,
              cropName: response.cropName || cycle.cropName,
              cropDurationType: response.cropDurationType || cycle.cropDurationType || "Non-perennial",
              plantationName: response.plantationName || cycle.plantationName,
              farmAreaName: cycle.farmAreaName,
            },
            width: "480px",
            disableClose: true,
          });

          promptRef
            .afterClosed()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((confirmed) => {
              if (!confirmed) {
                this.snack.open(
                  "Cycle completed. Plantation kept active.",
                  "Dismiss",
                  { duration: 3000 },
                );
                return;
              }

              const termRef = this.dialog.open(PlantationTerminateDialogComponent, {
                data: {
                  plantationId: response.plantationId || cycle.plantationId,
                  plantationName: response.plantationName || cycle.plantationName,
                  defaultNotes: `Terminated after completing crop cycle ${cycle.cycleName}.`,
                },
                width: "500px",
              });

              termRef
                .afterClosed()
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe((termResult) => {
                  if (!termResult) return;
                  this.service
                    .terminatePlantation(response.plantationId || cycle.plantationId, termResult)
                    .pipe(takeUntilDestroyed(this.destroyRef))
                    .subscribe({
                      next: () => {
                        this.snack.open(
                          "Plantation terminated. Farm area is now available for reuse.",
                          "Dismiss",
                          { duration: 4000 },
                        );
                        this.refreshLifecycle();
                      },
                      error: (e) =>
                        this.snack.open(
                          getApiErrorMessage(e, "Plantation could not be terminated."),
                          "Dismiss",
                          { duration: 5000 },
                        ),
                    });
                });
            });
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Cycle action could not be completed."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  cancel(): void {
    const cycle = this.cycle();
    if (!cycle) return;
    const dialogRef = this.dialog.open(CropCycleCancelDialogComponent, {
      data: {
        cycleId: cycle.id,
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
              this.refreshLifecycle();
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
