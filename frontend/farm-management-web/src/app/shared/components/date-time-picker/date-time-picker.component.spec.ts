import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideNativeDateAdapter } from "@angular/material/core";
import { NoopAnimationsModule } from "@angular/platform-browser/animations";
import { DateTimePickerComponent } from "./date-time-picker.component";

describe("DateTimePickerComponent", () => {
  let component: DateTimePickerComponent;
  let fixture: ComponentFixture<DateTimePickerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DateTimePickerComponent, NoopAnimationsModule],
      providers: [provideNativeDateAdapter()],
    }).compileComponents();

    fixture = TestBed.createComponent(DateTimePickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });

  it("should parse incoming ISO date string in writeValue", () => {
    const testDate = new Date(2026, 9, 15, 14, 30, 0); // Oct 15, 2026 14:30
    component.writeValue(testDate.toISOString());

    expect(component.selectedDate()).toBeTruthy();
    expect(component.selectedDate()?.getFullYear()).toBe(2026);
    expect(component.selectedDate()?.getMonth()).toBe(9);
    expect(component.selectedDate()?.getDate()).toBe(15);
    expect(component.selectedTime()).toBe("14:30");
  });

  it("should reset on writeValue with null", () => {
    component.writeValue(null);
    expect(component.selectedDate()).toBeNull();
    expect(component.selectedTime()).toBe("");
  });

  it("should emit combined ISO string when date changes", () => {
    let emittedVal: string | null = null;
    component.registerOnChange((val) => {
      emittedVal = val;
    });

    component.selectedTime.set("10:15");
    const newDate = new Date(2026, 9, 20);
    component.onDateChange(newDate);

    expect(emittedVal).toBeTruthy();
    const parsed = new Date(emittedVal!);
    expect(parsed.getFullYear()).toBe(2026);
    expect(parsed.getMonth()).toBe(9);
    expect(parsed.getDate()).toBe(20);
    expect(parsed.getHours()).toBe(10);
    expect(parsed.getMinutes()).toBe(15);
  });

  it("should emit combined ISO string when time changes", () => {
    let emittedVal: string | null = null;
    component.registerOnChange((val) => {
      emittedVal = val;
    });

    component.selectedDate.set(new Date(2026, 9, 20));
    component.onTimeChange({ target: { value: "16:45" } } as any);

    expect(emittedVal).toBeTruthy();
    const parsed = new Date(emittedVal!);
    expect(parsed.getHours()).toBe(16);
    expect(parsed.getMinutes()).toBe(45);
  });

  it("should update disabled state", () => {
    component.setDisabledState(true);
    expect(component.isDisabled()).toBeTrue();
  });
});
