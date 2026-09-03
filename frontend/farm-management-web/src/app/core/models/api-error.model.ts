export interface ApiErrorResponse {
  readonly message?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  const body = getApiErrorBody(error);
  return body?.message?.trim() || fallback;
}

export function getApiValidationErrors(
  error: unknown,
): Readonly<Record<string, readonly string[]>> {
  return getApiErrorBody(error)?.errors ?? {};
}

function getApiErrorBody(error: unknown): ApiErrorResponse | null {
  if (typeof error !== "object" || error === null || !("error" in error)) {
    return null;
  }
  const body = error.error;
  return typeof body === "object" && body !== null
    ? (body as ApiErrorResponse)
    : null;
}
