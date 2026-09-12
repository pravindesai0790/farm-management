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
            {{ data.farmName }} &bull; {{ data.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <!-- Filter bar -->
        <form class="filters-bar" [formGroup]="filterForm">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search eligible workers</mat-label>
            <input
              matInput
              formControlName="search"
              placeholder="Name or mobile number"
            />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>

          <mat-form-field appearance="outline" class="category-field">
            <mat-label>Labor Category</mat-label>
            <mat-select formControlName="category">
              <mat-option value="all">All categories</mat-option>
              @for (cat of uniqueCategories(); track cat) {
                <mat-option [value]="cat">{{ cat }}</mat-option>
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
            <h4>No eligible workers found</h4>
            <p>
              No active farm assignments covering {{ data.attendanceDate }} match your filter.
            </p>
          </div>
        } @else {
          <div class="table-container">
            <table mat-table [dataSource]="filteredWorkers()" class="workers-table">
              <!-- Selection Column -->
              <ng-container matColumnDef="select">
                <th mat-header-cell *matHeaderCellDef class="checkbox-col">
                  <mat-checkbox
                    [checked]="isAllSelectableChecked()"
                    [indeterminate]="isSomeSelectableChecked()"
                    [disabled]="selectableWorkers().length === 0"
                    (change)="toggleSelectAll($event.checked)"
                    aria-label="Select all available workers"
                  />
                </th>
                <td mat-cell *matCellDef="let worker" class="checkbox-col">
                  @if (isAlreadyAdded(worker.workerId)) {
                    <span
                      class="already-added-pill"
                      matTooltip="Already added to today's attendance"
                    >
                      Added
                    </span>
                  } @else {
                    <mat-checkbox
                      [checked]="selectedIds().has(worker.workerId)"
                      (change)="toggleWorker(worker.workerId, $event.checked)"
                      aria-label="Select worker"
                    />
                  }
                </td>
              </ng-container>

              <!-- Worker Info -->
              <ng-container matColumnDef="displayName">
                <th mat-header-cell *matHeaderCellDef>Worker Name</th>
                <td mat-cell *matCellDef="let worker">
                  <div class="worker-name-cell">
                    <strong>{{ worker.displayName }}</strong>
                    @if (worker.mobileNumber) {
                      <small class="muted-text">{{ worker.mobileNumber }}</small>
                    }
                  </div>
                </td>
              </ng-container>

              <!-- Gender -->
              <ng-container matColumnDef="gender">
                <th mat-header-cell *matHeaderCellDef>Gender</th>
                <td mat-cell *matCellDef="let worker">
                  <span class="gender-pill" [attr.data-gender]="worker.gender">
                    {{ worker.gender }}
                  </span>
                </td>
              </ng-container>

              <!-- Category -->
              <ng-container matColumnDef="category">
                <th mat-header-cell *matHeaderCellDef>Category</th>
                <td mat-cell *matCellDef="let worker">
                  {{ worker.laborCategoryName || "General" }}
                  @if (worker.contractorName) {
                    <br /><small class="muted-text">Via {{ worker.contractorName }}</small>
                  }
                </td>
              </ng-container>

              <!-- Assignment Period -->
              <ng-container matColumnDef="assignment">
                <th mat-header-cell *matHeaderCellDef>Assigned Period</th>
                <td mat-cell *matCellDef="let worker">
                  <small>{{ worker.assignedFrom }} to {{ worker.assignedTo || "Present" }}</small>
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
            <span>{{ selectedIds().size }} worker(s) selected</span>
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
        max-width: 820px;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 20px 24px 12px;
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
      }

      .filters-bar {
        display: flex;
        gap: 12px;
        margin-bottom: 12px;

        .search-field {
          flex: 2;
        }

        .category-field {
          flex: 1;
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
        max-height: 400px;
        overflow-y: auto;
        border: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
        border-radius: 8px;
      }

      .workers-table {
        width: 100%;

        .checkbox-col {
          width: 52px;
          text-align: center;
          padding: 0 8px;
        }

        .worker-name-cell {
          display: flex;
          flex-direction: column;

          .muted-text {
            font-size: 0.8rem;
            color: var(--mat-sys-on-surface-variant, #757575);
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

        .already-added-pill {
          display: inline-block;
          padding: 2px 6px;
          border-radius: 4px;
          font-size: 0.7rem;
          font-weight: 600;
          background: #e8f5e9;
          color: #2e7d32;
        }

        .row-already-added {
          opacity: 0.65;
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
          font-size: 0.9rem;
          font-weight: 500;
          color: #1b7a36;
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
    "displayName",
    "gender",
    "category",
    "assignment",
  ];

  readonly allEligibleWorkers = signal<readonly AttendanceEligibleWorker[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly alreadyAddedSet = computed(
    () => new Set(this.data.alreadyAddedWorkerIds || []),
  );

  readonly filterForm = this.formBuilder.nonNullable.group({
    search: [""],
    category: ["all"],
  });

  readonly uniqueCategories = computed(() => {
    const set = new Set<string>();
    for (const w of this.allEligibleWorkers()) {
      if (w.laborCategoryName) {
        set.add(w.laborCategoryName);
      }
    }
    return Array.from(set).sort();
  });

  readonly filteredWorkers = computed(() => {
    const list = this.allEligibleWorkers();
    const search = this.filterForm.controls.search.value.toLowerCase().trim();
    const cat = this.filterForm.controls.category.value;

    return list.filter((w) => {
      if (cat !== "all" && w.laborCategoryName !== cat) {
        return false;
      }
      if (search) {
        const nameMatch = w.displayName.toLowerCase().includes(search);
        const mobileMatch = w.mobileNumber ? w.mobileNumber.includes(search) : false;
        return nameMatch || mobileMatch;
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

    this.filterForm.valueChanges
      .pipe(debounceTime(200), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        // Trigger computed refresh automatically
      });
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
