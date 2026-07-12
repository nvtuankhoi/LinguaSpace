import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { RoomsApi } from '../api/rooms.api';
import {
  CreateRoomRequest,
  MessageDto,
  RoomDto,
  RoomSummaryDto,
  UpdateRoomRequest,
} from '../models';
import { RoomMessageEvent, RoomRealtimeService } from './room-realtime.service';

type Status = 'idle' | 'loading' | 'error';

interface RoomState {
  rooms: RoomSummaryDto[];
  current: RoomDto | null;
  messages: MessageDto[];
  status: Status;
}

const initialState: RoomState = { rooms: [], current: null, messages: [], status: 'idle' };

export const RoomStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, roomsApi = inject(RoomsApi), realtime = inject(RoomRealtimeService)) => {
    const loadRooms = async (): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const res = await firstValueFrom(roomsApi.getRooms());
        patchState(store, { rooms: res.items, status: 'idle' });
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    const refreshCurrent = async (): Promise<void> => {
      const id = store.current()?.id;
      if (id == null) {
        return;
      }
      try {
        const room = await firstValueFrom(roomsApi.getRoom(id));
        patchState(store, { current: room });
      } catch {
        /* leave the stale room in place on refresh failure */
      }
    };

    return {
      loadRooms,

      /** Re-fetch the current room so the participant list/count stays live
       *  on UserJoinedRoom/UserLeftRoom pushes. */
      async refreshParticipants(): Promise<void> {
        await refreshCurrent();
      },

      async openRoom(id: number): Promise<void> {
        patchState(store, { status: 'loading', current: null, messages: [] });
        try {
          const room = await firstValueFrom(roomsApi.getRoom(id));
          const messages = await firstValueFrom(roomsApi.getMessages(id));
          patchState(store, { current: room, messages, status: 'idle' });
          await realtime.connect(id);
          // Register membership server-side (drives the participant list and
          // AwardXpForRoomJoin). Non-fatal: the room still opens if this fails.
          await firstValueFrom(roomsApi.join(id)).catch(() => undefined);
        } catch {
          patchState(store, { status: 'error' });
        }
      },

      appendMessage(ev: RoomMessageEvent): void {
        if (store.messages().some((m) => m.id === ev.messageId)) {
          return;
        }
        // RoomHub's ReceiveMessage payload has no senderDisplayName — resolve it
        // from the room's participants, falling back to the raw userId.
        const senderDisplayName =
          ev.senderDisplayName ??
          store.current()?.participants.find((p) => p.userId === ev.senderId)?.displayName ??
          ev.senderId;
        patchState(store, {
          messages: [
            ...store.messages(),
            { id: ev.messageId, senderId: ev.senderId, senderDisplayName, content: ev.content, type: 'Text', sentAt: ev.sentAt, isDeleted: false },
          ],
        });
      },

      /** Soft-deletes a room message (backend allows owner OR host). */
      async deleteMessage(messageId: number): Promise<void> {
        const roomId = store.current()?.id;
        if (roomId == null) {
          return;
        }
        try {
          await firstValueFrom(roomsApi.deleteMessage(roomId, messageId));
          patchState(store, {
            messages: store.messages().map((m) =>
              m.id === messageId ? { ...m, isDeleted: true, content: '' } : m,
            ),
          });
        } catch {
          /* surfaced elsewhere */
        }
      },

      async send(content: string): Promise<void> {
        const room = store.current();
        if (!room || !content.trim()) {
          return;
        }
        await realtime.send(room.id, content.trim());
      },

      async closeRoom(): Promise<void> {
        const id = store.current()?.id;
        await realtime.disconnect();
        // Tell the server we left (clears membership). Non-fatal on failure.
        if (id != null) {
          await firstValueFrom(roomsApi.leave(id)).catch(() => undefined);
        }
        patchState(store, { current: null, messages: [] });
      },

      async createRoom(req: CreateRoomRequest): Promise<number> {
        const roomId = await firstValueFrom(roomsApi.createRoom(req));
        await loadRooms();
        return roomId;
      },

      // ---- Host controls ----

      async updateRoom(req: UpdateRoomRequest): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.updateRoom(id, req));
        await refreshCurrent();
      },

      async deleteRoom(): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.deleteRoom(id));
        await realtime.disconnect();
        patchState(store, { current: null, messages: [] });
      },

      async transferHost(targetUserId: string): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.transferHost(id, targetUserId));
        await refreshCurrent();
      },

      async kickParticipant(targetUserId: string): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.kick(id, targetUserId));
        await refreshCurrent();
      },

      /** Toggle a participant's text-chat mute (host moderation). */
      async muteParticipant(targetUserId: string, mute: boolean): Promise<void> {
        const room = store.current();
        if (!room) {
          return;
        }
        await firstValueFrom(roomsApi.mute(room.id, targetUserId, { mute }));
        patchState(store, {
          current: {
            ...room,
            participants: room.participants.map((p) =>
              p.userId === targetUserId ? { ...p, isMuted: mute } : p,
            ),
          },
        });
      },

      /** Send a room invite to a user who isn't in the room yet. */
      async invite(targetUserId: string): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.invite(id, targetUserId));
      },
    };
  }),
);
