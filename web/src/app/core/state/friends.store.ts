import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { UsersApi } from '../api/users.api';
import { FriendRequestDto, UserSummaryDto } from '../models';
import { AuthStore } from '../auth/auth.store';

type Status = 'idle' | 'loading' | 'error';

interface FriendsState {
  friends: UserSummaryDto[];
  followers: UserSummaryDto[];
  following: UserSummaryDto[];
  friendRequests: FriendRequestDto[];
  blocked: UserSummaryDto[];
  status: Status;
}

const initialState: FriendsState = {
  friends: [],
  followers: [],
  following: [],
  friendRequests: [],
  blocked: [],
  status: 'idle',
};

export const FriendsStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => {
    const auth = inject(AuthStore);
    return {
      /** Requests addressed to me (I can accept/decline). */
      incomingRequests: computed(() => {
        const me = auth.user()?.userId;
        return me ? store.friendRequests().filter((r) => r.addresseeId === me) : [];
      }),
      /** Requests I sent that are still pending (I can cancel). */
      outgoingRequests: computed(() => {
        const me = auth.user()?.userId;
        return me ? store.friendRequests().filter((r) => r.requesterId === me) : [];
      }),
    };
  }),
  withMethods((store, api = inject(UsersApi), auth = inject(AuthStore)) => {
    return {
      async loadAll(): Promise<void> {
        patchState(store, { status: 'loading' });
        try {
          const userId = auth.user()?.userId;
          if (!userId) {
            patchState(store, { status: 'idle' });
            return;
          }
          const [friends, followers, following, requests, blocked] = await Promise.all([
            firstValueFrom(api.getFriends(userId)),
            firstValueFrom(api.getFollowers(userId)),
            firstValueFrom(api.getFollowing(userId)),
            firstValueFrom(api.getFriendRequests()),
            firstValueFrom(api.getBlockedUsers()),
          ]);
          patchState(store, {
            friends,
            followers,
            following,
            friendRequests: requests,
            blocked,
            status: 'idle',
          });
        } catch {
          patchState(store, { status: 'error' });
        }
      },

      async respondToRequest(id: number, accept: boolean): Promise<void> {
        try {
          await firstValueFrom(api.respondFriendRequest(id, { accept }));
          patchState(store, {
            friendRequests: store.friendRequests().filter((r) => r.id !== id),
          });
          if (accept) {
            // Optimistically reload friends
            const userId = auth.user()?.userId;
            if (userId) {
              const friends = await firstValueFrom(api.getFriends(userId));
              patchState(store, { friends });
            }
          }
        } catch (e) {
          console.error(e);
        }
      },
      
      async removeFriend(userId: string): Promise<void> {
        try {
          await firstValueFrom(api.removeFriend(userId));
          patchState(store, {
            friends: store.friends().filter(f => f.userId !== userId)
          });
        } catch (e) {
          console.error(e);
        }
      },

      async unfollow(userId: string): Promise<void> {
        try {
          await firstValueFrom(api.unfollowUser(userId));
          patchState(store, {
            following: store.following().filter(f => f.userId !== userId)
          });
        } catch (e) {
          console.error(e);
        }
      },

      async cancelRequest(id: number): Promise<void> {
        try {
          await firstValueFrom(api.cancelFriendRequest(id));
          patchState(store, {
            friendRequests: store.friendRequests().filter((r) => r.id !== id),
          });
        } catch (e) {
          console.error(e);
        }
      },

      async unblock(userId: string): Promise<void> {
        try {
          await firstValueFrom(api.unblockUser(userId));
          patchState(store, {
            blocked: store.blocked().filter((u) => u.userId !== userId),
          });
        } catch (e) {
          console.error(e);
        }
      }
    };
  }),
);
