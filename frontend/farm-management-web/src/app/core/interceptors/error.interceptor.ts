import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { from, of, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

const loginPath = `${environment.apiUrl}/auth/login`;
const refreshPath = `${environment.apiUrl}/auth/refresh`;
const logoutPath = `${environment.apiUrl}/auth/logout`;

const isAuthenticationRequest = (url: string): boolean =>
  url === loginPath || url === refreshPath || url === logoutPath;

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const snackBar = inject(MatSnackBar);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (isAuthenticationRequest(request.url)) {
        return throwError(() => error);
      }

      if (error.status === 403) {
        void router.navigateByUrl('/forbidden');
        return throwError(() => error);
      }

      if (error.status === 500) {
        snackBar.open('Something went wrong. Please try again.', 'Dismiss', { duration: 5000 });
        return throwError(() => error);
      }

      if (error.status !== 401) {
        return throwError(() => error);
      }

      return authService.refreshAccessToken().pipe(
        switchMap((response) => next(request.clone({
          setHeaders: { Authorization: `Bearer ${response.accessToken}` },
          withCredentials: true
        }))),
        catchError((refreshError: unknown) => {
          return authService.logout().pipe(
            catchError(() => of(undefined)),
            switchMap(() => from(router.navigateByUrl('/login'))),
            switchMap(() => throwError(() => refreshError))
          );
        })
      );
    })
  );
};
