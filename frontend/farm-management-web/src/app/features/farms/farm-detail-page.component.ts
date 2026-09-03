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
import { ActivatedRoute, RouterLink } from "@angular/router";
import { forkJoin } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  Farm,
  FarmArea,
} from "../../core/farm-management/farm-management.models";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-farm-detail-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    RouterLink,
  ],
  templateUrl: "./farm-detail-page.component.html",
  styleUrl: "./farm-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmDetailPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly farmId = this.route.snapshot.paramMap.get("id")!;
  readonly farm = signal<Farm | null>(null);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly isLoading = signal(true);
  ngOnInit(): void {
    forkJoin({
      farm: this.service.getFarm(this.farmId),
      areas: this.service.listAreas(this.farmId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.farm.set(r.farm);
          this.areas.set(r.areas);
          this.isLoading.set(false);
        },
        error: (e) => {
          this.snack.open(
            getApiErrorMessage(e, "Farm could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
          this.isLoading.set(false);
        },
      });
  }
}
