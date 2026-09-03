import { HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";

import { environment } from "../../../environments/environment";
import { AuthStore } from "../auth/auth.store";

const loginPath = `${environment.apiUrl}/auth/login`;
const refreshPath = `${environment.apiUrl}/auth/refresh`;

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const accessToken = inject(AuthStore).accessToken();
  const isAuthenticationRequest =
    request.url === loginPath || request.url === refreshPath;
  const isApiRequest = request.url.startsWith(environment.apiUrl);

  const headers: Record<string, string> = {};
  if (accessToken !== null && !isAuthenticationRequest) {
    headers["Authorization"] = `Bearer ${accessToken}`;
  }

  return next(
    request.clone({
      setHeaders: headers,
      withCredentials: isApiRequest || request.withCredentials,
    }),
  );
};
