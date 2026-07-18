import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { UsersApi } from '../api/users.api';
import { AddLanguageRequest, LanguageLevel, UpdateProfileRequest, UserProfileDto } from '../models';
import { AuthStore } from '../auth/auth.store';

type Status = 'idle' | 'loading' | 'error';

interface ProfileState {
  profile: UserProfileDto | null;
  status: Status;
  editing: boolean;
  saving: boolean;
}

const initialState: ProfileState = { profile: null, status: 'idle', editing: false, saving: false };

export const ProfileStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, usersApi = inject(UsersApi), auth = inject(AuthStore)) => {
    const load = async (): Promise<void> => {
      const userId = auth.user()?.userId;
      if (!userId) {
        patchState(store, { profile: null, status: 'idle' });
        return;
      }
      patchState(store, { status: 'loading' });
      try {
        const profile = await firstValueFrom(usersApi.getUser(userId));
        patchState(store, { profile, status: 'idle' });
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    return {
      load,

      beginEdit(): void {
        patchState(store, { editing: true });
      },

      cancelEdit(): void {
        patchState(store, { editing: false });
      },

      async save(req: UpdateProfileRequest): Promise<void> {
        patchState(store, { saving: true });
        try {
          await firstValueFrom(usersApi.updateProfile(req));
          // Keep the auth/shell header in sync with the new display name.
          await auth.loadCurrentUser();
          await load();
          patchState(store, { editing: false, saving: false });
        } catch {
          patchState(store, { saving: false, status: 'error' });
        }
      },

      async addLanguage(req: AddLanguageRequest): Promise<void> {
        const profile = store.profile();
        if (!profile) {
          return;
        }
        try {
          const languageId = await firstValueFrom(usersApi.addLanguage(req));
          patchState(store, {
            profile: {
              ...profile,
              languages: [
                ...profile.languages,
                { id: languageId, languageCode: req.languageCode, type: req.type, level: req.level ?? null },
              ],
            },
          });
        } catch {
          /* ignore */
        }
      },

      async updateLanguage(languageId: number, level: LanguageLevel | null): Promise<void> {
        const profile = store.profile();
        if (!profile) {
          return;
        }
        try {
          await firstValueFrom(usersApi.updateLanguage(languageId, { level }));
          patchState(store, {
            profile: {
              ...profile,
              languages: profile.languages.map((l) => (l.id === languageId ? { ...l, level } : l)),
            },
          });
        } catch {
          /* ignore */
        }
      },

      async removeLanguage(languageId: number): Promise<void> {
        const profile = store.profile();
        if (!profile) {
          return;
        }
        try {
          await firstValueFrom(usersApi.removeLanguage(languageId));
          patchState(store, {
            profile: {
              ...profile,
              languages: profile.languages.filter((l) => l.id !== languageId),
            },
          });
        } catch {
          /* ignore */
        }
      },
    };
  }),
);
