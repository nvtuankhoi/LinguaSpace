import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ActiveSessionDto,
  AuthResponseDto,
  ChangeEmailRequest,
  ChangePasswordRequest,
  CurrentUserDto,
  LoginRequest,
  RegisterRequest,
  RegisterResult,
  VerifyEmailRequest,
} from '../models';

/**
 * Auth HTTP client. The silent token refresh is owned by the auth interceptor
 * (core/http/auth.interceptor.ts), so it is intentionally not exposed here.
 */
@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  login(req: LoginRequest): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.base}/Auth/login`, req, { withCredentials: true });
  }

  register(req: RegisterRequest): Observable<RegisterResult> {
    return this.http.post<RegisterResult>(`${this.base}/Auth/register`, req);
  }

  me(): Observable<CurrentUserDto> {
    return this.http.get<CurrentUserDto>(`${this.base}/Auth/me`, { withCredentials: true });
  }

  logout(): Observable<void> {
    // withCredentials sends the HttpOnly refresh_token cookie so the server can
    // revoke it and clear the cookie (login/me do the same).
    return this.http.post<void>(`${this.base}/Auth/logout`, {}, { withCredentials: true });
  }

  // ─── Account management ──────────────────────────────────────────────────

  resendVerification(): Observable<void> {
    return this.http.post<void>(`${this.base}/Auth/resend-verification`, {}, { withCredentials: true });
  }

  /** Confirms the email using the token sent at registration. POST /Auth/verify-email → 204. */
  verifyEmail(req: VerifyEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/Auth/verify-email`, req, { withCredentials: true });
  }

  changePassword(req: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/Auth/change-password`, req, { withCredentials: true });
  }

  changeEmail(req: ChangeEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/Auth/change-email`, req, { withCredentials: true });
  }

  getSessions(): Observable<ActiveSessionDto[]> {
    return this.http.get<ActiveSessionDto[]>(`${this.base}/Auth/sessions`, { withCredentials: true });
  }

  revokeAllSessions(): Observable<void> {
    return this.http.delete<void>(`${this.base}/Auth/sessions`, { withCredentials: true });
  }

  revokeSession(sessionId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/Auth/sessions/${sessionId}`, { withCredentials: true });
  }

  forgotPassword(email: string): Observable<unknown> {
    // Always 200 (no enumeration) per contract; withCredentials in case the
    // response sets anything, harmless otherwise.
    return this.http.post(`${this.base}/Auth/forgot-password`, { email }, { withCredentials: true });
  }

  resetPassword(req: { token: string; newPassword: string }): Observable<void> {
    return this.http.post<void>(`${this.base}/Auth/reset-password`, req, { withCredentials: true });
  }
}
