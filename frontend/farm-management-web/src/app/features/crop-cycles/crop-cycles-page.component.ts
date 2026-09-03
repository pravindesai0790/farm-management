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
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatSnackBar } from "@angular/material/snack-bar";
import { RouterLink } from "@angular/router";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { CropCycle } from "../../core/farm-management/farm-management.models";
import { PermissionService } from "../../core/auth/permission.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";
@Component({
  selector: "app-crop-cycles-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: "./crop-cycles-page.component.html",
  styleUrl: "./crop-cycles-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropCyclesPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly columns = ["cycle", "plantation", "dates", "status", "actions"];
  readonly cycles = signal<readonly CropCycle[]>([]);
  readonly status = signal("");
  readonly isLoading = signal(false);
  ngOnInit(): void {
    this.load();
  }
  load(): void {
    this.isLoading.set(true);
    this.service
      .listCycles(undefined, this.status() || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (r) => {
          this.cycles.set(r.items);
          this.isLoading.set(false);
        },
        error: (e) => {
          this.isLoading.set(false);
          this.snack.open(
            getApiErrorMessage(e, "Crop cycles could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          );
        },
      });
  }
}
