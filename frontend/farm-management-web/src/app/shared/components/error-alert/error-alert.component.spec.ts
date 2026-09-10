import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ErrorAlertComponent } from './error-alert.component';

describe('ErrorAlertComponent', () => {
  let component: ErrorAlertComponent;
  let fixture: ComponentFixture<ErrorAlertComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorAlertComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorAlertComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should not render anything when error and fallback are empty', () => {
    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).toBeNull();
  });

  it('should not render anything when error is null even if fallback is provided', () => {
    fixture.componentRef.setInput('error', null);
    fixture.componentRef.setInput('fallback', 'Farm could not be saved.');
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).toBeNull();
  });

  it('should not render anything when error is empty string even if fallback is provided', () => {
    fixture.componentRef.setInput('error', '');
    fixture.componentRef.setInput('fallback', 'Farm could not be saved.');
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).toBeNull();
  });

  it('should render single validation error from errors property', () => {
    fixture.componentRef.setInput('error', {
      error: {
        message: 'Validation failed',
        errors: {
          area: ['Allocated area exceeds available area.'],
        },
      },
    });
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('Allocated area exceeds available area.');
    expect(alert.textContent).not.toContain('Validation failed');
  });

  it('should render multiple validation errors as a list', () => {
    fixture.componentRef.setInput('error', {
      error: {
        message: 'Validation failed',
        errors: {
          area: ['Allocated area exceeds available area.'],
          code: ['Code is required.'],
        },
      },
    });
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.error-list li');
    expect(items.length).toBe(2);
    expect(items[0].textContent).toContain('Allocated area exceeds available area.');
    expect(items[1].textContent).toContain('Code is required.');
  });

  it('should render message when errors property does not exist', () => {
    fixture.componentRef.setInput('error', {
      error: {
        message: 'A duplicate record exists.',
      },
    });
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('A duplicate record exists.');
  });

  it('should render fallback when error has no message or errors', () => {
    fixture.componentRef.setInput('error', {});
    fixture.componentRef.setInput('fallback', 'Something went wrong. Please try again.');
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('.error-alert');
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('Something went wrong. Please try again.');
  });
});
