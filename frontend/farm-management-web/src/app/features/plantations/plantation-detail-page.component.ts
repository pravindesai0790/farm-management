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
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  CropCycle,
  Plantation,
} from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
import { PlantationTerminateDialogComponent } from "./plantation-terminate-dialog.component";
@Component({
  selector: "app-plantation-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    RouterLink,
  ],
  templateUrl: "./plantation-detail-page.component.html",
  styleUrl: "./plantation-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantationDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly plantation = signal<Plantation | null>(null);
  readonly cycles = signal<readonly CropCycle[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service
      .getPlantation(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (p) => {
          this.plantation.set(p);
          this.service
            .listCycles(p.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((r) => this.cycles.set(r.items));
          this.isLoading.set(false);
        },
        error: (e) => {
          this.snack.open(
            getApiErrorMessage(e, "Plantation could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
          this.isLoading.set(false);
        },
      });
  }

  activate(): void {
    this.service
      .activatePlantation(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.open("Plantation activated.", "Dismiss", {
            duration: 3000,
          });
          this.load();
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Plantation could not be activated."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  terminate(): void {
    const p = this.plantation();
    if (!p) return;
    const dialogRef = this.dialog.open(PlantationTerminateDialogComponent, {
      data: {
        plantationId: p.id,
        plantationCode: p.plantationCode,
        plantationName: p.plantationName,
      },
      width: "500px",
    });

    dialogRef
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) return;
        this.service
          .terminatePlantation(this.id, result)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.snack.open("Plantation terminated.", "Dismiss", {
                duration: 3000,
              });
              this.load();
            },
            error: (e) =>
              this.snack.open(
                getApiErrorMessage(e, "Plantation could not be terminated."),
                "Dismiss",
                { duration: 5000 },
              ),
          });
      });
  }

  archive(): void {
    this.service
      .archivePlantation(this.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.open("Plantation archived.", "Dismiss", {
            duration: 3000,
          });
          this.load();
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Plantation could not be archived."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  onStatusChange(targetStatus: string): void {
    const current = this.plantation();
    if (!current || targetStatus === current.status) return;

    if (targetStatus === "ACTIVE" && current.status === "PLANNED") {
      this.activate();
    } else if (targetStatus === "TERMINATED" && current.status === "ACTIVE") {
      this.terminate();
    } else if (targetStatus === "ARCHIVED" && current.status === "TERMINATED") {
      this.archive();
    }
  }

  isStatusActionDisabled(p: Plantation): boolean {
    if (p.status === "ARCHIVED") return true;
    if (p.status === "PLANNED" && !this.permissionService.has("Plantation.Activate")) return true;
    if (p.status === "ACTIVE" && !this.permissionService.has("Plantation.Terminate")) return true;
    if (p.status === "TERMINATED" && !this.permissionService.has("Plantation.Update")) return true;
    return false;
  }
}
