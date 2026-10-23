import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';

/** Protects the authenticated app; sends logged-out users to login, preserving
 *  the intended URL (incl. query params) as ?returnUrl so deep links (e.g. the
 *  email-verification link) survive the login round-trip. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Keeps logged-in users out of the public entry (landing / login / register). */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  return auth.isAuthenticated() ? router.createUrlTree(['/app/feed']) : true;
};

/**
 * Restricts a route to Administrators. Authenticated non-admins are sent to the
 * feed; logged-out users are sent to login (authGuard runs first on /app).
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const roles = auth.user()?.roles ?? [];
  return roles.includes('Administrator') ? true : router.createUrlTree(['/app/feed']);
};
