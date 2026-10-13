import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ReactiveFormsModule } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import {
  StartIrrigationDialogComponent,
  StartIrrigationDialogData,
} from "./start-irrigation-dialog.component";

describe("StartIrrigationDialogComponent", () => {
  let component: StartIrrigationDialogComponent;
  let fixture: ComponentFixture<StartIrrigationDialogComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<StartIrrigationDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultData: StartIrrigationDialogData = {
    irrigationId: "irr-1",
    farmName: "Green Valley Farm",
    farmAreaName: "North Block",
    concurrencyToken: "ct-100",
  };

  beforeEach(async () => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", ["start"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockIrrigationService.start.and.returnValue(of({ id: "irr-1", status: "InProgress" } as any));

    await TestBed.configureTestingModule({
      imports: [StartIrrigationDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: defaultData },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StartIrrigationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create with empty optional start time", () => {
    expect(component).toBeTruthy();
    expect(component.form.value.actualStartedAt).toBeNull();
  });

  it("should close dialog with false on onCancel", () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(false);
  });

  it("should submit with null timestamp if input is left blank to use server default", () => {
    component.onSubmit();

    expect(mockIrrigationService.start).toHaveBeenCalledWith("irr-1", {
      actualStartedAt: null,
      concurrencyToken: "ct-100",
    });
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Irrigation event started successfully.",
      "Dismiss",
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("should submit with ISO timestamp when specified", () => {
    component.form.patchValue({ actualStartedAt: "2026-10-25T11:00" });
    component.onSubmit();

    expect(mockIrrigationService.start).toHaveBeenCalledWith(
      "irr-1",
      jasmine.objectContaining({
        actualStartedAt: jasmine.any(String),
        concurrencyToken: "ct-100",
      }),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("should handle API error gracefully", () => {
    mockIrrigationService.start.and.returnValue(
      throwError(() => ({ status: 400, error: { message: "Cannot start event" } })),
    );

    component.onSubmit();

    expect(component.isSubmitting()).toBeFalse();
    expect(mockSnackBar.open).toHaveBeenCalledWith("Cannot start event", "Dismiss", jasmine.any(Object));
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });
});
