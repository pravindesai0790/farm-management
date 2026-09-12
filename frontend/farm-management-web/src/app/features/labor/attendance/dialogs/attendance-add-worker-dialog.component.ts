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
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
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
import { MatTooltipModule } from "@angular/material/tooltip";
import { debounceTime, distinctUntilChanged, finalize, switchMap } from "rxjs";

import {
  ATTENDANCE_TYPE_OPTIONS,
  AttendanceEligibleWorker,
  AttendanceType,
  AttendanceWagePreviewResponse,
} from "../../../../core/labor/attendance.models";
import { AttendanceService } from "../../../../core/labor/attendance.service";
import { getApiErrorMessage } from "../../../../core/models/api-error.model";
import { ErrorAlertComponent } from "../../../../shared/components/error-alert/error-alert.component";

export interface AttendanceAddWorkerDialogData {
  readonly farmId: string;
  readonly farmName: string;
  readonly attendanceDate: string;
  readonly alreadyAddedWorkerIds: readonly string[];
  readonly initialSearch?: string | null;
}

export interface AttendanceAddWorkerResult {
  readonly worker: AttendanceEligibleWorker;
  readonly attendanceType: AttendanceType;
  readonly workingHours?: number | null;
  readonly notes?: string | null;
}

@Component({
  selector: "app-attendance-add-worker-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ErrorAlertComponent,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    ReactiveFormsModule,
  ],
  template: `
    <div class="dialog-container">
      <!-- Dialog Header -->
      <div class="dialog-header">
        <div class="header-icon">
          <mat-icon>person_search</mat-icon>
        </div>
        <div class="header-title">
          <h2 mat-dialog-title>Search &amp; Add Worker</h2>
          <p class="subtitle">
            {{ data.farmName }} &bull; Attendance Date: {{ data.attendanceDate }}
          </p>
        </div>
      </div>

      <mat-dialog-content class="dialog-content">
        @if (errorMessage()) {
          <app-error-alert [error]="errorMessage()!" />
        }

        <!-- Search Input Bar -->
        <div class="search-section">
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Search eligible workers</mat-label>
            <input
              matInput
              [formControl]="searchInput"
              placeholder="Search by worker name or mobile number"
              autocomplete="off"
            />
            @if (searchInput.value) {
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

          <div class="search-helper-meta">
            <span class="info-tag">
              <mat-icon>verified_user</mat-icon>
              Eligibility verified by backend active farm assignment
            </span>
          </div>
        </div>

        <div class="split-layout">
          <!-- Left Column: Search Results List -->
          <div class="results-column">
            <div class="column-header">
              <span class="column-title">
                Eligible Workers ({{ searchResults().length }})
              </span>
              @if (isSearching()) {
                <span class="searching-indicator">
                  <mat-spinner diameter="14" /> Searching…
                </span>
              }
            </div>

            @if (isSearching() && searchResults().length === 0) {
              <div class="loading-state">
                <mat-spinner diameter="28" />
                <span>Checking worker assignments…</span>
              </div>
            } @else if (searchResults().length === 0) {
              <div class="empty-results-state">
                <mat-icon>person_off</mat-icon>
                <p>No eligible workers found matching your search.</p>
                <small>Only workers with an active farm assignment on {{ data.attendanceDate }} are eligible.</small>
              </div>
            } @else {
              <div class="worker-results-list">
                @for (worker of searchResults(); track worker.workerId) {
                  <div
                    class="worker-result-card"
                    [class.card-selected]="selectedWorker()?.workerId === worker.workerId"
                    [class.card-disabled]="isAlreadyAdded(worker.workerId)"
                    (click)="onSelectWorker(worker)"
                  >
                    <div class="card-avatar" [attr.data-gender]="worker.gender">
                      <mat-icon>{{ worker.gender === 'FEMALE' ? 'woman' : 'man' }}</mat-icon>
                    </div>

                    <div class="card-details">
                      <div class="card-top-row">
                        <strong class="worker-name">{{ worker.displayName }}</strong>
                        @if (isAlreadyAdded(worker.workerId)) {
                          <span class="already-added-pill">Already Added</span>
                        } @else {
                          <span class="eligible-badge">Eligible</span>
                        }
                      </div>

                      @if (worker.firstName && worker.lastName && worker.displayName !== worker.firstName + ' ' + worker.lastName) {
                        <span class="full-name">{{ worker.firstName }} {{ worker.lastName }}</span>
                      }

                      <div class="worker-tags">
                        @if (worker.mobileNumber) {
                          <span class="tag mobile-tag">
                            <mat-icon>phone</mat-icon> {{ worker.mobileNumber }}
                          </span>
                        }
                        <span class="tag category-tag">
                          {{ worker.laborCategoryName || "General" }}
                        </span>
                        <span class="tag type-tag">
                          {{ worker.employmentType }}
                        </span>
                      </div>

                      @if (worker.contractorName) {
                        <span class="contractor-label">
                          Contractor: {{ worker.contractorName }}
                        </span>
                      }

                      <div class="assignment-meta">
                        <small>Assigned: {{ worker.assignedFrom }} to {{ worker.assignedTo || "Ongoing" }}</small>
                      </div>
                    </div>
                  </div>
                }
              </div>
            }
          </div>

          <!-- Right Column: Worker Identity & Configuration -->
          <div class="config-column">
            @if (selectedWorker(); as w) {
              <div class="selected-worker-panel">
                <!-- Rich Worker Identity Card -->
                <div class="worker-identity-card">
                  <div class="identity-header">
                    <div class="identity-avatar" [attr.data-gender]="w.gender">
                      <mat-icon>{{ w.gender === 'FEMALE' ? 'woman' : 'man' }}</mat-icon>
                    </div>
                    <div class="identity-title">
                      <h3>{{ w.displayName }}</h3>
                      @if (w.firstName && w.lastName) {
                        <p class="full-name">{{ w.firstName }} {{ w.lastName }}</p>
                      }
                      <span class="badge-status-active">
                        <mat-icon>check_circle</mat-icon> Active Farm Assignment
                      </span>
                    </div>
                  </div>

                  <div class="identity-grid">
                    <div class="grid-item">
                      <span class="label">Mobile Number</span>
                      <strong>{{ w.mobileNumber || "—" }}</strong>
                    </div>

                    <div class="grid-item">
                      <span class="label">Gender</span>
                      <span class="gender-pill" [attr.data-gender]="w.gender">{{ w.gender }}</span>
                    </div>

                    <div class="grid-item">
                      <span class="label">Labor Category</span>
                      <strong>{{ w.laborCategoryName || "General" }}</strong>
                    </div>

                    <div class="grid-item">
                      <span class="label">Employment Type</span>
                      <strong>{{ w.employmentType }}</strong>
                    </div>

                    @if (w.contractorName) {
                      <div class="grid-item full-width">
                        <span class="label">Contractor</span>
                        <span>{{ w.contractorName }}</span>
                      </div>
                    }

                    <div class="grid-item full-width">
                      <span class="label">Farm Assignment Period</span>
                      <span class="assignment-text">
                        {{ w.assignedFrom }} &rarr; {{ w.assignedTo || "Ongoing" }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Attendance Configuration -->
                <form [formGroup]="configForm" class="config-form">
                  <div class="form-title">
                    <mat-icon>tune</mat-icon>
                    <span>Attendance Settings</span>
                  </div>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Attendance Type</mat-label>
                    <mat-select formControlName="attendanceType">
                      @for (opt of attendanceTypeOptions; track opt.value) {
                        <mat-option [value]="opt.value">
                          {{ opt.label }} &mdash; {{ opt.description }}
                        </mat-option>
                      }
                    </mat-select>
                  </mat-form-field>

                  @if (configForm.controls.attendanceType.value === 'HOURLY') {
                    <mat-form-field appearance="outline" class="full-width">
                      <mat-label>Working Hours</mat-label>
                      <input
                        matInput
                        type="number"
                        step="0.25"
                        min="0.25"
                        max="24"
                        formControlName="workingHours"
                        placeholder="e.g. 6.0"
                      />
                      <span matSuffix>&nbsp;hours&nbsp;</span>
                      @if (
                        configForm.controls.workingHours.touched &&
                        configForm.controls.workingHours.invalid
                      ) {
                        <mat-error>Hours must be between 0.25 and 24.</mat-error>
                      }
                    </mat-form-field>
                  }

                  <!-- Live Wage Preview Box -->
                  @if (wagePreview(); as wp) {
                    <div class="wage-preview-box">
                      <div class="wage-preview-row">
                        <span class="label">Resolved Wage Rate:</span>
                        <strong>{{ wp.currencySymbol }}{{ wp.rate | number: '1.2-2' }}</strong>
                      </div>
                      <div class="wage-preview-row">
                        <span class="label">Calculated Earnings:</span>
                        <strong class="earnings-highlight">
                          {{ wp.currencySymbol }}{{ wp.calculatedAmount | number: '1.2-2' }}
                        </strong>
                      </div>
                      @if (wp.ineligibilityReason) {
                        <div class="wage-warning">
                          <mat-icon>warning</mat-icon> {{ wp.ineligibilityReason }}
                        </div>
                      }
                    </div>
                  } @else if (isLoadingPreview()) {
                    <div class="wage-preview-loading">
                      <mat-spinner diameter="16" />
                      <span>Fetching active wage rate…</span>
                    </div>
                  }

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Shift / Task Notes (optional)</mat-label>
                    <input
                      matInput
                      formControlName="notes"
                      placeholder="e.g. Morning irrigation shift"
                    />
                  </mat-form-field>
                </form>
              </div>
            } @else {
              <div class="no-selection-state">
                <mat-icon>touch_app</mat-icon>
                <h4>Select a worker from the search results</h4>
                <p>
                  Click on an eligible worker card to view their identity details and configure their attendance type before adding.
                </p>
              </div>
            }
          </div>
        </div>
      </mat-dialog-content>

      <!-- Action Buttons -->
      <mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close type="button">Cancel</button>
        <button
          mat-flat-button
          color="primary"
          type="button"
          [disabled]="!selectedWorker() || isAlreadyAdded(selectedWorker()?.workerId!) || configForm.invalid"
          (click)="confirmAdd()"
        >
          <mat-icon>add_circle</mat-icon>
          Add to Attendance
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .dialog-container {
        display: flex;
        flex-direction: column;
        width: 100%;
        max-width: 920px;
        max-height: 90vh;
      }

      .dialog-header {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 18px 24px 12px;
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
          flex-shrink: 0;

          mat-icon { font-size: 24px; width: 24px; height: 24px; }
        }

        .header-title {
          h2 { margin: 0; font-size: 1.25rem; font-weight: 600; }
          .subtitle { margin: 2px 0 0; font-size: 0.85rem; color: #666; }
        }
      }

      .dialog-content {
        padding: 16px 24px;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .search-section {
        .search-field {
          width: 100%;
          margin-bottom: -0.5em;
        }

        .search-helper-meta {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-top: 4px;

          .info-tag {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            font-size: 0.78rem;
            color: #1b7a36;
            mat-icon { font-size: 14px; width: 14px; height: 14px; }
          }
        }
      }

      .split-layout {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 16px;
        min-height: 420px;
      }

      .results-column, .config-column {
        display: flex;
        flex-direction: column;
        border: 1px solid #e0e0e0;
        border-radius: 8px;
        overflow: hidden;
      }

      .results-column {
        background: #fafafa;

        .column-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 10px 14px;
          background: #f0f0f0;
          border-bottom: 1px solid #e0e0e0;

          .column-title {
            font-size: 0.85rem;
            font-weight: 600;
            color: #444;
          }

          .searching-indicator {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 0.78rem;
            color: #1b7a36;
          }
        }
      }

      .worker-results-list {
        overflow-y: auto;
        max-height: 380px;
        display: flex;
        flex-direction: column;
        gap: 6px;
        padding: 8px;
      }

      .worker-result-card {
        display: flex;
        gap: 10px;
        padding: 10px 12px;
        background: #ffffff;
        border: 1px solid #e0e0e0;
        border-radius: 6px;
        cursor: pointer;
        transition: all 0.15s ease;

        &:hover:not(.card-disabled) {
          border-color: #1b7a36;
          background: #f9fdfa;
        }

        &.card-selected {
          border-color: #1b7a36;
          background: rgba(30, 142, 62, 0.08);
          box-shadow: 0 0 0 1px #1b7a36;
        }

        &.card-disabled {
          opacity: 0.6;
          background: #f5f5f5;
          cursor: not-allowed;
        }

        .card-avatar {
          width: 36px;
          height: 36px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          flex-shrink: 0;
          background: #e3f2fd;
          color: #1565c0;

          &[data-gender="FEMALE"] {
            background: #fce4ec;
            color: #c2185b;
          }

          mat-icon { font-size: 20px; width: 20px; height: 20px; }
        }

        .card-details {
          flex: 1;
          display: flex;
          flex-direction: column;
          gap: 2px;

          .card-top-row {
            display: flex;
            justify-content: space-between;
            align-items: center;

            .worker-name {
              font-size: 0.9rem;
              color: #212121;
            }

            .eligible-badge {
              font-size: 0.68rem;
              font-weight: 600;
              padding: 1px 6px;
              border-radius: 10px;
              background: #e8f5e9;
              color: #1b7a36;
            }

            .already-added-pill {
              font-size: 0.68rem;
              font-weight: 600;
              padding: 1px 6px;
              border-radius: 10px;
              background: #e0e0e0;
              color: #555;
            }
          }

          .full-name {
            font-size: 0.78rem;
            color: #666;
          }

          .worker-tags {
            display: flex;
            gap: 4px;
            flex-wrap: wrap;
            margin-top: 2px;

            .tag {
              font-size: 0.72rem;
              padding: 1px 5px;
              border-radius: 4px;
              background: #f0f0f0;
              color: #555;

              &.mobile-tag {
                display: inline-flex;
                align-items: center;
                gap: 2px;
                mat-icon { font-size: 11px; width: 11px; height: 11px; }
              }

              &.category-tag {
                background: #e8f5e9;
                color: #2e7d32;
              }

              &.type-tag {
                background: #e3f2fd;
                color: #1565c0;
              }
            }
          }

          .contractor-label {
            font-size: 0.72rem;
            color: #7b1fa2;
            font-style: italic;
          }

          .assignment-meta {
            font-size: 0.7rem;
            color: #888;
            margin-top: 2px;
          }
        }
      }

      .config-column {
        background: #ffffff;
        padding: 14px;
        overflow-y: auto;
      }

      .selected-worker-panel {
        display: flex;
        flex-direction: column;
        gap: 14px;
      }

      .worker-identity-card {
        border: 1px solid #e0e0e0;
        border-radius: 8px;
        padding: 12px;
        background: #f8fbf9;

        .identity-header {
          display: flex;
          align-items: center;
          gap: 10px;
          padding-bottom: 10px;
          border-bottom: 1px solid #e0e0e0;

          .identity-avatar {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #e3f2fd;
            color: #1565c0;

            &[data-gender="FEMALE"] {
              background: #fce4ec;
              color: #c2185b;
            }

            mat-icon { font-size: 22px; width: 22px; height: 22px; }
          }

          .identity-title {
            h3 { margin: 0; font-size: 1.05rem; font-weight: 600; color: #1b7a36; }
            .full-name { margin: 1px 0; font-size: 0.8rem; color: #666; }
            .badge-status-active {
              display: inline-flex;
              align-items: center;
              gap: 3px;
              font-size: 0.7rem;
              color: #1b7a36;
              font-weight: 500;
              mat-icon { font-size: 12px; width: 12px; height: 12px; }
            }
          }
        }

        .identity-grid {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 8px;
          margin-top: 10px;
          font-size: 0.82rem;

          .grid-item {
            display: flex;
            flex-direction: column;

            &.full-width {
              grid-column: span 2;
            }

            .label {
              font-size: 0.72rem;
              color: #888;
              text-transform: uppercase;
            }

            .gender-pill {
              display: inline-block;
              width: fit-content;
              padding: 1px 6px;
              border-radius: 10px;
              font-size: 0.75rem;
              font-weight: 500;
              background: #e3f2fd;
              color: #1565c0;

              &[data-gender="FEMALE"] {
                background: #fce4ec;
                color: #c2185b;
              }
            }

            .assignment-text {
              font-size: 0.78rem;
              color: #444;
            }
          }
        }
      }

      .config-form {
        display: flex;
        flex-direction: column;
        gap: 10px;

        .form-title {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 0.88rem;
          font-weight: 600;
          color: #333;

          mat-icon { font-size: 18px; width: 18px; height: 18px; color: #1b7a36; }
        }

        .full-width { width: 100%; margin-bottom: -1em; }
      }

      .wage-preview-box {
        padding: 10px 12px;
        background: #f1f8e9;
        border: 1px solid #dcedc8;
        border-radius: 6px;
        display: flex;
        flex-direction: column;
        gap: 4px;
        font-size: 0.82rem;

        .wage-preview-row {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .label { color: #555; }
          .earnings-highlight { color: #1b7a36; font-size: 0.95rem; }
        }

        .wage-warning {
          display: flex;
          align-items: center;
          gap: 4px;
          font-size: 0.75rem;
          color: #e65100;
          mat-icon { font-size: 14px; width: 14px; height: 14px; }
        }
      }

      .wage-preview-loading {
        display: flex;
        align-items: center;
        gap: 6px;
        font-size: 0.78rem;
        color: #666;
        padding: 6px 0;
      }

      .empty-results-state, .loading-state, .no-selection-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 36px 16px;
        text-align: center;
        color: #888;
        gap: 8px;

        mat-icon { font-size: 32px; width: 32px; height: 32px; opacity: 0.6; }
        p { margin: 0; font-size: 0.85rem; }
        small { font-size: 0.75rem; color: #999; }
        h4 { margin: 0; font-size: 0.95rem; font-weight: 600; color: #555; }
      }

      .dialog-actions {
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        padding: 12px 24px 16px;
        border-top: 1px solid var(--mat-sys-outline-variant, #e0e0e0);
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AttendanceAddWorkerDialogComponent implements OnInit {
  readonly data = inject<AttendanceAddWorkerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AttendanceAddWorkerDialogComponent>);
  private readonly attendanceService = inject(AttendanceService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly attendanceTypeOptions = ATTENDANCE_TYPE_OPTIONS;

  readonly searchInput = this.formBuilder.control(this.data.initialSearch || "");
  readonly searchResults = signal<readonly AttendanceEligibleWorker[]>([]);
  readonly selectedWorker = signal<AttendanceEligibleWorker | null>(null);

  readonly isSearching = signal(false);
  readonly isLoadingPreview = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly wagePreview = signal<AttendanceWagePreviewResponse | null>(null);

  readonly alreadyAddedSet = computed(
    () => new Set(this.data.alreadyAddedWorkerIds || []),
  );

  readonly configForm = this.formBuilder.nonNullable.group({
    attendanceType: ["FULL_DAY" as AttendanceType, Validators.required],
    workingHours: [null as number | null],
    notes: [""],
  });

  ngOnInit(): void {
    // Initial search or load
    this.executeSearch(this.searchInput.value?.trim() || "");

    // Debounced search input stream
    this.searchInput.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => {
        this.executeSearch(term?.trim() || "");
      });

    // Handle attendanceType changes for workingHours validation & wage preview
    this.configForm.controls.attendanceType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((type) => {
        if (type === "HOURLY") {
          this.configForm.controls.workingHours.setValidators([
            Validators.required,
            Validators.min(0.25),
            Validators.max(24),
          ]);
          if (!this.configForm.controls.workingHours.value) {
            this.configForm.controls.workingHours.setValue(8);
          }
        } else {
          this.configForm.controls.workingHours.clearValidators();
          this.configForm.controls.workingHours.setValue(null);
        }
        this.configForm.controls.workingHours.updateValueAndValidity();
        this.fetchWagePreview();
      });

    // Handle hours change for wage preview
    this.configForm.controls.workingHours.valueChanges
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        if (this.configForm.controls.attendanceType.value === "HOURLY") {
          this.fetchWagePreview();
        }
      });
  }

  clearSearch(): void {
    this.searchInput.setValue("");
  }

  executeSearch(term: string): void {
    this.isSearching.set(true);
    this.errorMessage.set(null);

    this.attendanceService
      .getEligibleWorkers(
        this.data.farmId,
        this.data.attendanceDate,
        term || null,
        1,
        100,
      )
      .pipe(
        finalize(() => this.isSearching.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.searchResults.set(res.items || []);

          // If a worker was previously selected, check if still in results or keep selected
          const current = this.selectedWorker();
          if (current && !this.searchResults().some((w) => w.workerId === current.workerId)) {
            // Keep current selected worker intact
          } else if (!current && res.items && res.items.length > 0) {
            // Do NOT auto-select immediately; let user pick
          }
        },
        error: (err) => {
          this.errorMessage.set(
            getApiErrorMessage(err, "Failed to search eligible workers."),
          );
        },
      });
  }

  isAlreadyAdded(workerId: string): boolean {
    return this.alreadyAddedSet().has(workerId);
  }

  // Clicking a worker selects them in the preview pane; does NOT add to attendance automatically!
  onSelectWorker(worker: AttendanceEligibleWorker): void {
    if (this.isAlreadyAdded(worker.workerId)) return;
    this.selectedWorker.set(worker);
    this.fetchWagePreview();
  }

  fetchWagePreview(): void {
    const worker = this.selectedWorker();
    if (!worker) {
      this.wagePreview.set(null);
      return;
    }

    const type = this.configForm.controls.attendanceType.value;
    const hours = this.configForm.controls.workingHours.value;

    this.isLoadingPreview.set(true);

    this.attendanceService
      .previewWage({
        workerId: worker.workerId,
        attendanceDate: this.data.attendanceDate,
        attendanceType: type,
        workingHours: type === "HOURLY" ? hours : null,
        farmId: this.data.farmId,
      })
      .pipe(
        finalize(() => this.isLoadingPreview.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (res) => {
          this.wagePreview.set(res);
        },
        error: (err) => {
          console.warn("Could not preview wage:", err);
          this.wagePreview.set(null);
        },
      });
  }

  // User intentionally clicks "Add to Attendance"
  confirmAdd(): void {
    const worker = this.selectedWorker();
    if (!worker || this.isAlreadyAdded(worker.workerId) || this.configForm.invalid) {
      return;
    }

    const result: AttendanceAddWorkerResult = {
      worker,
      attendanceType: this.configForm.controls.attendanceType.value,
      workingHours: this.configForm.controls.workingHours.value,
      notes: this.configForm.controls.notes.value?.trim() || null,
    };

    this.dialogRef.close(result);
  }
}
