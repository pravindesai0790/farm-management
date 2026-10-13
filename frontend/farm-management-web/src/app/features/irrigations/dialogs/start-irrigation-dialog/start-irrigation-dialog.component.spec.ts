import { ComponentFixture, TestBed } from "@angular/core/testing";
import { ReactiveFormsModule } from "@angular/forms";
import { provideNativeDateAdapter } from "@angular/material/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of, throwError } from "rxjs";

import { FarmManagementService } from "../../../../core/farm-management/farm-management.service";
import { IrrigationService } from "../../../../core/irrigations/irrigation.service";
import {
  StartIrrigationDialogComponent,
  StartIrrigationDialogData,
} from "./start-irrigation-dialog.component";

describe("StartIrrigationDialogComponent", () => {
  let component: StartIrrigationDialogComponent;
  let fixture: ComponentFixture<StartIrrigationDialogComponent>;

  let mockIrrigationService: jasmine.SpyObj<IrrigationService>;
  let mockFarmService: jasmine.SpyObj<FarmManagementService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<StartIrrigationDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultData: StartIrrigationDialogData = {
    irrigationId: "irr-1",
    farmId: "farm-1",
    farmName: "Green Valley Farm",
    farmAreaId: "area-1",
    farmAreaName: "North Block",
    plantationId: null,
    cropCycleId: null,
    cropCycleStageId: null,
    concurrencyToken: "ct-100",
  };

  beforeEach(async () => {
    mockIrrigationService = jasmine.createSpyObj("IrrigationService", [
      "start",
      "getCropCycleStages",
    ]);
    mockFarmService = jasmine.createSpyObj("FarmManagementService", [
      "listFarmAreas",
      "listPlantations",
      "listCycles",
    ]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockIrrigationService.start.and.returnValue(of({ id: "irr-1", status: "InProgress" } as any));
    mockFarmService.listFarmAreas.and.returnValue(
      of({ items: [{ id: "area-1", name: "North Block" } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listPlantations.and.returnValue(
      of({ items: [{ id: "plant-1", plantationName: "Wheat Field", cropName: "Wheat" } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockFarmService.listCycles.and.returnValue(
      of({ items: [{ id: "cycle-1", cycleName: "Spring 2026" } as any], totalCount: 1, page: 1, pageSize: 100, totalPages: 1 }),
    );
    mockIrrigationService.getCropCycleStages.and.returnValue(
      of([{ id: "stage-1", stageName: "Vegetative" } as any]),
    );

    await TestBed.configureTestingModule({
      imports: [StartIrrigationDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: IrrigationService, useValue: mockIrrigationService },
        { provide: FarmManagementService, useValue: mockFarmService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: MAT_DIALOG_DATA, useValue: defaultData },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StartIrrigationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create with pre-populated location and empty start time", () => {
    expect(component).toBeTruthy();
    expect(component.form.value.farmAreaId).toBe("area-1");
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
      farmAreaId: "area-1",
      plantationId: null,
      cropCycleId: null,
      cropCycleStageId: null,
      concurrencyToken: "ct-100",
    });
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      "Irrigation event started successfully.",
      "Dismiss",
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });

  it("should submit with updated location and ISO timestamp when specified", () => {
    component.form.patchValue({
      farmAreaId: "area-1",
      plantationId: "plant-1",
      cropCycleId: "cycle-1",
      cropCycleStageId: "stage-1",
      actualStartedAt: "2026-10-25T11:00",
    });
    component.onSubmit();

    expect(mockIrrigationService.start).toHaveBeenCalledWith(
      "irr-1",
      jasmine.objectContaining({
        farmAreaId: "area-1",
        plantationId: "plant-1",
        cropCycleId: "cycle-1",
        cropCycleStageId: "stage-1",
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
