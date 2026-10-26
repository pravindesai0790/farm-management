import { downloadFileBlob, extractBlobErrorMessage } from './download.utils';

describe('download.utils', () => {
  describe('downloadFileBlob', () => {
    let createObjectURLSpy: jasmine.Spy;
    let revokeObjectURLSpy: jasmine.Spy;

    beforeEach(() => {
      createObjectURLSpy = spyOn(window.URL, 'createObjectURL').and.returnValue('blob:http://localhost/test');
      revokeObjectURLSpy = spyOn(window.URL, 'revokeObjectURL');
      spyOn(document.body, 'appendChild').and.callThrough();
      spyOn(document.body, 'removeChild').and.callThrough();
    });

    it('should use defaultFilename when contentDisposition is omitted', () => {
      const blob = new Blob(['sample data'], { type: 'text/csv' });
      downloadFileBlob(blob, 'export.csv');

      expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
      expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:http://localhost/test');
    });

    it('should extract filename from Content-Disposition header when present', () => {
      const blob = new Blob(['sample data'], { type: 'text/csv' });
      downloadFileBlob(
        blob,
        'fallback.csv',
        'attachment; filename="custom_fertilizer_report.csv"',
      );

      expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
      expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:http://localhost/test');
    });
  });

  describe('extractBlobErrorMessage', () => {
    it('should extract detail from JSON error blob', async () => {
      const errorJson = JSON.stringify({
        title: 'Validation Error',
        detail: 'Organization scope mismatch.',
      });
      const errorBlob = new Blob([errorJson], { type: 'application/problem+json' });
      const httpError = { error: errorBlob };

      const msg = await extractBlobErrorMessage(httpError, 'Fallback message');
      expect(msg).toBe('Organization scope mismatch.');
    });

    it('should extract title from JSON error blob if detail is missing', async () => {
      const errorJson = JSON.stringify({
        title: 'Export Failed',
      });
      const errorBlob = new Blob([errorJson], { type: 'application/problem+json' });
      const httpError = { error: errorBlob };

      const msg = await extractBlobErrorMessage(httpError, 'Fallback message');
      expect(msg).toBe('Export Failed');
    });

    it('should return raw text if blob contains non-JSON text', async () => {
      const rawText = 'Server Error 500';
      const errorBlob = new Blob([rawText], { type: 'text/plain' });
      const httpError = { error: errorBlob };

      const msg = await extractBlobErrorMessage(httpError, 'Fallback message');
      expect(msg).toBe('Server Error 500');
    });

    it('should fallback when error is standard non-blob error object', async () => {
      const httpError = { error: { message: 'Network Timeout' } };
      const msg = await extractBlobErrorMessage(httpError, 'Default Fallback');
      expect(msg).toBe('Network Timeout');
    });
  });
});
