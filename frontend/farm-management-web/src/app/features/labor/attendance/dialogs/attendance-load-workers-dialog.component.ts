import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatTooltipModule } from "@angular/material/tooltip";
import { debounceTime, distinctUntilChanged, finalize, forkJoin } from "rxjs";

import {
  AttendanceEligibleWorker,
} from "../../../../core/labor/attendance.models";
import { AttendanceService } from "../../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface AttendanceLoadWorkersDialogData {
  readonly farmId: string;
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly alreadyAddedWorkerIds: readonly string[];
}

@Component({
  selector: "app-attendance-load-workers-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>group_add</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Load Eligible Workers</h2>
          <p class="subtitle">
            {{ data.farmName }} &bull; Attendance Date: {{ data.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <!-- Informational banner -->
        <div class="eligibility-info-banner">
          <mat-icon>verified_user</mat-icon>
          <span>
            Backend-verified: Showing active workers assigned to <strong>{{ data.farmName }}</strong> on <strong>{{ data.attendanceDate }}</strong>.
          </span>
        </div>

        <!-- Filter bar -->
        <form class="filters-bar" [formGroup]="filterForm">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search eligible workers</mat-label>
            <input
              matInput
              formControlName="search"
              placeholder="Name, mobile, or contractor"
              autocomplete="off"
            />
            @if (searchQuery()) {
              <button
                mat-icon-button
                matSuffix
                type="button"
                aria-label="Clear search"
                (click)="clearSearch()"
              >
                <mat-icon>close</mat-icon>
              </button>
            } @else {
              <mat-icon matSuffix>search</mat-icon>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="filter-dropdown">
            <mat-label>Category</mat-label>
            <mat-select formControlName="category">
              <mat-option value="all">All Categories</mat-option>
              @for (cat of uniqueCategories(); track cat) {
                <mat-option [value]="cat">{{ cat }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="filter-dropdown">
            <mat-label>Employment Type</mat-label>
            <mat-select formControlName="employmentType">
              <mat-option value="all">All Types</mat-option>
              @for (type of uniqueEmploymentTypes(); track type) {
                <mat-option [value]="type">{{ formatEmploymentType(type) }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </form>

        @if (isLoading()) {
          <div class="loading-state">
            <mat-spinner diameter="36" />
            <span>Loading assigned workers for {{ data.attendanceDate }}…</span>
          </div>
        } @else if (filteredWorkers().length === 0) {
          <div class="empty-state">
            <mat-icon>person_off</mat-icon>
            @if (hasActiveFilters()) {
              <h4>No workers match your filters</h4>
              <p>
                Try clearing your search query or changing the category / employment type filter.
              </p>
              <button
                mat-stroked-button
                color="primary"
                type="button"
                (click)="resetFilters()"
              >
                <mat-icon>filter_alt_off</mat-icon>
                Reset Filters
              </button>
            } @else {
              <h4>No eligible workers found</h4>
              <p>
                No active farm assignments covering {{ data.attendanceDate }} exist for this farm.
              </p>
            }
          </div>
        } @else {
          <div class="table-container">
            <table mat-table [dataSource]="filteredWorkers()" class="workers-table">
              <!-- 1. Selection Checkbox Column -->
              <ng-container matColumnDef="select">
                <th mat-header-cell *matHeaderCellDef class="checkbox-col">
                  <mat-checkbox
                    [checked]="isAllSelectableChecked()"
                    [indeterminate]="isSomeSelectableChecked()"
                    [disabled]="selectableWorkers().length === 0"
                    (change)="toggleSelectAll($event.checked)"
                    aria-label="Select all available workers"
                    matTooltip="Select all eligible workers"
                  />
                </th>
                <td mat-cell *matCellDef="let worker" class="checkbox-col">
                  @if (isAlreadyAdded(worker.workerId)) {
                    <mat-checkbox
                      [checked]="false"
                      [disabled]="true"
                      matTooltip="Already added to today's attendance"
                      aria-label="Worker already added"
                    />
                  } @else {
                    <mat-checkbox
                      [checked]="selectedIds().has(worker.workerId)"
                      (change)="toggleWorker(worker.workerId, $event.checked)"
                      aria-label="Select worker"
                    />
                  }
                </td>
              </ng-container>

              <!-- 2. Worker Identity: Display Name, Full Name, Mobile -->
              <ng-container matColumnDef="worker">
                <th mat-header-cell *matHeaderCellDef>Worker Identity</th>
                <td mat-cell *matCellDef="let worker">
                  <div class="worker-identity-cell">
                    <div class="worker-name-line">
                      <strong class="display-name">{{ worker.displayName }}</strong>
                      @if (
                        worker.firstName &&
                        worker.lastName &&
                        worker.displayName !== worker.firstName + ' ' + worker.lastName
                      ) {
                        <span class="full-name">({{ worker.firstName }} {{ worker.lastName }})</span>
                      }
                    </div>
                    @if (worker.mobileNumber) {
                      <div class="mobile-line">
                        <mat-icon class="inline-icon">phone</mat-icon>
                        <span>{{ worker.mobileNumber }}</span>
                      </div>
                    }
                  </div>
                </td>
              </ng-container>

              <!-- 3. Gender -->
              <ng-container matColumnDef="gender">
                <th mat-header-cell *matHeaderCellDef class="gender-col">Gender</th>
                <td mat-cell *matCellDef="let worker" class="gender-col">
                  <span class="gender-pill" [attr.data-gender]="worker.gender">
                    {{ worker.gender }}
                  </span>
                </td>
              </ng-container>

              <!-- 4. Labor Category, Employment Type & Contractor -->
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category &amp; Employment</th>
                <td mat-cell *matCellDef="let worker">
                  <div class="category-meta-cell">
                    <div class="tags-row">
                      <span class="badge category-badge">
                        {{ worker.laborCategoryName || "General" }}
                      </span>
                      <span class="badge type-badge">
                        {{ formatEmploymentType(worker.employmentType) }}
                      </span>
                    </div>
                    @if (worker.contractorName) {
                      <div class="contractor-line">
                        <mat-icon class="inline-icon">business</mat-icon>
                        <span>Contractor: {{ worker.contractorName }}</span>
                      </div>
                    }
                  </div>
                </td>
              </ng-container>

              <!-- 5. Farm Assignment Period -->
              <ng-container matColumnDef="assignment">
                <th mat-header-cell *matHeaderCellDef>Farm Assignment</th>
                <td mat-cell *matCellDef="let worker">
                  <div class="assignment-cell">
                    <span class="dates-range">
                      {{ worker.assignedFrom }} &rarr; {{ worker.assignedTo || "Ongoing" }}
                    </span>
                  </div>
                </td>
              </ng-container>

              <!-- 6. Eligibility Status Pill -->
              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef class="status-col">Eligibility</th>
                <td mat-cell *matCellDef="let worker" class="status-col">
                  @if (isAlreadyAdded(worker.workerId)) {
                    <span class="already-added-pill" matTooltip="Already present in today's attendance grid">
                      Already Added
                    </span>
                  } @else {
                    <span class="eligible-badge" matTooltip="Verified active assignment on {{ data.attendanceDate }}">
                      <mat-icon class="inline-icon">check_circle</mat-icon>
                      Eligible
                    </span>
                  }
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
              <tr
                mat-row
                *matRowDef="let row; columns: displayedColumns"
                [class.row-already-added]="isAlreadyAdded(row.workerId)"
                [class.row-selected]="selectedIds().has(row.workerId)"
              ></tr>
            </table>
          </div>
        }
      </mat-dialog-content>

      <mat-dialog-actions class="dialog-actions">
        <div class="selection-count">
          @if (selectedIds().size > 0) {
            <span class="count-badge">
              <mat-icon>check</mat-icon>
              {{ selectedIds().size }} worker(s) selected
            </span>
          } @else {
            <span class="helper-text">Select one or more workers to add to attendance</span>
          }
        </div>
        <div class="button-group">
          <button mat-button mat-dialog-close type="button">Cancel</button>
          <button
            mat-flat-button
            color="primary"
            type="button"
            [disabled]="selectedIds().size === 0"
            (click)="confirmSelection()"
          >
            <mat-icon>add</mat-icon>
            Add Selected ({{ selectedIds().size }})
          </button>
        </div>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .dialog-container {
        display: flex;
        flex-direction: column;
        max-height: 85vh;
        width: 100%;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 20px 24px 14px;
        border-bottom: 1px solid var(--mat-sys-outline-variant, #e0e0e0);

        .header-icon {
          width: 44px;
          height: 44px;
          border-radius: 10px;
          background: rgba(30, 142, 62, 0.12);
          color: #1b7a36;
          display: flex;
          align-items: center;
          justify-content: center;

          mat-icon {
            font-size: 24px;
            width: 24px;
            height: 24px;
          }
        }

        .header-title {
          h2 {
            margin: 0;
            font-size: 1.25rem;
            font-weight: 600;
          }

          .subtitle {
            margin: 2px 0 0;
            font-size: 0.85rem;
            color: var(--mat-sys-on-surface-variant, #666);
          }
        }
      }

      .dialog-content {
        padding: 16px 24px;
        overflow-y: auto;
        flex: 1;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .eligibility-info-banner {
        display: flex;
        align-items: center;
        gap: 8px;
        background: #f1f8e9;
        border: 1px solid #c5e1a5;
        border-radius: 6px;
        padding: 8px 12px;
        font-size: 0.82rem;
        color: #2e7d32;

        mat-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
          flex-shrink: 0;
        }
      }

      .filters-bar {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;

        .search-field {
          flex: 2;
          min-width: 200px;
        }

        .filter-dropdown {
          flex: 1;
          min-width: 150px;
        }
      }

      .loading-state,
      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 48px 24px;
        gap: 12px;
        color: var(--mat-sys-on-surface-variant, #666);

        mat-icon {
          font-size: 40px;
          width: 40px;
          height: 40px;
          opacity: 0.6;
        }

        h4 {
          margin: 0;
          font-size: 1.1rem;
        }

        p {
          margin: 0;
          font-size: 0.9rem;
        }
      }

      .table-container {
        max-height: 380px;
        overflow-y: auto;
        border: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
        border-radius: 8px;
      }

      .workers-table {
        width: 100%;

        .checkbox-col {
          width: 48px;
          text-align: center;
          padding: 0 8px;
        }

        .gender-col {
          width: 80px;
          text-align: center;
        }

        .status-col {
          width: 110px;
          text-align: center;
        }

        .worker-identity-cell {
          display: flex;
          flex-direction: column;
          gap: 2px;
          padding: 4px 0;

          .worker-name-line {
            display: flex;
            align-items: center;
            gap: 6px;
            flex-wrap: wrap;

            .display-name {
              font-size: 0.9rem;
              color: #212121;
            }

            .full-name {
              font-size: 0.8rem;
              color: #666;
            }
          }

          .mobile-line {
            display: flex;
            align-items: center;
            gap: 4px;
            font-size: 0.78rem;
            color: #757575;

            .inline-icon {
              font-size: 12px;
              width: 12px;
              height: 12px;
            }
          }
        }

        .gender-pill {
          display: inline-block;
          padding: 2px 8px;
          border-radius: 12px;
          font-size: 0.75rem;
          font-weight: 500;
          background: #f0f0f0;
          color: #424242;

          &[data-gender="MALE"] {
            background: #e3f2fd;
            color: #1565c0;
          }

          &[data-gender="FEMALE"] {
            background: #fce4ec;
            color: #c2185b;
          }
        }

        .category-meta-cell {
          display: flex;
          flex-direction: column;
          gap: 4px;

          .tags-row {
            display: flex;
            gap: 4px;
            flex-wrap: wrap;

            .badge {
              font-size: 0.72rem;
              padding: 2px 6px;
              border-radius: 4px;
              font-weight: 500;
            }

            .category-badge {
              background: #e8f5e9;
              color: #2e7d32;
            }

            .type-badge {
              background: #e3f2fd;
              color: #1565c0;
            }
          }

          .contractor-line {
            display: flex;
            align-items: center;
            gap: 4px;
            font-size: 0.75rem;
            color: #7b1fa2;

            .inline-icon {
              font-size: 12px;
              width: 12px;
              height: 12px;
            }
          }
        }

        .assignment-cell {
          font-size: 0.8rem;
          color: #555;
        }

        .eligible-badge {
          display: inline-flex;
          align-items: center;
          gap: 3px;
          padding: 2px 8px;
          border-radius: 10px;
          font-size: 0.72rem;
          font-weight: 600;
          background: #e8f5e9;
          color: #1b7a36;

          .inline-icon {
            font-size: 13px;
            width: 13px;
            height: 13px;
          }
        }

        .already-added-pill {
          display: inline-block;
          padding: 2px 8px;
          border-radius: 10px;
          font-size: 0.72rem;
          font-weight: 600;
          background: #f5f5f5;
          color: #9e9e9e;
          border: 1px solid #e0e0e0;
        }

        .row-already-added {
          opacity: 0.6;
          background: rgba(0, 0, 0, 0.02);
        }

        .row-selected {
          background: rgba(30, 142, 62, 0.04);
        }
      }

      .dialog-actions {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 12px 24px 16px;
        border-top: 1px solid var(--mat-sys-outline-variant, #e0e0e0);

        .selection-count {
          font-size: 0.88rem;

          .count-badge {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            font-weight: 600;
            color: #1b7a36;

            mat-icon {
              font-size: 16px;
              width: 16px;
              height: 16px;
            }
          }

          .helper-text {
            color: var(--mat-sys-on-surface-variant, #757575);
          }
        }

        .button-group {
          display: flex;
          gap: 8px;
        }
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceLoadWorkersDialogComponent implements OnInit {
  readonly data = inject<AttendanceLoadWorkersDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AttendanceLoadWorkersDialogComponent>);
  private readonly attendanceService = inject(AttendanceService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly displayedColumns: readonly string[] = [
    "select",
    "worker",
    "gender",
    "category",
    "assignment",
    "status",
  ];

  readonly allEligibleWorkers = signal<readonly AttendanceEligibleWorker[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly alreadyAddedSet = computed(
    () => new Set(this.data.alreadyAddedWorkerIds || []),
  );

  readonly searchQuery = signal<string>("");
  readonly selectedCategory = signal<string>("all");
  readonly selectedEmploymentType = signal<string>("all");

  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    category: ["all"],
    employmentType: ["all"],
  });

  readonly hasActiveFilters = computed(() => {
    return (
      this.searchQuery().length > 0 ||
      this.selectedCategory() !== "all" ||
      this.selectedEmploymentType() !== "all"
    );
  });

  readonly uniqueCategories = computed(() => {
    const set = new Set<string>();
    for (const w of this.allEligibleWorkers()) {
      set.add(w.laborCategoryName || "General");
    }
    return Array.from(set).sort();
  });

  readonly uniqueEmploymentTypes = computed(() => {
    const set = new Set<string>();
    for (const w of this.allEligibleWorkers()) {
      if (w.employmentType) {
        set.add(w.employmentType);
      }
    }
    return Array.from(set).sort();
  });

  readonly filteredWorkers = computed(() => {
    const list = this.allEligibleWorkers();
    const search = this.searchQuery().toLowerCase().trim();
    const cat = this.selectedCategory();
    const empType = this.selectedEmploymentType();

    return list.filter((w) => {
      // Labor category filter
      if (cat !== "all") {
        const workerCategory = (w.laborCategoryName || "General").trim();
        if (workerCategory.toLowerCase() !== cat.toLowerCase()) {
          return false;
        }
      }

      // Employment type filter
      if (empType !== "all") {
        const workerEmpType = (w.employmentType || "").trim();
        if (workerEmpType.toUpperCase() !== empType.toUpperCase()) {
          return false;
        }
      }

      // Text search filter (matches display name, first/last name, mobile, contractor, category, employment type)
      if (search) {
        const nameMatch = (w.displayName || "").toLowerCase().includes(search);
        const firstNameMatch = (w.firstName || "").toLowerCase().includes(search);
        const lastNameMatch = (w.lastName || "").toLowerCase().includes(search);
        const fullName = `${w.firstName || ""} ${w.lastName || ""}`.toLowerCase();
        const fullNameMatch = fullName.includes(search);
        const mobileMatch = w.mobileNumber ? w.mobileNumber.toLowerCase().includes(search) : false;
        const contractorMatch = w.contractorName ? w.contractorName.toLowerCase().includes(search) : false;
        const categoryMatch = (w.laborCategoryName || "General").toLowerCase().includes(search);
        const typeMatch = (w.employmentType || "").toLowerCase().includes(search);

        return (
          nameMatch ||
          firstNameMatch ||
          lastNameMatch ||
          fullNameMatch ||
          mobileMatch ||
          contractorMatch ||
          categoryMatch ||
          typeMatch
        );
      }

      return true;
    });
  });

  readonly selectableWorkers = computed(() => {
    return this.filteredWorkers().filter(
      (w) => !this.alreadyAddedSet().has(w.workerId),
    );
  });

  readonly isAllSelectableChecked = computed(() => {
    const selectable = this.selectableWorkers();
    if (selectable.length === 0) return false;
    const currentSelected = this.selectedIds();
    return selectable.every((w) => currentSelected.has(w.workerId));
  });

  readonly isSomeSelectableChecked = computed(() => {
    const selectable = this.selectableWorkers();
    if (selectable.length === 0) return false;
    const currentSelected = this.selectedIds();
    const checkedCount = selectable.filter((w) => currentSelected.has(w.workerId)).length;
    return checkedCount > 0 && checkedCount < selectable.length;
  });

  ngOnInit(): void {
    this.loadWorkers();

    this.filterForm.controls.search.valueChanges
      .pipe(
        debounceTime(150),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => {
        this.searchQuery.set(term?.trim() || "");
      });

    this.filterForm.controls.category.valueChanges
      .pipe(
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((category) => {
        this.selectedCategory.set(category || "all");
      });

    this.filterForm.controls.employmentType.valueChanges
      .pipe(
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((empType) => {
        this.selectedEmploymentType.set(empType || "all");
      });
  }

  clearSearch(): void {
    this.filterForm.controls.search.setValue("");
  }

  resetFilters(): void {
    this.filterForm.setValue({
      search: "",
      category: "all",
      employmentType: "all",
    });
  }

  formatEmploymentType(type: string): string {
    switch (type?.toUpperCase()) {
      case "PERMANENT":
        return "Permanent";
      case "DAILY_WAGE":
        return "Daily Wage";
      case "SEASONAL":
        return "Seasonal";
      case "CONTRACT":
        return "Contract";
      default:
        return type || "Unknown";
    }
  }

  loadWorkers(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .getEligibleWorkers(this.data.farmId, this.data.attendanceDate, null, 1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          const allItems = [...(res.items || [])];
          if (res.totalCount > 100) {
            const totalPages = Math.ceil(res.totalCount / 100);
            const remainingRequests = [];
            for (let p = 2; p <= totalPages; p++) {
              remainingRequests.push(
                this.attendanceService.getEligibleWorkers(
                  this.data.farmId,
                  this.data.attendanceDate,
                  null,
                  p,
                  100,
                ),
              );
            }
            forkJoin(remainingRequests)
              .pipe(
                finalize(() => this.isLoading.set(false)),
                takeUntilDestroyed(this.destroyRef),
              )
              .subscribe({
                next: (pages) => {
                  for (const pageRes of pages) {
                    allItems.push(...(pageRes.items || []));
                  }
                  this.allEligibleWorkers.set(allItems);
                },
                error: (err) => {
                  this.allEligibleWorkers.set(allItems);
                  this.errorMessage.set(
                    getApiErrorMessage(err, "Failed to load all eligible workers."),
                  );
                },
              });
          } else {
            this.isLoading.set(false);
            this.allEligibleWorkers.set(allItems);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(getApiErrorMessage(err, "Failed to load eligible workers."));
        },
      });
  }

  isAlreadyAdded(workerId: string): boolean {
    return this.alreadyAddedSet().has(workerId);
  }

  toggleWorker(workerId: string, checked: boolean): void {
    const set = new Set(this.selectedIds());
    if (checked) {
      set.add(workerId);
    } else {
      set.delete(workerId);
    }
    this.selectedIds.set(set);
  }

  toggleSelectAll(checked: boolean): void {
    const set = new Set(this.selectedIds());
    const selectable = this.selectableWorkers();

    if (checked) {
      for (const w of selectable) {
        set.add(w.workerId);
      }
    } else {
      for (const w of selectable) {
        set.delete(w.workerId);
      }
    }
    this.selectedIds.set(set);
  }

  confirmSelection(): void {
    const selected = this.allEligibleWorkers().filter((w) =>
      this.selectedIds().has(w.workerId),
    );
    this.dialogRef.close(selected);
  }
}
