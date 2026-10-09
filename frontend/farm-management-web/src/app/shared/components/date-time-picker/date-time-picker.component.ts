import { CommonModule } from "@angular/common";
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  forwardRef,
  signal,
} from "@angular/core";
import {
  ControlValueAccessor,
  FormsModule,
  NG_VALUE_ACCESSOR,
} from "@angular/forms";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTimepickerModule } from "@angular/material/timepicker";

@Component({
  selector: "app-date-time-picker",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatTimepickerModule,
    MatIconModule,
  ],
  templateUrl: "./date-time-picker.component.html",
  styleUrl: "./date-time-picker.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DateTimePickerComponent),
      multi: true,
    },
  ],
})
export class DateTimePickerComponent implements ControlValueAccessor {
  @Input() label = "Date & Time";
  @Input() dateLabel = "Date";
  @Input() timeLabel = "Time";
  @Input() required = false;
  @Input() maxDate: Date | null = null;
  @Input() minDate: Date | null = null;
  @Input() interval: string | number = "30m";

  readonly selectedDate = signal<Date | null>(null);
  readonly selectedTimeDate = signal<Date | null>(null);
  readonly isDisabled = signal<boolean>(false);

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | Date | null): void {
    if (!value) {
      this.selectedDate.set(null);
      this.selectedTimeDate.set(null);
      return;
    }

    const d = typeof value === "string" ? new Date(value) : value;
    if (isNaN(d.getTime())) {
      this.selectedDate.set(null);
      this.selectedTimeDate.set(null);
      return;
    }

    this.selectedDate.set(d);
    this.selectedTimeDate.set(d);
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }

  onDateChange(newDate: Date | null): void {
    this.selectedDate.set(newDate);
    // If date is selected but no time yet, default time to current time
    if (newDate && !this.selectedTimeDate()) {
      const now = new Date();
      this.selectedTimeDate.set(now);
    }
    this.emitCombinedValue();
  }

  onTimeChange(newTime: Date | null): void {
    this.selectedTimeDate.set(newTime);
    this.emitCombinedValue();
  }

  private emitCombinedValue(): void {
    this.onTouched();
    const dateVal = this.selectedDate();
    if (!dateVal) {
      this.onChange(null);
      return;
    }

    const combined = new Date(dateVal);
    const timeVal = this.selectedTimeDate();
    if (timeVal) {
      combined.setHours(timeVal.getHours(), timeVal.getMinutes(), 0, 0);
    } else {
      combined.setHours(0, 0, 0, 0);
    }

    this.onChange(combined.toISOString());
  }
}

