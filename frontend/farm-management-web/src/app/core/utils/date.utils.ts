/**
 * Date conversion utilities to safely bridge between JavaScript Date objects
 * used by Angular Material Datepicker and ISO 'YYYY-MM-DD' DateOnly strings used by the backend API.
 */

/**
 * Parses a DateOnly string ('YYYY-MM-DD'), ISO string, or Date instance into a local Date object.
 * Avoids UTC timezone conversion offsets by constructing local Date instances for 'YYYY-MM-DD' inputs.
 */
export function parseDateOnly(
  value: string | Date | null | undefined,
): Date | null {
  if (!value) {
    return null;
  }

  if (value instanceof Date) {
    return isNaN(value.getTime()) ? null : value;
  }

  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(trimmed);
    if (match) {
      const year = parseInt(match[1], 10);
      const month = parseInt(match[2], 10) - 1;
      const day = parseInt(match[3], 10);
      const date = new Date(year, month, day);
      return isNaN(date.getTime()) ? null : date;
    }

    const fallback = new Date(trimmed);
    return isNaN(fallback.getTime()) ? null : fallback;
  }

  return null;
}

/**
 * Formats a Date object or date string into a clean 'YYYY-MM-DD' string for backend consumption.
 * Returns null if the value is empty or invalid.
 */
export function formatDateOnly(
  value: Date | string | null | undefined,
): string | null {
  if (!value) {
    return null;
  }

  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }
    if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
      return trimmed;
    }
    const parsed = parseDateOnly(trimmed);
    return parsed ? formatDateOnly(parsed) : null;
  }

  if (value instanceof Date) {
    if (isNaN(value.getTime())) {
      return null;
    }
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  return null;
}
