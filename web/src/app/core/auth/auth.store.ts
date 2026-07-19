import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { AuthApi } from '../api/auth.api';
import { CurrentUserDto, LoginRequest, RegisterRequest } from '../models';
import { TokenService } from './token.service';

export type AuthStatus = 'idle' | 'loading' | 'authenticated' | 'error';

interface AuthState {
  user: CurrentUserDto | null;
  status: AuthStatus;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  status: 'idle',
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ user, status }) => ({
    isAuthenticated: computed(() => user() !== null),
    isLoading: computed(() => status() === 'loading'),
  })),
  withMethods((store, authApi = inject(AuthApi), tokens = inject(TokenService)) => {
    const authenticate = async (email: string, password: string): Promise<void> => {
      const res = await firstValueFrom(authApi.login({ email, password }));
      tokens.set(res.accessToken);
      const user = await firstValueFrom(authApi.me());
      patchState(store, { user, status: 'authenticated', error: null });
    };

    return {
      async login(req: LoginRequest): Promise<void> {
        patchState(store, { status: 'loading', error: null });
        try {
          await authenticate(req.email, req.password);
        } catch (err) {
          patchState(store, { status: 'error', error: messageOf(err) });
          throw err;
        }
      },

      async register(req: RegisterRequest): Promise<void> {
        patchState(store, { status: 'loading', error: null });
        try {
          await firstValueFrom(authApi.register(req));
          await authenticate(req.email, req.password);
        } catch (err) {
          patchState(store, { status: 'error', error: messageOf(err) });
          throw err;
        }
      },

      /** Restores the session on cold start using the refresh cookie (handled by the interceptor). */
      async loadCurrentUser(): Promise<void> {
        try {
          const user = await firstValueFrom(authApi.me());
          patchState(store, { user, status: 'authenticated', error: null });
        } catch {
          tokens.clear();
          patchState(store, { user: null, status: 'idle' });
        }
      },

      async logout(): Promise<void> {
        try {
          await firstValueFrom(authApi.logout());
        } catch {
          /* even if the call fails, clear local state */
        }
        tokens.clear();
        patchState(store, { user: null, status: 'idle', error: null });
      },

      clearError(): void {
        patchState(store, { error: null, status: 'idle' });
      },
    };
  }),
);

function messageOf(err: unknown): string {
  if (err && typeof err === 'object' && 'error' in err) {
    const body = (err as { error?: unknown }).error;
    if (body && typeof body === 'object' && 'detail' in body) {
      return String((body as { detail?: unknown }).detail);
    }
    if (typeof body === 'string') {
      return body;
    }
  }
  return 'Something went wrong. Please try again.';
}
