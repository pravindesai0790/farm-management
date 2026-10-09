import { ComponentFixture, TestBed } from "@angular/core/testing";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { provideNativeDateAdapter } from "@angular/material/core";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { of } from "rxjs";

import { SprayService } from "../../../../core/sprays/spray.service";
import {
  ScheduleSprayDialogComponent,
  ScheduleSprayDialogData,
} from "./schedule-spray-dialog.component";

describe("ScheduleSprayDialogComponent", () => {
  let component: ScheduleSprayDialogComponent;
  let fixture: ComponentFixture<ScheduleSprayDialogComponent>;
  let mockSprayService: jasmine.SpyObj<SprayService>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<ScheduleSprayDialogComponent>>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const sampleData: ScheduleSprayDialogData = {
    sprayId: "spray-100",
    referenceNumber: "SP-2026-0001",
    farmName: "Green Valley Farm",
    currentScheduledDateTime: "2026-10-15T08:00:00Z",
    isReschedule: false,
  };

  beforeEach(async () => {
    mockSprayService = jasmine.createSpyObj("SprayService", ["scheduleSpray", "rescheduleSpray"]);
    mockDialogRef = jasmine.createSpyObj("MatDialogRef", ["close"]);
    mockSnackBar = jasmine.createSpyObj("MatSnackBar", ["open"]);

    mockSprayService.scheduleSpray.and.returnValue(of({} as any));
    mockSprayService.rescheduleSpray.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [ScheduleSprayDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MAT_DIALOG_DATA, useValue: sampleData },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: SprayService, useValue: mockSprayService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleSprayDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create dialog and initialize scheduledDateTime", () => {
    expect(component).toBeTruthy();
    expect(component.form.get("scheduledDateTime")?.value).toBeTruthy();
  });

  it("should close dialog on cancel", () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(false);
  });

  it("should submit schedule spray", () => {
    component.form.patchValue({
      scheduledDateTime: "2026-10-16T10:00:00.000Z",
    });

    component.onSubmit();

    expect(mockSprayService.scheduleSpray).toHaveBeenCalled();
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
