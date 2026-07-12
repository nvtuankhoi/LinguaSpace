import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthStore } from './auth.store';

/** Protects the authenticated app; sends logged-out users to login. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  return auth.isAuthenticated() ? true : router.createUrlTree(['/login']);
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
