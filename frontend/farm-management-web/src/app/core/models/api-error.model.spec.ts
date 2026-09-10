import {
  getApiErrorMessage,
  getApiValidationErrors,
  getApiValidationMessages,
} from './api-error.model';

describe('api-error.model', () => {
  it('should extract validation messages from errors dictionary', () => {
    const errorResponse = {
      error: {
        message: 'Validation failed',
        errors: {
          allocatedArea: ['Allocated area exceeds available area (0 ha).'],
          plantationCode: ['Plantation code already exists.'],
        },
      },
    };

    const messages = getApiValidationMessages(errorResponse);
    expect(messages).toEqual([
      'Allocated area exceeds available area (0 ha).',
      'Plantation code already exists.',
    ]);
  });

  it('should prioritize validation errors over message in getApiErrorMessage', () => {
    const errorResponse = {
      error: {
        message: 'Validation failed',
        errors: {
          allocatedArea: ['Allocated area exceeds available area (0 ha).'],
        },
      },
    };

    const result = getApiErrorMessage(errorResponse, 'Fallback');
    expect(result).toBe('Allocated area exceeds available area (0 ha).');
    expect(result).not.toContain('Validation failed');
  });

  it('should return message when errors property does not exist', () => {
    const errorResponse = {
      error: {
        message: 'A plantation with this code already exists.',
      },
    };

    const result = getApiErrorMessage(errorResponse, 'Fallback');
    expect(result).toBe('A plantation with this code already exists.');
  });

  it('should return message when errors property is empty', () => {
    const errorResponse = {
      error: {
        message: 'Conflict occurred',
        errors: {},
      },
    };

    const result = getApiErrorMessage(errorResponse, 'Fallback');
    expect(result).toBe('Conflict occurred');
  });

  it('should return fallback when error is null or undefined', () => {
    expect(getApiErrorMessage(null, 'Fallback error')).toBe('Fallback error');
    expect(getApiErrorMessage(undefined, 'Fallback error')).toBe('Fallback error');
  });

  it('should return string error directly if error is a string', () => {
    expect(getApiErrorMessage('Something broke', 'Fallback')).toBe('Something broke');
  });
});
