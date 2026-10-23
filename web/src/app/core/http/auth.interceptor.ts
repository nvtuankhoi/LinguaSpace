import {
  HttpContext,
  HttpContextToken,
  HttpClient,
  HttpErrorResponse,
  HttpEvent,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { catchError, finalize, map, share, switchMap, tap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthStore } from '../auth/auth.store';
import { TokenService } from '../auth/token.service';

/**
 * Marks a request to bypass the bearer header and 401-refresh logic.
 * Used by the refresh call itself to avoid recursion.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

const REFRESH_URL = `${environment.apiBaseUrl}/Auth/refresh`;

// The single in-flight refresh, shared by every concurrent 401. Cleared on
// settle (success or failure) so a later 401 — after this token has also
// expired — can trigger a fresh refresh. Module-level so concurrent 401s
// share one refresh POST and, crucially, one failure (no thundering herd).
let refresh$: Observable<string> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Inject up front, in the injection context — reactive callbacks below run
  // outside it, so inject() there would throw (NG0203).
  const tokens = inject(TokenService);
  const http = inject(HttpClient);
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (req.context.get(SKIP_AUTH)) {
    return next(req);
  }

  return next(applyToken(req, tokens.value)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }
      return refreshAndRetry(req, next, tokens, http, auth, router);
    }),
  );
};

/** Refreshes once; concurrent 401s share the same refresh (and the same failure). */
function refreshAndRetry(
  req: Parameters<HttpInterceptorFn>[0],
  next: Parameters<HttpInterceptorFn>[1],
  tokens: TokenService,
  http: HttpClient,
  auth: InstanceType<typeof AuthStore>,
  router: Router,
): Observable<HttpEvent<unknown>> {
  return getRefresh$(tokens, http, auth, router).pipe(
    switchMap((token) => next(applyToken(req, token))),
  );
}

/**
 * Returns the shared in-flight refresh, creating it on first demand. The
 * refresh-failure side-effect (forceLogout + redirect) lives upstream of
 * `share()`, so it fires exactly once even when many 401s are waiting — and
 * because the error multicasts to every subscriber, none of them hang.
 */
function getRefresh$(
  tokens: TokenService,
  http: HttpClient,
  auth: InstanceType<typeof AuthStore>,
  router: Router,
): Observable<string> {
  if (refresh$) {
    return refresh$;
  }

  refresh$ = http
    .post<{ accessToken: string; expiresIn: number }>(REFRESH_URL, {}, {
      context: new HttpContext().set(SKIP_AUTH, true),
      withCredentials: true,
    })
    .pipe(
      tap((res) => tokens.set(res.accessToken)),
      map((res) => res.accessToken),
      catchError((err) => {
        // Refresh token expired/revoked — the session is over. Tear down local
        // auth state and bounce to /login. Mid-session only: on cold start the
        // user is still null here (loadCurrentUser hasn't resolved), and
        // navigating during bootstrap is unsafe — loadCurrentUser's own catch
        // + the auth guard already route a dead session to login.
        const wasAuthenticated = auth.user() !== null;
        auth.forceLogout();
        if (wasAuthenticated) {
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
        }
        return throwError(() => err);
      }),
      finalize(() => {
        refresh$ = null;
      }),
      share(),
    );
  return refresh$;
}

function applyToken(req: Parameters<HttpInterceptorFn>[0], token: string | null) {
  if (!token) {
    return req;
  }
  // withCredentials sends the HttpOnly refresh_token cookie on refresh.
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` }, withCredentials: true });
}
