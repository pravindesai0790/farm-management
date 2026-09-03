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
import { MatTableModule } from "@angular/material/table";
import { RouterLink } from "@angular/router";
import { forkJoin } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { FarmArea } from "../../core/farm-management/farm-management.models";
@Component({
  selector: "app-farm-areas-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink,
  ],
  templateUrl: "./farm-areas-page.component.html",
  styleUrl: "./farm-areas-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FarmAreasPageComponent implements OnInit {
  private readonly service = inject(FarmManagementService);
  private readonly destroyRef = inject(DestroyRef);
  readonly permissionService = inject(PermissionService);
  readonly columns = ["farm", "area", "size", "status", "actions"];
  readonly areas = signal<readonly FarmArea[]>([]);
  readonly farmNames = signal<Record<string, string>>({});
  readonly isLoading = signal(true);
  ngOnInit(): void {
    this.service
      .listFarms(1, 100, "", null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (farms) => {
          const requests = farms.items.map((farm) =>
            this.service.listAreas(farm.id),
          );
          forkJoin(requests)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((areaLists) => {
              this.areas.set(areaLists.flat());
              this.farmNames.set(
                Object.fromEntries(
                  farms.items.map((farm) => [farm.id, farm.name]),
                ),
              );
              this.isLoading.set(false);
            });
        },
        error: () => this.isLoading.set(false),
      });
  }
}
