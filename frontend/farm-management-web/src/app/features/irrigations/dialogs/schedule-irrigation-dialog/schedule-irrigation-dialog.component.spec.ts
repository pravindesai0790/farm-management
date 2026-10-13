import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ReactiveFormsModule } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import {
  ScheduleIrrigationDialogComponent,
  ScheduleIrrigationDialogData,
} from "./schedule-irrigation-dialog.component";

describe("ScheduleIrrigationDialogComponent", () => {
  let component: ScheduleIrrigationDialogComponent;
  let fixture: ComponentFixture<ScheduleIrrigationDialogComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<ScheduleIrrigationDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultData: ScheduleIrrigationDialogData = {
    irrigationId: "irr-1",
    farmName: "Green Valley Farm",
    farmAreaName: "North Block",
    currentScheduledAt: "2026-10-25T14:00:00Z",
    isReschedule: false,
    concurrencyToken: "ct-100",
  };

  const setupTest = async (data: ScheduleIrrigationDialogData = defaultData) => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", ["schedule", "reschedule"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockIrrigationService.schedule.and.returnValue(of({ id: "irr-1", status: "Scheduled" } as any));
    mockIrrigationService.reschedule.and.returnValue(of({ id: "irr-1", status: "Scheduled" } as any));

    await TestBed.configureTestingModule({
      imports: [ScheduleIrrigationDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleIrrigationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe("Schedule Mode (isReschedule = false)", () => {
    beforeEach(async () => {
      await setupTest();
    });

    it("should initialize with initial date value", () => {
      expect(component).toBeTruthy();
      expect(component.form.valid).toBeTrue();
      expect(component.data.isReschedule).toBeFalse();
    });

    it("should close dialog with false on onCancel", () => {
      component.onCancel();
      expect(mockDialogRef.close).toHaveBeenCalledWith(false);
    });

    it("should call schedule with ISO timestamp and concurrencyToken on submit", () => {
      component.form.patchValue({ scheduledAt: "2026-10-26T10:30" });
      component.onSubmit();

      expect(mockIrrigationService.schedule).toHaveBeenCalledWith(
        "irr-1",
        jasmine.objectContaining({
          scheduledAt: jasmine.any(String),
          concurrencyToken: "ct-100",
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event scheduled successfully.",
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });

    it("should handle schedule API error gracefully", () => {
      mockIrrigationService.schedule.and.returnValue(
        throwError(() => ({ status: 400, error: { message: "Invalid schedule transition" } })),
      );

      component.form.patchValue({ scheduledAt: "2026-10-26T10:30" });
      component.onSubmit();

      expect(component.isSubmitting()).toBeFalse();
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Invalid schedule transition",
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockDialogRef.close).not.toHaveBeenCalled();
    });
  });

  describe("Reschedule Mode (isReschedule = true)", () => {
    beforeEach(async () => {
      await setupTest({ ...defaultData, isReschedule: true, concurrencyToken: "ct-200" });
    });

    it("should call reschedule on submit", () => {
      component.form.patchValue({ scheduledAt: "2026-10-28T09:00" });
      component.onSubmit();

      expect(mockIrrigationService.reschedule).toHaveBeenCalledWith(
        "irr-1",
        jasmine.objectContaining({
          scheduledAt: jasmine.any(String),
          concurrencyToken: "ct-200",
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event rescheduled successfully.",
        "Dismiss",
        jasmine.any(Object),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });

    it("should allow past scheduled date without client validation error", () => {
      component.form.patchValue({ scheduledAt: "2026-01-01T08:00" });
      expect(component.form.valid).toBeTrue();
      component.onSubmit();
      expect(mockIrrigationService.reschedule).toHaveBeenCalled();
    });
  });
});
