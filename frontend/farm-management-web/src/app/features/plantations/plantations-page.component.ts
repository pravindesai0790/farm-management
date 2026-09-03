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
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatSnackBar } from "@angular/material/snack-bar";
import { RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { Plantation } from "../../core/farm-management/farm-management.models";
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
    "crop",
    "area",
    "date",
    "status",
    "actions",
  ];
  readonly plantations = signal<readonly Plantation[]>([]);
  readonly status = signal("");
  readonly isLoading = signal(false);
  ngOnInit(): void {
    this.load();
  }
  load(): void {
    this.isLoading.set(true);
    this.service
      .listPlantations(undefined, undefined, this.status() || undefined)
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
