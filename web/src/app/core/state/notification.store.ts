import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { NotificationsApi } from '../api/notifications.api';
import { NotificationDto } from '../models';

type Status = 'idle' | 'loading' | 'error';

interface NotificationState {
  items: NotificationDto[];
  status: Status;
  unreadCount: number;
}

const initialState: NotificationState = { items: [], status: 'idle', unreadCount: 0 };

export const NotificationStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, api = inject(NotificationsApi)) => {
    const loadUnreadCount = async (): Promise<void> => {
      try {
        const count = await firstValueFrom(api.getUnreadCount());
        patchState(store, { unreadCount: count });
      } catch {
        /* silent — badge is non-critical */
      }
    };

    const load = async (): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const res = await firstValueFrom(api.getNotifications());
        patchState(store, { items: res.items, status: 'idle' });
        await loadUnreadCount();
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    return {
      load,
      loadUnreadCount,

      /** Live notification pushed via PresenceHub (PresenceRealtimeService.onNotification). */
      addRealtime(n: NotificationDto): void {
        if (store.items().some((x) => x.id === n.id)) {
          return;
        }
        patchState(store, {
          items: [n, ...store.items()],
          unreadCount: store.unreadCount() + 1,
        });
      },

      async markRead(ids: number[]): Promise<void> {
        await firstValueFrom(api.markRead({ notificationIds: ids }));
        patchState(store, {
          items: store.items().map((n) => (ids.includes(n.id) ? { ...n, isRead: true } : n)),
        });
        await loadUnreadCount();
      },

      async markAllRead(): Promise<void> {
        await firstValueFrom(api.markRead({}));
        patchState(store, {
          items: store.items().map((n) => ({ ...n, isRead: true })),
          unreadCount: 0,
        });
      },

      async deleteBatch(ids: number[]): Promise<void> {
        await firstValueFrom(api.deleteBatch({ notificationIds: ids }));
        patchState(store, {
          items: store.items().filter((n) => !ids.includes(n.id)),
        });
        await loadUnreadCount();
      },

      async deleteAll(): Promise<void> {
        await firstValueFrom(api.deleteBatch({}));
        patchState(store, { items: [], unreadCount: 0 });
      },
    };
  }),
);
