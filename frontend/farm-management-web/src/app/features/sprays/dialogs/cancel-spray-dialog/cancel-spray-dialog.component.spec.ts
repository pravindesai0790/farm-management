import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { SprayService } from "../../../../core/sprays/spray.service";
import {
  CancelSprayDialogComponent,
  CancelSprayDialogData,
} from "./cancel-spray-dialog.component";

describe("CancelSprayDialogComponent", () => {
  let component: CancelSprayDialogComponent;
  let fixture: ComponentFixture<CancelSprayDialogComponent>;

  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CancelSprayDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleData: CancelSprayDialogData = {
    sprayId: "sp-100",
    referenceNumber: "SP-2026-0099",
    farmName: "Test Farm",
    status: "Scheduled",
  };

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", ["cancelSpray"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    await TestBed.configureTestingModule({
      imports: [CancelSprayDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: SprayService, useValue: mockSprayService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: sampleData },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CancelSprayDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("should initialize form with empty reason and invalid state", () => {
    expect(component.form.valid).toBeFalse();
    expect(component.form.get("reason")?.value).toBe("");
  });

  it("should validate that reason is required", () => {
    component.form.patchValue({ reason: "   " });
    component.onSubmit();
    expect(mockSprayService.cancelSpray).not.toHaveBeenCalled();
  });

  it("should call cancelSpray and close dialog with true on success", () => {
    mockSprayService.cancelSpray.and.returnValue(of(void 0));
    component.form.patchValue({ reason: "Unfavorable wind conditions" });

    component.onSubmit();

    expect(mockSprayService.cancelSpray).toHaveBeenCalledWith(
      "sp-100",
      "Unfavorable wind conditions",
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringContaining("cancelled"),
      jasmine.any(String),
      jasmine.any(Object),
    );
  });

  it("should handle error gracefully on cancelSpray failure", () => {
    mockSprayService.cancelSpray.and.returnValue(
      throwError(() => ({ message: "Server error" })),
    );
    component.form.patchValue({ reason: "Unfavorable wind conditions" });

    component.onSubmit();

    expect(mockDialogRef.close).not.toHaveBeenCalled();
    expect(component.isSubmitting()).toBeFalse();
    expect(mockSnackBar.open).toHaveBeenCalled();
  });

  it("should close dialog with false on cancel", () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(false);
  });
});
