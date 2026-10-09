import { CommonModule } from "@angular/common";
import { Component, OnInit, computed, inject, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterModule } from "@angular/router";
import { PermissionService } from "../../../core/auth/permission.service";
import { getApiErrorMessage } from "../../../core/models/api-error.model";
import {
  ApplicationMethodItem,
  MasterTypeKind,
  ProductTypeItem,
  TargetItem,
  TargetType,
} from "../../../core/sprays/spray-master-data.models";
import { SprayMasterDataService } from "../../../core/sprays/spray-master-data.service";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";
import {
  SprayMasterEditorDialogComponent,
  SprayMasterEditorDialogData,
} from "./dialogs/spray-master-editor-dialog.component";

@Component({
  selector: "app-spray-master-data-page",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatTabsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: "./spray-master-data-page.component.html",
  styleUrl: "./spray-master-data-page.component.scss",
})
export class SprayMasterDataPageComponent implements OnInit {
  private readonly masterService = inject(SprayMasterDataService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  readonly permissionService = inject(PermissionService);

  readonly activeTab = signal<number>(0);
  readonly isLoading = signal(false);

  // Raw data signals
  readonly productTypes = signal<readonly ProductTypeItem[]>([]);
  readonly targets = signal<readonly TargetItem[]>([]);
  readonly applicationMethods = signal<readonly ApplicationMethodItem[]>([]);

  // Filter signals
  readonly searchFilter = signal<string>("");
  readonly statusFilter = signal<string>("ALL");
  readonly targetTypeFilter = signal<string>("ALL");

  readonly targetTypeOptions: readonly TargetType[] = [
    "Disease",
    "Insect",
    "Mite",
    "Weed",
    "Other",
  ];

  // Table columns
  readonly productTypeColumns: readonly string[] = [
    "code",
    "name",
    "ownership",
    "description",
    "displayOrder",
    "status",
    "actions",
  ];

  readonly targetColumns: readonly string[] = [
    "code",
    "name",
    "targetType",
    "ownership",
    "description",
    "displayOrder",
    "status",
    "actions",
  ];

  readonly applicationMethodColumns: readonly string[] = [
    "code",
    "name",
    "ownership",
    "description",
    "displayOrder",
    "status",
    "actions",
  ];

  // Filtered computed lists
  readonly filteredProductTypes = computed(() => {
    const search = this.searchFilter().trim().toLowerCase();
    const status = this.statusFilter();

    return this.productTypes().filter((item) => {
      const matchesSearch =
        !search ||
        item.name.toLowerCase().includes(search) ||
        item.code.toLowerCase().includes(search) ||
        (item.description && item.description.toLowerCase().includes(search));

      const matchesStatus =
        status === "ALL" ||
        (status === "ACTIVE" && item.isActive) ||
        (status === "INACTIVE" && !item.isActive);

      return matchesSearch && matchesStatus;
    });
  });

  readonly filteredTargets = computed(() => {
    const search = this.searchFilter().trim().toLowerCase();
    const status = this.statusFilter();
    const typeFilter = this.targetTypeFilter();

    return this.targets().filter((item) => {
      const matchesSearch =
        !search ||
        item.name.toLowerCase().includes(search) ||
        item.code.toLowerCase().includes(search) ||
        (item.description && item.description.toLowerCase().includes(search));

      const matchesStatus =
        status === "ALL" ||
        (status === "ACTIVE" && item.isActive) ||
        (status === "INACTIVE" && !item.isActive);

      const matchesType =
        typeFilter === "ALL" ||
        item.targetType.toLowerCase() === typeFilter.toLowerCase();

      return matchesSearch && matchesStatus && matchesType;
    });
  });

  readonly filteredApplicationMethods = computed(() => {
    const search = this.searchFilter().trim().toLowerCase();
    const status = this.statusFilter();

    return this.applicationMethods().filter((item) => {
      const matchesSearch =
        !search ||
        item.name.toLowerCase().includes(search) ||
        item.code.toLowerCase().includes(search) ||
        (item.description && item.description.toLowerCase().includes(search));

      const matchesStatus =
        status === "ALL" ||
        (status === "ACTIVE" && item.isActive) ||
        (status === "INACTIVE" && !item.isActive);

      return matchesSearch && matchesStatus;
    });
  });

  // Current tab metadata
  readonly currentKind = computed<MasterTypeKind>(() => {
    switch (this.activeTab()) {
      case 0:
        return "product-type";
      case 1:
        return "target";
      default:
        return "application-method";
    }
  });

  readonly currentItems = computed(() => {
    switch (this.activeTab()) {
      case 0:
        return this.productTypes();
      case 1:
        return this.targets();
      default:
        return this.applicationMethods();
    }
  });

  readonly totalCount = computed(() => this.currentItems().length);
  readonly systemCount = computed(() => this.currentItems().filter((x) => x.isSystem).length);
  readonly customCount = computed(() => this.currentItems().filter((x) => !x.isSystem).length);
  readonly activeCount = computed(() => this.currentItems().filter((x) => x.isActive).length);
  readonly inactiveCount = computed(() => this.currentItems().filter((x) => !x.isActive).length);

  // Permissions
  readonly canCreateCurrent = computed(() => {
    switch (this.activeTab()) {
      case 0:
        return this.permissionService.has("ProductType.Create");
      case 1:
        return this.permissionService.has("Target.Create");
      default:
        return this.permissionService.has("ApplicationMethod.Create");
    }
  });

  readonly canUpdateCurrent = computed(() => {
    switch (this.activeTab()) {
      case 0:
        return this.permissionService.has("ProductType.Update");
      case 1:
        return this.permissionService.has("Target.Update");
      default:
        return this.permissionService.has("ApplicationMethod.Update");
    }
  });

  readonly canToggleCurrent = computed(() => {
    switch (this.activeTab()) {
      case 0:
        return (
          this.permissionService.has("ProductType.Activate") &&
          this.permissionService.has("ProductType.Deactivate")
        );
      case 1:
        return (
          this.permissionService.has("Target.Activate") &&
          this.permissionService.has("Target.Deactivate")
        );
      default:
        return (
          this.permissionService.has("ApplicationMethod.Activate") &&
          this.permissionService.has("ApplicationMethod.Deactivate")
        );
    }
  });

  ngOnInit(): void {
    this.loadAllMasters();
  }

  loadAllMasters(): void {
    this.isLoading.set(true);

    this.masterService.listProductTypes(true).subscribe({
      next: (pts) => this.productTypes.set(pts),
      error: (err) => this.showError(err, "Failed to load product types."),
    });

    this.masterService.listTargets(null, true).subscribe({
      next: (tg) => this.targets.set(tg),
      error: (err) => this.showError(err, "Failed to load targets."),
    });

    this.masterService.listApplicationMethods(true).subscribe({
      next: (am) => {
        this.applicationMethods.set(am);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.showError(err, "Failed to load application methods.");
      },
    });
  }

  onTabChanged(index: number): void {
    this.activeTab.set(index);
    this.searchFilter.set("");
    this.statusFilter.set("ALL");
    this.targetTypeFilter.set("ALL");
  }

  openCreateDialog(): void {
    const dialogData: SprayMasterEditorDialogData = {
      kind: this.currentKind(),
    };

    const ref = this.dialog.open(SprayMasterEditorDialogComponent, {
      width: "560px",
      data: dialogData,
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadAllMasters();
      }
    });
  }

  openEditDialog(item: ProductTypeItem | TargetItem | ApplicationMethodItem): void {
    if (item.isSystem) {
      return;
    }

    const dialogData: SprayMasterEditorDialogData = {
      kind: this.currentKind(),
      item,
    };

    const ref = this.dialog.open(SprayMasterEditorDialogComponent, {
      width: "560px",
      data: dialogData,
    });

    ref.afterClosed().subscribe((saved) => {
      if (saved) {
        this.loadAllMasters();
      }
    });
  }

  toggleStatus(item: ProductTypeItem | TargetItem | ApplicationMethodItem): void {
    if (item.isSystem) {
      return;
    }

    if (item.isActive) {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        width: "420px",
        data: {
          title: `Deactivate ${item.name}?`,
          message: `Deactivating will exclude this ${this.getKindLabel(this.currentKind())} from future spray application forms. Existing records will remain intact.`,
          confirmText: "Deactivate",
          cancelText: "Cancel",
          color: "warn",
        },
      });

      dialogRef.afterClosed().subscribe((confirmed) => {
        if (confirmed) {
          this.executeStatusToggle(item);
        }
      });
    } else {
      this.executeStatusToggle(item);
    }
  }

  private executeStatusToggle(
    item: ProductTypeItem | TargetItem | ApplicationMethodItem,
  ): void {
    const kind = this.currentKind();
    let action$;

    if (kind === "product-type") {
      action$ = item.isActive
        ? this.masterService.deactivateProductType(item.id)
        : this.masterService.activateProductType(item.id);
    } else if (kind === "target") {
      action$ = item.isActive
        ? this.masterService.deactivateTarget(item.id)
        : this.masterService.activateTarget(item.id);
    } else {
      action$ = item.isActive
        ? this.masterService.deactivateApplicationMethod(item.id)
        : this.masterService.activateApplicationMethod(item.id);
    }

    action$.subscribe({
      next: () => {
        this.snack.open(
          `"${item.name}" ${item.isActive ? "deactivated" : "activated"} successfully.`,
          "Close",
          { duration: 3000 },
        );
        this.loadAllMasters();
      },
      error: (err) => {
        this.showError(err, `Failed to update status for "${item.name}".`);
      },
    });
  }

  getKindLabel(kind: MasterTypeKind): string {
    switch (kind) {
      case "product-type":
        return "Product Type";
      case "target":
        return "Target";
      case "application-method":
        return "Application Method";
    }
  }

  private showError(err: unknown, fallback: string): void {
    const msg = getApiErrorMessage(err, fallback);
    this.snack.open(msg, "Close", { duration: 5000 });
  }
}
