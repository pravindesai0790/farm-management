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
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { CropCycle } from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-crop-cycle-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
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
  private readonly destroyRef = inject(DestroyRef);
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
  run(action: "start" | "harvest" | "complete" | "cancel"): void {
    const request =
      action === "start"
        ? this.service.startCycle(this.id, this.today())
        : action === "harvest"
          ? this.service.harvestCycle(this.id, this.today())
          : action === "complete"
            ? this.service.completeCycle(this.id, this.today())
            : this.service.cancelCycle(this.id, {
                cancellationDate: this.today(),
                cancellationReasonId: prompt("Enter cancellation reason ID"),
                notes: "Cancelled from cycle details.",
              });
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
    this.run("cancel");
  }
}
