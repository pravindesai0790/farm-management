import { ComponentFixture, TestBed } from "@angular/core/testing";
import { FormControl, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { Unit } from "../../../../core/farm-management/farm-management.models";
import {
  IrrigationDetailsResponse,
  IrrigationMethodDto,
} from "../../../../core/irrigations/irrigation.models";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import {
  CompleteIrrigationDialogComponent,
  CompleteIrrigationDialogData,
  completeWaterAndTimingValidator,
} from "./complete-irrigation-dialog.component";

describe("CompleteIrrigationDialogComponent", () => {
  let component: CompleteIrrigationDialogComponent;
  let fixture: ComponentFixture<CompleteIrrigationDialogComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<CompleteIrrigationDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleMethods: IrrigationMethodDto[] = [
    {
      id: "method-1",
      organizationId: null,
      code: "DRIP",
      name: "Drip Irrigation",
      description: null,
      displayOrder: 1,
      isSystem: true,
      isActive: true,
    },
    {
      id: "method-2",
      organizationId: null,
      code: "SPRINKLER",
      name: "Sprinkler Irrigation",
      description: null,
      displayOrder: 2,
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleUnits: Unit[] = [
    {
      id: "unit-l",
      code: "L",
      name: "Liters",
      symbol: "L",
      unitCategory: "Volume",
      isSystem: true,
      isActive: true,
    },
  ];

  const sampleIrrigation: IrrigationDetailsResponse = {
    id: "irr-1",
    organizationId: "org-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Block",
    plantationId: "plant-1",
    plantationName: "Wheat Field",
    cropCycleId: "cycle-1",
    cropCycleName: "Spring 2026",
    cropCycleStageId: "stage-1",
    cropCycleStageName: "Vegetative",
    irrigationMethodId: "method-1",
    irrigationMethodName: "Drip Irrigation",
    status: "Draft",
    statusName: "Draft",
    isOverdue: false,
    plannedAt: "2026-10-25T08:00:00Z",
    scheduledAt: null,
    actualStartedAt: null,
    actualEndedAt: null,
    actualDurationMinutes: null,
    plannedWaterQuantity: 1000,
    plannedWaterUnitId: "unit-l",
    plannedWaterUnitName: "Liters",
    plannedWaterUnitSymbol: "L",
    actualWaterQuantity: null,
    actualWaterUnitId: null,
    actualWaterUnitName: null,
    actualWaterUnitSymbol: null,
    cancellationReason: null,
    notes: "Planned watering",
    concurrencyToken: "ct-100",
    createdAt: "2026-10-12T00:00:00Z",
    createdBy: "user-1",
    updatedAt: null,
    updatedBy: null,
    completedAt: null,
  };

  const defaultData: CompleteIrrigationDialogData = {
    irrigation: sampleIrrigation,
  };

  const setupTest = async (data: CompleteIrrigationDialogData = defaultData) => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", [
      "getMethods",
      "complete",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", ["listUnits"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockIrrigationService.getMethods.and.returnValue(of(sampleMethods));
    mockFarmService.listUnits.and.returnValue(of(sampleUnits));
    mockIrrigationService.complete.and.returnValue(of({ id: "irr-1", status: "Completed" } as any));

    await TestBed.configureTestingModule({
      imports: [CompleteIrrigationDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CompleteIrrigationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  };

  describe("completeWaterAndTimingValidator", () => {
    it("should flag waterUnitRequired if quantity is present without unit", () => {
      const form = new FormGroup({
        actualWaterQuantity: new FormControl(500),
        actualWaterUnitId: new FormControl(""),
        actualStartedAt: new FormControl(""),
        actualEndedAt: new FormControl(""),
      });
      const errors = completeWaterAndTimingValidator(form);
      expect(errors).toEqual({ waterUnitRequired: true });
    });

    it("should flag waterQuantityRequired if unit is present without quantity", () => {
      const form = new FormGroup({
        actualWaterQuantity: new FormControl(null),
        actualWaterUnitId: new FormControl("unit-l"),
        actualStartedAt: new FormControl(""),
        actualEndedAt: new FormControl(""),
      });
      const errors = completeWaterAndTimingValidator(form);
      expect(errors).toEqual({ waterQuantityRequired: true });
    });

    it("should flag endBeforeStart when end is earlier than start", () => {
      const form = new FormGroup({
        actualWaterQuantity: new FormControl(null),
        actualWaterUnitId: new FormControl(""),
        actualStartedAt: new FormControl("2026-10-25T10:00"),
        actualEndedAt: new FormControl("2026-10-25T09:00"),
      });
      const errors = completeWaterAndTimingValidator(form);
      expect(errors).toEqual({ endBeforeStart: true });
    });

    it("should return null for valid timing and volume", () => {
      const form = new FormGroup({
        actualWaterQuantity: new FormControl(500),
        actualWaterUnitId: new FormControl("unit-l"),
        actualStartedAt: new FormControl("2026-10-25T09:00"),
        actualEndedAt: new FormControl("2026-10-25T10:00"),
      });
      const errors = completeWaterAndTimingValidator(form);
      expect(errors).toBeNull();
    });
  });

  describe("Component Initialization & Completion", () => {
    beforeEach(async () => {
      await setupTest();
    });

    it("should load methods and units on init and pre-select method", () => {
      expect(component).toBeTruthy();
      expect(mockIrrigationService.getMethods).toHaveBeenCalledWith(true);
      expect(mockFarmService.listUnits).toHaveBeenCalledWith("Volume");
      expect(component.methods().length).toBe(2);
      expect(component.volumeUnits().length).toBe(1);
      expect(component.form.value.irrigationMethodId).toBe("method-1");
    });

    it("should require irrigationMethodId", () => {
      component.form.patchValue({ irrigationMethodId: "" });
      expect(component.form.valid).toBeFalse();
      expect(component.form.controls.irrigationMethodId.hasError("required")).toBeTrue();
    });

    it("should close dialog with false on onCancel", () => {
      component.onCancel();
      expect(mockDialogRef.close).toHaveBeenCalledWith(false);
    });

    it("should submit complete payload with manual duration and concurrency token", () => {
      component.form.patchValue({
        irrigationMethodId: "method-1",
        actualStartedAt: "2026-10-25T08:00",
        actualEndedAt: "2026-10-25T09:30",
        actualDurationMinutes: 90,
        actualWaterQuantity: 1100,
        actualWaterUnitId: "unit-l",
        notes: "Completed smoothly",
      });

      component.onSubmit();

      expect(mockIrrigationService.complete).toHaveBeenCalledWith(
        "irr-1",
        jasmine.objectContaining({
          irrigationMethodId: "method-1",
          actualDurationMinutes: 90,
          actualWaterQuantity: 1100,
          actualWaterUnitId: "unit-l",
          notes: "Completed smoothly",
          concurrencyToken: "ct-100",
        }),
      );
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        "Irrigation event completed successfully.",
        "OK",
        jasmine.any(Object),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });

    it("should allow direct completion from Scheduled status without prior start step", () => {
      (component as any).data = { irrigation: { ...sampleIrrigation, status: "Scheduled" as any } };
      component.form.patchValue({ irrigationMethodId: "method-1" });

      component.onSubmit();

      expect(mockIrrigationService.complete).toHaveBeenCalledWith(
        "irr-1",
        jasmine.objectContaining({
          irrigationMethodId: "method-1",
          concurrencyToken: "ct-100",
        }),
      );
      expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    });

    it("should handle complete API error gracefully", () => {
      mockIrrigationService.complete.and.returnValue(
        throwError(() => ({ status: 400, error: { message: "Method is inactive" } })),
      );

      component.onSubmit();

      expect(component.isSubmitting()).toBeFalse();
      expect(component.errorMessage()).toBe("Method is inactive");
      expect(mockSnackBar.open).toHaveBeenCalledWith("Method is inactive", "Dismiss", jasmine.any(Object));
      expect(mockDialogRef.close).not.toHaveBeenCalled();
    });
  });
});
