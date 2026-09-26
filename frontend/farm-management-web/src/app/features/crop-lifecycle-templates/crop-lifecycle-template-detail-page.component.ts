import { DatePipe } from "@angular/common";
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
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { PermissionService } from "../../core/auth/permission.service";
import { BreadcrumbService } from "../../core/breadcrumb/breadcrumb.service";
import {
  CropLifecycleTemplate,
} from "../../core/crop-lifecycle-templates/crop-lifecycle-template.models";
import { CropLifecycleTemplateService } from "../../core/crop-lifecycle-templates/crop-lifecycle-template.service";
import { getApiErrorMessage } from "../../core/models/api-error.model";

@Component({
  selector: "app-crop-lifecycle-template-detail-page",
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTooltipModule,
    RouterLink,
  ],
  templateUrl: "./crop-lifecycle-template-detail-page.component.html",
  styleUrl: "./crop-lifecycle-template-detail-page.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CropLifecycleTemplateDetailPageComponent implements OnInit {
  private readonly service = inject(CropLifecycleTemplateService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly snack = inject(MatSnackBar);

  readonly permissionService = inject(PermissionService);

  readonly id = this.route.snapshot.paramMap.get("id")!;
  readonly template = signal<CropLifecycleTemplate | null>(null);
  readonly isLoading = signal(true);

  readonly stageColumns = [
    "sequence",
    "stageName",
    "expectedDuration",
    "description",
    "status",
  ];

  readonly totalDurationDays = computed(() => {
    const t = this.template();
    if (!t || !t.stages) return 0;
    return t.stages.reduce((sum, s) => sum + (s.expectedDurationDays || 0), 0);
  });

  readonly totalDurationMonths = computed(() => {
    const days = this.totalDurationDays();
    return (days / 30.4).toFixed(1);
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.service
      .get(this.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (t) => {
          this.template.set(t);
          this.breadcrumbService.setEntityName(t.id, t.name);
          this.breadcrumbService.setTrail([
            { label: "Dashboard", route: "/dashboard", icon: "space_dashboard" },
            { label: "Lifecycle templates", route: "/crop-lifecycle-templates" },
            { label: t.name },
          ]);
        },
        error: (e) =>
          this.snack.open(
            getApiErrorMessage(e, "Lifecycle template could not be loaded."),
            "Dismiss",
            { duration: 5000 },
          ),
      });
  }

  canModify(): boolean {
    const t = this.template();
    if (!t) return false;
    if (t.isSystem) {
      return this.permissionService.hasRole("GlobalAdmin");
    }
    return true;
  }

  toggleTemplateStatus(activate: boolean): void {
    const t = this.template();
    if (!t) return;
    const req$ = activate
      ? this.service.activate(t.id)
      : this.service.deactivate(t.id);

    req$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
          getApiErrorMessage(e, "Status could not be updated."),
          "Dismiss",
          { duration: 5000 },
        ),
    });
  }
}
