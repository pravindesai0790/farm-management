import { getApiErrorMessage } from '../models/api-error.model';

/**
 * Downloads a Blob as a file with the given fallback filename or extracted from Content-Disposition header.
 */
export function downloadFileBlob(
  blob: Blob,
  defaultFilename: string,
  contentDisposition?: string | null,
): void {
  let filename = defaultFilename;

  if (contentDisposition) {
    // Check for standard filename or UTF-8 encoded filename*
    const match = /filename\*?=['"]?(?:UTF-\d['"]*)?([^;\r\n"']*)['"]?/i.exec(contentDisposition);
    if (match && match[1]) {
      try {
        filename = decodeURIComponent(match[1].trim());
      } catch {
        filename = match[1].trim();
      }
    }
  }

  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  window.URL.revokeObjectURL(url);
}

/**
 * Asynchronously extracts an error message from an unknown HTTP error.
 * If the error body is a Blob (such as with responseType: 'blob'), parses text or JSON problem details.
 */
export async function extractBlobErrorMessage(error: unknown, fallback: string): Promise<string> {
  if (error && typeof error === 'object' && 'error' in error) {
    const errorBody = (error as { error: unknown }).error;
    if (errorBody instanceof Blob) {
      try {
        const text = await errorBody.text();
        if (text && text.trim()) {
          try {
            const parsed = JSON.parse(text);
            if (typeof parsed === 'object' && parsed !== null) {
              if (parsed.detail && typeof parsed.detail === 'string') {
                return parsed.detail.trim();
              }
              if (parsed.title && typeof parsed.title === 'string') {
                return parsed.title.trim();
              }
              if (parsed.message && typeof parsed.message === 'string') {
                return parsed.message.trim();
              }
            }
          } catch {
            // Not valid JSON, return raw text if reasonable
            return text.trim();
          }
        }
      } catch {
        // Blob reading failed, continue to fallback
      }
    }
  }

  return getApiErrorMessage(error, fallback);
}
