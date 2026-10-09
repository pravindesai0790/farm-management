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

@Component({
  selector: "app-date-time-picker",
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
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

  readonly selectedDate = signal<Date | null>(null);
  readonly selectedTime = signal<string>("");
  readonly isDisabled = signal<boolean>(false);

  private onChange: (value: string | null) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string | Date | null): void {
    if (!value) {
      this.selectedDate.set(null);
      this.selectedTime.set("");
      return;
    }

    const d = typeof value === "string" ? new Date(value) : value;
    if (isNaN(d.getTime())) {
      this.selectedDate.set(null);
      this.selectedTime.set("");
      return;
    }

    this.selectedDate.set(d);
    const hh = String(d.getHours()).padStart(2, "0");
    const mm = String(d.getMinutes()).padStart(2, "0");
    this.selectedTime.set(`${hh}:${mm}`);
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
    // If date is selected but no time yet, default time to current time or 08:00
    if (newDate && !this.selectedTime()) {
      const now = new Date();
      const hh = String(now.getHours()).padStart(2, "0");
      const mm = String(now.getMinutes()).padStart(2, "0");
      this.selectedTime.set(`${hh}:${mm}`);
    }
    this.emitCombinedValue();
  }

  onTimeChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedTime.set(input?.value || "");
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
    const timeVal = this.selectedTime();
    if (timeVal) {
      const parts = timeVal.split(":");
      const hours = parseInt(parts[0], 10) || 0;
      const minutes = parseInt(parts[1], 10) || 0;
      combined.setHours(hours, minutes, 0, 0);
    } else {
      combined.setHours(0, 0, 0, 0);
    }

    this.onChange(combined.toISOString());
  }
}
