import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  CropCycleStagePlannedDatesDialogComponent,
  CropCycleStagePlannedDatesDialogData,
} from "./crop-cycle-stage-planned-dates-dialog.component";

describe("CropCycleStagePlannedDatesDialogComponent", () => {
  let component: CropCycleStagePlannedDatesDialogComponent;
  let fixture: ComponentFixture<CropCycleStagePlannedDatesDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CropCycleStagePlannedDatesDialogComponent>>;
  let mockService: jasmine.SpyObj<FarmManagementService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultDialogData: CropCycleStagePlannedDatesDialogData = {
    stageId: "stage-1",
    stageName: "Pruning",
    sequenceNumber: 2,
    currentStatus: "IN_PROGRESS",
    plannedStartDate: "2026-04-01",
    plannedEndDate: "2026-04-15",
    actualStartDate: "2026-04-01",
    actualEndDate: null,
    expectedDurationDays: 14,
    prevStageName: "Dormancy",
    prevStagePlannedStartDate: "2026-03-01",
    nextStageName: "Bud Break",
    nextStagePlannedEndDate: "2026-04-30",
  };

  function setupTestBed(dialogData: CropCycleStagePlannedDatesDialogData) {
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockService = jasmine.createSpyObj("FarmManagementService", ["updateCycleStagePlannedDates"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    TestBed.configureTestingModule({
      imports: [CropCycleStagePlannedDatesDialogComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNativeDateAdapter(),
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: FarmManagementService, useValue: mockService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CropCycleStagePlannedDatesDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    setupTestBed(defaultDialogData);
  });

  it("should create component and populate form with existing planned dates", () => {
    expect(component).toBeTruthy();
    expect(component.form.valid).toBeTrue();
    expect(component.calculatedDurationDays).toBe(14);
  });

  it("should validate that planned end date cannot be before planned start date", () => {
    component.form.patchValue({
      plannedStartDate: new Date("2026-04-15"),
      plannedEndDate: new Date("2026-04-10"),
    });

    expect(component.form.invalid).toBeTrue();
    expect(component.form.hasError("beforeStartDate")).toBeTrue();
  });

  it("should validate that planned start date cannot be before previous stage start date", () => {
    component.form.patchValue({
      plannedStartDate: new Date("2026-02-15"),
      plannedEndDate: new Date("2026-04-10"),
    });

    expect(component.form.invalid).toBeTrue();
    expect(component.form.hasError("beforePrevStageStart")).toBeTrue();
  });

  it("should submit updated planned dates successfully and close dialog", () => {
    mockService.updateCycleStagePlannedDates.and.returnValue(
      of({
        id: "stage-1",
        cropCycleId: "cycle-1",
        lifecycleTemplateStageId: "lts-2",
        stageName: "Pruning",
        sequenceNumber: 2,
        expectedDurationDays: 14,
        plannedStartDate: "2026-04-05",
        plannedEndDate: "2026-04-19",
        actualStartDate: "2026-04-01",
        actualEndDate: null,
        status: "IN_PROGRESS",
        notes: null,
      }),
    );

    component.form.patchValue({
      plannedStartDate: new Date("2026-04-05"),
      plannedEndDate: new Date("2026-04-19"),
    });

    component.onSubmit();

    expect(mockService.updateCycleStagePlannedDates).toHaveBeenCalledWith("stage-1", {
      plannedStartDate: "2026-04-05",
      plannedEndDate: "2026-04-19",
    });
    expect(mockSnackBar.open).toHaveBeenCalled();
    expect(mockDialogRef.close).toHaveBeenCalledWith({ success: true });
  });

  it("should display API error message when backend validation fails", () => {
    const errorResponse = {
      status: 400,
      error: {
        message: "Planned start date cannot be before plantation planting date.",
        errors: { plannedStartDate: ["Planned start date cannot be before plantation planting date."] },
      },
    };
    mockService.updateCycleStagePlannedDates.and.returnValue(throwError(() => errorResponse));

    component.onSubmit();

    expect(component.errorMessage()).toBe("Planned start date cannot be before plantation planting date.");
    expect(component.isSubmitting()).toBeFalse();
  });

  it("should close dialog without submitting when cancel is clicked", () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalled();
  });
});
