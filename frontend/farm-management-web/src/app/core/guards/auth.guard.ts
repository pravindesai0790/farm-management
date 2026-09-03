import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { map } from "rxjs/operators";

import { AuthService } from "../auth/auth.service";

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService
    .ensureAuthenticated()
    .pipe(
      map((isAuthenticated) =>
        isAuthenticated
          ? true
          : router.createUrlTree(["/login"], {
              queryParams: { returnUrl: state.url },
            }),
      ),
    );
};
