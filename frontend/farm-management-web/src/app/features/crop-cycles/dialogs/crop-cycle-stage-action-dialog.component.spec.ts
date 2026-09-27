import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";
import { FarmManagementService } from "../../../core/farm-management/farm-management.service";
import {
  CropCycleStageActionDialogComponent,
  CropCycleStageActionDialogData,
} from "./crop-cycle-stage-action-dialog.component";

describe("CropCycleStageActionDialogComponent", () => {
  let component: CropCycleStageActionDialogComponent;
  let fixture: ComponentFixture<CropCycleStageActionDialogComponent>;
  let mockService: jasmine.SpyObj<FarmManagementService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CropCycleStageActionDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultDialogData: CropCycleStageActionDialogData = {
    actionMode: "complete",
    stageId: "stage-123",
    stageName: "Bud Break",
    sequenceNumber: 3,
    currentStatus: "IN_PROGRESS",
    actualStartDate: "2026-03-01",
    actualEndDate: null,
    nextStageName: "Shoot Growth",
    nextStageSequence: 4,
  };

  function setupTestBed(dialogData: CropCycleStageActionDialogData) {
    mockService = jasmine.createSpyObj("FarmManagementService", [
      "completeCycleStage",
      "skipCycleStage",
      "reopenCycleStage",
      "overrideCycleStage",
    ]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    TestBed.configureTestingModule({
      imports: [CropCycleStageActionDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: FarmManagementService, useValue: mockService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CropCycleStageActionDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  describe("Complete Mode", () => {
    beforeEach(() => {
      setupTestBed({ ...defaultDialogData, actionMode: "complete" });
    });

    it("should create component and initialize complete form", () => {
      expect(component).toBeTruthy();
      expect(component.form.valid).toBeTrue();
      expect(component.getActionButtonText()).toBe("Completion");
      expect(component.getButtonColor()).toBe("primary");
    });

    it("should submit complete action and close dialog on success", () => {
      mockService.completeCycleStage.and.returnValue(
        of({
          id: "stage-123",
          cropCycleId: "cycle-1",
          lifecycleTemplateStageId: "lt-1",
          stageName: "Bud Break",
          sequenceNumber: 3,
          expectedDurationDays: 10,
          plannedStartDate: "2026-03-01",
          plannedEndDate: "2026-03-10",
          actualStartDate: "2026-03-01",
          actualEndDate: "2026-03-11",
          status: "COMPLETED",
          notes: "Completed smoothly",
        }),
      );

      component.onSubmit();

      expect(mockService.completeCycleStage).toHaveBeenCalledWith("stage-123", jasmine.objectContaining({
        actualEndDate: jasmine.any(String),
      }));
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Stage "Bud Break" completed successfully.',
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith({ success: true, actionMode: "complete" });
    });
  });

  describe("Skip Mode", () => {
    beforeEach(() => {
      setupTestBed({ ...defaultDialogData, actionMode: "skip" });
    });

    it("should require a reason to skip", () => {
      expect(component.form.valid).toBeFalse();
      expect(component.form.get("reason")?.hasError("required")).toBeTrue();

      component.form.patchValue({ reason: "Unfavorable frost weather" });
      expect(component.form.valid).toBeTrue();
    });

    it("should submit skip action with reason and close dialog", () => {
      mockService.skipCycleStage.and.returnValue(
        of({
          id: "stage-123",
          cropCycleId: "cycle-1",
          lifecycleTemplateStageId: "lt-1",
          stageName: "Bud Break",
          sequenceNumber: 3,
          expectedDurationDays: 10,
          plannedStartDate: "2026-03-01",
          plannedEndDate: "2026-03-10",
          actualStartDate: "2026-03-01",
          actualEndDate: null,
          status: "SKIPPED",
          notes: null,
        }),
      );

      component.form.patchValue({ reason: "Skipped due to weather" });
      component.onSubmit();

      expect(mockService.skipCycleStage).toHaveBeenCalledWith("stage-123", {
        reason: "Skipped due to weather",
        skipDate: jasmine.any(String),
      });
      expect(mockDialogRef.close).toHaveBeenCalledWith({ success: true, actionMode: "skip" });
    });
  });

  describe("Reopen Mode", () => {
    beforeEach(() => {
      setupTestBed({ ...defaultDialogData, actionMode: "reopen", currentStatus: "COMPLETED" });
    });

    it("should require a reason to reopen", () => {
      expect(component.form.valid).toBeFalse();
      component.form.patchValue({ reason: "Pruning stage needs additional cut" });
      expect(component.form.valid).toBeTrue();
    });

    it("should display API error and keep dialog open on HTTP failure", () => {
      mockService.reopenCycleStage.and.returnValue(
        throwError(() => ({
          status: 409,
          error: { message: "Cannot reopen Stage 3 because prior stages are not yet completed." },
        })),
      );

      component.form.patchValue({ reason: "Correction required" });
      component.onSubmit();

      expect(mockService.reopenCycleStage).toHaveBeenCalled();
      expect(component.errorMessage()).toBe("Cannot reopen Stage 3 because prior stages are not yet completed.");
      expect(mockDialogRef.close).not.toHaveBeenCalled();
    });
  });

  describe("Override Mode", () => {
    beforeEach(() => {
      setupTestBed({ ...defaultDialogData, actionMode: "override" });
    });

    it("should validate targetStatus and require actualEndDate if status is COMPLETED", () => {
      component.form.patchValue({ targetStatus: "COMPLETED", actualEndDate: null, reason: "Manual adjustment" });
      fixture.detectChanges();
      expect(component.form.valid).toBeFalse();

      component.form.patchValue({ actualEndDate: new Date() });
      expect(component.form.valid).toBeTrue();
    });

    it("should reject end date earlier than start date", () => {
      component.form.patchValue({
        targetStatus: "IN_PROGRESS",
        actualStartDate: new Date("2026-05-10"),
        actualEndDate: new Date("2026-05-01"),
        reason: "Manual adjustment",
      });

      expect(component.form.hasError("beforeStartDate")).toBeTrue();
      expect(component.form.valid).toBeFalse();
    });
  });
});
