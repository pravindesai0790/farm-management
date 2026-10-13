import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ReactiveFormsModule } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import {
  CancelIrrigationDialogComponent,
  CancelIrrigationDialogData,
} from "./cancel-irrigation-dialog.component";

describe("CancelIrrigationDialogComponent", () => {
  let component: CancelIrrigationDialogComponent;
  let fixture: ComponentFixture<CancelIrrigationDialogComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CancelIrrigationDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultData: CancelIrrigationDialogData = {
    irrigationId: "irr-1",
    farmName: "Green Valley Farm",
    farmAreaName: "North Block",
    currentStatus: "Scheduled",
    concurrencyToken: "ct-100",
  };

  beforeEach(async () => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", ["cancel"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockIrrigationService.cancel.and.returnValue(of({ id: "irr-1", status: "Cancelled" } as any));

    await TestBed.configureTestingModule({
      imports: [CancelIrrigationDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: defaultData },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CancelIrrigationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should initialize with empty reason and invalid form", () => {
    expect(component).toBeTruthy();
    expect(component.form.valid).toBeFalse();
    expect(component.form.controls.reason.hasError("required")).toBeTrue();
  });

  it("should close dialog with false on onCancel", () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(false);
  });

  it("should reject whitespace-only reason", () => {
    component.form.patchValue({ reason: "   " });
    component.onSubmit();
    expect(component.form.controls.reason.hasError("required")).toBeTrue();
    expect(mockIrrigationService.cancel).not.toHaveBeenCalled();
  });

  it("should submit trimmed cancellation reason with concurrencyToken", () => {
    component.form.patchValue({ reason: "Heavy rain forecasted" });
    component.onSubmit();

    expect(mockIrrigationService.cancel).toHaveBeenCalledWith("irr-1", {
      cancellationReason: "Heavy rain forecasted",
      concurrencyToken: "ct-100",
    });
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Irrigation event has been cancelled.",
      "Dismiss",
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("should handle API cancellation errors gracefully", () => {
    mockIrrigationService.cancel.and.returnValue(
      throwError(() => ({ status: 400, error: { message: "Cannot cancel completed event" } })),
    );

    component.form.patchValue({ reason: "Equipment failure" });
    component.onSubmit();

    expect(component.isSubmitting()).toBeFalse();
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Cannot cancel completed event",
      "Dismiss",
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });
});
