import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { debounceTime, distinctUntilChanged, finalize, merge } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { CropLifecycleTemplate } from "../../core/crop-lifecycle-templates/crop-lifecycle-template.models";
import { CropLifecycleTemplateService } from "../../core/crop-lifecycle-templates/crop-lifecycle-template.service";
import { Crop } from "../../core/farm-management/farm-management.models";
import { FarmManagementService } from "../../core/farm-management/farm-management.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";

@Component({
  selector: "app-crop-lifecycle-templates-page",
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: "./crop-lifecycle-templates-page.component.html",
  styleUrl: "./crop-lifecycle-templates-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropLifecycleTemplatesPageComponent implements OnInit {
  private readonly templateService = inject(CropLifecycleTemplateService);
  private readonly farmService = inject(FarmManagementService);
  private readonly fb = inject(FormBuilder);
  private readonly snack = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  readonly permissionService = inject(PermissionService);

  readonly columns = [
    "crop",
    "templateName",
    "default",
    "system",
    "active",
    "stageCount",
    "actions",
  ];

  readonly templates = signal<readonly CropLifecycleTemplate[]>([]);
  readonly crops = signal<readonly Crop[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly isLoading = signal(false);

  readonly filterForm = this.fb.nonNullable.group({
    cropId: ["all"],
    status: ["all"],
  });

  ngOnInit(): void {
    const queryCropId = this.route.snapshot.queryParamMap.get("cropId");
    if (queryCropId) {
      this.filterForm.controls.cropId.setValue(queryCropId);
    }

    this.loadCrops();

    this.filterForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });

    this.load();
  }

  loadCrops(): void {
    this.farmService
      .listCrops(1, 100, "", true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.crops.set(res.items),
        error: () => {},
      });
  }

  load(): void {
    const cropIdVal = this.filterForm.controls.cropId.value;
    const statusVal = this.filterForm.controls.status.value;

    this.isLoading.set(true);
    this.templateService
      .list({
        page: this.pageIndex() + 1,
        pageSize: this.pageSize(),
        cropId: cropIdVal === "all" ? null : cropIdVal,
        isActive: statusVal === "all" ? null : statusVal === "active",
      })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (res) => {
          this.templates.set(res.items);
          this.totalCount.set(res.totalCount);
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Lifecycle templates could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  pageChanged(e: PageEvent): void {
    this.pageIndex.set(e.pageIndex);
    this.pageSize.set(e.pageSize);
    this.load();
  }

  changeStatus(template: CropLifecycleTemplate, activate: boolean): void {
    const request = activate
      ? this.templateService.activate(template.id)
      : this.templateService.deactivate(template.id);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.snack.open(
          activate ? "Template activated." : "Template deactivated.",
          "Dismiss",
          { duration: 3000 },
        );
        this.load();
      },
      error: (e) =>
        this.snack.open(
          getApiErrorMessage(e, "Template status could not be changed."),
          "Dismiss",
          { duration: 5000 },
        ),
    });
  }

  canModify(template: CropLifecycleTemplate): boolean {
    if (template.isSystem) {
      return this.permissionService.hasRole("GlobalAdmin");
    }
    return true;
  }
}
