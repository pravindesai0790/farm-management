import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { FertilizerApplicationDetailsResponse } from '../../../../core/fertilizers/fertilizer.models';
import { FertilizerService } from '../../../../core/fertilizers/fertilizer.service';
import {
  ScheduleFertilizerDialogComponent,
  ScheduleFertilizerDialogData,
} from './schedule-fertilizer-dialog.component';

describe('ScheduleFertilizerDialogComponent', () => {
  let component: ScheduleFertilizerDialogComponent;
  let fixture: ComponentFixture<ScheduleFertilizerDialogComponent>;

  let mockDialogRef: jasmine.SpyObj<MatDialogRef<ScheduleFertilizerDialogComponent>>;
  let mockFertilizerService: jasmine.SpyObj<FertilizerService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const defaultData: ScheduleFertilizerDialogData = {
    applicationId: 'app-1',
    referenceNumber: 'FA-202610-001',
    farmName: 'Green Valley Farm',
    status: 'Planned',
    currentScheduledDate: '2026-10-25',
    isReschedule: false,
    version: 1,
  };

  const sampleResult: FertilizerApplicationDetailsResponse = {
    id: 'app-1',
    referenceNumber: 'FA-202610-001',
    status: 'Scheduled',
    statusName: 'Scheduled',
    version: 2,
  } as any;

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockFertilizerService = jasmine.createSpyObj('FertilizerService', ['schedule', 'reschedule']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [ScheduleFertilizerDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: defaultData },
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleFertilizerDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and initialize scheduledDate from data', () => {
    expect(component).toBeTruthy();
    expect(component.form.value.scheduledDate).toBeTruthy();
    expect(component.data.isReschedule).toBeFalse();
  });

  it('should call schedule on submit when isReschedule is false', () => {
    mockFertilizerService.schedule.and.returnValue(of(sampleResult));

    component.form.patchValue({
      scheduledDate: new Date(2026, 9, 28),
    });

    component.onSubmit();

    expect(mockFertilizerService.schedule).toHaveBeenCalledWith(
      'app-1',
      jasmine.objectContaining({
        scheduledDate: jasmine.stringMatching(/2026-10/),
        version: 1,
      }),
    );
    expect(mockSnackBar.open).toHaveBeenCalledWith(
      jasmine.stringMatching(/scheduled/),
      'Dismiss',
      jasmine.any(Object),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(sampleResult);
  });

  it('should call reschedule on submit when isReschedule is true', async () => {
    // Reconfigure for reschedule test
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ScheduleFertilizerDialogComponent, NoopAnimationsModule],
      providers: [
        provideNativeDateAdapter(),
        { provide: MatDialogRef, useValue: mockDialogRef },
        {
          provide: MAT_DIALOG_DATA,
          useValue: { ...defaultData, isReschedule: true, status: 'Scheduled' },
        },
        { provide: FertilizerService, useValue: mockFertilizerService },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    const rescheduleFixture = TestBed.createComponent(ScheduleFertilizerDialogComponent);
    const rescheduleComp = rescheduleFixture.componentInstance;
    rescheduleFixture.detectChanges();

    mockFertilizerService.reschedule.and.returnValue(of(sampleResult));

    rescheduleComp.form.patchValue({
      scheduledDate: new Date(2026, 9, 30),
    });

    rescheduleComp.onSubmit();

    expect(mockFertilizerService.reschedule).toHaveBeenCalledWith(
      'app-1',
      jasmine.objectContaining({
        scheduledDate: jasmine.stringMatching(/2026-10/),
        version: 1,
      }),
    );
    expect(mockDialogRef.close).toHaveBeenCalledWith(sampleResult);
  });

  it('should handle API errors and set errorMessage signal', () => {
    mockFertilizerService.schedule.and.returnValue(
      throwError(() => new Error('Concurrency conflict')),
    );

    component.onSubmit();

    expect(component.isSubmitting()).toBeFalse();
    expect(component.errorMessage()).toBe('Concurrency conflict');
    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });

  it('should close dialog with null on onCancel', () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith(null);
  });
});
