import { parseDateOnly, formatDateOnly } from './date.utils';

describe('date.utils', () => {
  describe('parseDateOnly', () => {
    it('should return null for null, undefined, or empty values', () => {
      expect(parseDateOnly(null)).toBeNull();
      expect(parseDateOnly(undefined)).toBeNull();
      expect(parseDateOnly('')).toBeNull();
      expect(parseDateOnly('   ')).toBeNull();
    });

    it('should return the date object if passed a valid Date', () => {
      const date = new Date(2026, 8, 10);
      expect(parseDateOnly(date)).toBe(date);
    });

    it('should correctly parse YYYY-MM-DD into local Date', () => {
      const parsed = parseDateOnly('2026-09-10');
      expect(parsed).not.toBeNull();
      expect(parsed?.getFullYear()).toBe(2026);
      expect(parsed?.getMonth()).toBe(8); // September is 8 (0-indexed)
      expect(parsed?.getDate()).toBe(10);
    });

    it('should return null for invalid date string', () => {
      expect(parseDateOnly('invalid-date')).toBeNull();
    });
  });

  describe('formatDateOnly', () => {
    it('should return null for null, undefined, or empty values', () => {
      expect(formatDateOnly(null)).toBeNull();
      expect(formatDateOnly(undefined)).toBeNull();
      expect(formatDateOnly('')).toBeNull();
      expect(formatDateOnly('   ')).toBeNull();
    });

    it('should format a valid Date into YYYY-MM-DD', () => {
      const date = new Date(2026, 8, 10);
      expect(formatDateOnly(date)).toBe('2026-09-10');
    });

    it('should preserve already valid YYYY-MM-DD strings', () => {
      expect(formatDateOnly('2026-09-10')).toBe('2026-09-10');
    });

    it('should format single digit months and days with leading zeros', () => {
      const date = new Date(2026, 0, 5);
      expect(formatDateOnly(date)).toBe('2026-01-05');
    });

    it('should return null for invalid Date', () => {
      const invalidDate = new Date('invalid');
      expect(formatDateOnly(invalidDate)).toBeNull();
    });
  });
});
