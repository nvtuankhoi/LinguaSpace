import {
  HttpContext,
  HttpContextToken,
  HttpClient,
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, tap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TokenService } from '../auth/token.service';

/**
 * Marks a request to bypass the bearer header and 401-refresh logic.
 * Used by the refresh call itself to avoid recursion.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

const REFRESH_URL = `${environment.apiBaseUrl}/Auth/refresh`;

// Single-flight refresh state (module-level so concurrent 401s share one refresh).
let isRefreshing = false;
const tokenSubject = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenService);
  const http = inject(HttpClient);

  if (req.context.get(SKIP_AUTH)) {
    return next(req);
  }

  return next(applyToken(req, tokens.value)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }
      return refreshAndRetry(req, next, tokens, http);
    }),
  );
};

/** Refreshes the access token once; concurrent 401s wait for the same refresh. */
function refreshAndRetry(
  req: Parameters<HttpInterceptorFn>[0],
  next: Parameters<HttpInterceptorFn>[1],
  tokens: TokenService,
  http: HttpClient,
) {
  if (!isRefreshing) {
    isRefreshing = true;
    tokenSubject.next(null);

    return http
      .post<{ accessToken: string; expiresIn: number }>(REFRESH_URL, {}, {
        context: new HttpContext().set(SKIP_AUTH, true),
        withCredentials: true,
      })
      .pipe(
        tap((res) => tokens.set(res.accessToken)),
        switchMap((res) => {
          isRefreshing = false;
          tokenSubject.next(res.accessToken);
          return next(applyToken(req, res.accessToken));
        }),
        catchError((err) => {
          isRefreshing = false;
          tokenSubject.next(null);
          tokens.clear();
          return throwError(() => err);
        }),
      );
  }

  // A refresh is in flight — wait for the fresh token, then retry once.
  return tokenSubject.pipe(
    filter((t): t is string => t !== null),
    take(1),
    switchMap((token) => next(applyToken(req, token))),
  );
}

function applyToken(req: Parameters<HttpInterceptorFn>[0], token: string | null) {
  if (!token) {
    return req;
  }
  // withCredentials sends the HttpOnly refresh_token cookie on refresh.
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` }, withCredentials: true });
}
