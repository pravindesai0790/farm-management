export interface ApiErrorResponse {
  readonly message?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

export function getApiErrorBody(error: unknown): ApiErrorResponse | null {
  if (typeof error !== "object" || error === null) {
    return null;
  }
  if ("error" in error && error.error) {
    const body = error.error;
    if (typeof body === "object" && body !== null) {
      return body as ApiErrorResponse;
    }
  }
  return error as ApiErrorResponse;
}

export function getApiValidationErrors(
  error: unknown,
): Readonly<Record<string, readonly string[]>> {
  const body = getApiErrorBody(error);
  if (
    body &&
    typeof body === "object" &&
    "errors" in body &&
    body.errors &&
    typeof body.errors === "object"
  ) {
    return body.errors as Readonly<Record<string, readonly string[]>>;
  }
  return {};
}

export function getApiValidationMessages(error: unknown): readonly string[] {
  const errorsObj = getApiValidationErrors(error);
  const result: string[] = [];
  for (const [_, val] of Object.entries(errorsObj)) {
    if (Array.isArray(val)) {
      for (const msg of val) {
        if (typeof msg === "string" && msg.trim()) {
          result.push(msg.trim());
        }
      }
    } else if (typeof val === "string" && (val as string).trim()) {
      result.push((val as string).trim());
    }
  }
  return result;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  const validationMessages = getApiValidationMessages(error);
  if (validationMessages.length > 0) {
    return validationMessages.join(" ");
  }
  const body = getApiErrorBody(error);
  if (typeof body?.message === "string" && body.message.trim()) {
    return body.message.trim();
  }
  if (typeof error === "string" && error.trim()) {
    return error.trim();
  }
  return fallback;
}
