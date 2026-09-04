import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import {
  Crop,
  Farm,
  FarmArea,
  Plantation,
} from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";

@Component({
  selector: "app-plantations-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./plantations-page.component.html",
  styleUrl: "./plantations-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlantationsPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);

  readonly columns = [
    "plantation",
    "farm",
    "area",
    "crop",
    "date",
    "status",
    "actions",
  ];

  readonly plantations = signal<readonly Plantation[]>([]);
  readonly farms = signal<readonly Farm[]>([]);
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly crops = signal<readonly Crop[]>([]);

  readonly farmId = signal("");
  readonly farmAreaId = signal("");
  readonly cropId = signal("");
  readonly status = signal("");
  readonly isLoading = signal(false);

  readonly hasActiveFilters = computed(
    () =>
      !!this.farmId() ||
      !!this.farmAreaId() ||
      !!this.cropId() ||
      !!this.status(),
  );

  ngOnInit(): void {
    this.service
      .listFarms(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.farms.set(res.items),
      });

    this.service
      .listCrops(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.crops.set(res.items),
      });

    this.load();
  }

  onFarmChange(newFarmId: string): void {
    this.farmId.set(newFarmId);
    this.farmAreaId.set("");
    if (newFarmId) {
      this.service
        .listAreas(newFarmId, true)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (areas) => this.areas.set(areas),
        });
    } else {
      this.areas.set([]);
    }
    this.load();
  }

  onAreaChange(newAreaId: string): void {
    this.farmAreaId.set(newAreaId);
    this.load();
  }

  onCropChange(newCropId: string): void {
    this.cropId.set(newCropId);
    this.load();
  }

  onStatusChange(newStatus: string): void {
    this.status.set(newStatus);
    this.load();
  }

  clearFilters(): void {
    this.farmId.set("");
    this.farmAreaId.set("");
    this.cropId.set("");
    this.status.set("");
    this.areas.set([]);
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.service
      .listPlantations(
        this.farmId() || undefined,
        this.farmAreaId() || undefined,
        this.status() || undefined,
        this.cropId() || undefined,
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.plantations.set(r.items);
          this.isLoading.set(false);
        },
        error: (e) => {
          this.isLoading.set(false);
          this.snack.open(
            getApiErrorMessage(e, "Plantations could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }
}
