import { Injectable, signal } from '@angular/core';

/**
 * In-memory access-token store. The refresh token is an HttpOnly, Secure
 * cookie managed entirely by the browser; Angular never reads it.
 *
 * Access token is intentionally NOT persisted to localStorage — keeping it
 * in memory limits exposure if the tab is compromised.
 */
@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly _token = signal<string | null>(null);

  /** Readonly signal of the current access token (null when logged out). */
  readonly token = this._token.asReadonly();

  get value(): string | null {
    return this._token();
  }

  set(token: string | null): void {
    this._token.set(token);
  }

  clear(): void {
    this._token.set(null);
  }
}
