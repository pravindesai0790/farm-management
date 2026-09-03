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
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  CropCycle,
  Plantation,
} from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-plantation-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
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
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly plantation = signal<Plantation | null>(null);
  readonly cycles = signal<readonly CropCycle[]>([]);
  readonly isLoading = signal(true);
  ngOnInit(): void {
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
  terminate(): void {
    const reason = prompt("Enter the termination reason ID");
    if (!reason) return;
    this.service
      .terminatePlantation(this.id, {
        terminationDate: new Date().toISOString().slice(0, 10),
        endReasonId: reason,
        notes: "Terminated from plantation details.",
        cancelActiveCycles: true,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.snack.open("Plantation terminated.", "Dismiss", {
            duration: 3000,
          });
          void this.router.navigateByUrl("/plantations");
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Plantation could not be terminated."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }
}
