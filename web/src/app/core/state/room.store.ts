import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { RoomsApi } from '../api/rooms.api';
import {
  CreateRoomRequest,
  MessageDto,
  RoomDto,
  RoomMediaParticipantDto,
  RoomSummaryDto,
  UpdateRoomRequest,
} from '../models';
import { RoomMessageEvent, RoomRealtimeService } from './room-realtime.service';

type Status = 'idle' | 'loading' | 'error';

interface RoomState {
  rooms: RoomSummaryDto[];
  current: RoomDto | null;
  messages: MessageDto[];
  /** userIds currently in the room's voice/video media session. */
  mediaParticipantIds: string[];
  status: Status;
}

const initialState: RoomState = { rooms: [], current: null, messages: [], mediaParticipantIds: [], status: 'idle' };

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

    /** Load rooms the current user participates in (GET /Rooms/mine). */
    const loadMyRooms = async (): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const res = await firstValueFrom(roomsApi.getMine());
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
      loadMyRooms,

      /** Re-fetch the current room so the participant list/count stays live
       *  on UserJoinedRoom/UserLeftRoom pushes. */
      async refreshParticipants(): Promise<void> {
        await refreshCurrent();
      },

      async openRoom(id: number): Promise<void> {
        patchState(store, { status: 'loading', current: null, messages: [], mediaParticipantIds: [] });
        try {
          const room = await firstValueFrom(roomsApi.getRoom(id));
          const messages = await firstValueFrom(roomsApi.getMessages(id));
          // Seed who's currently in the voice/video call so non-AV participants
          // see the in-call set immediately, then keep it live via UserJoined/
          // UserLeftMedia. Non-fatal: media presence is best-effort.
          const media = await firstValueFrom(roomsApi.getMediaParticipants(id)).catch(
            () => [] as RoomMediaParticipantDto[],
          );
          patchState(store, {
            current: room,
            messages,
            mediaParticipantIds: media.map((m) => m.userId),
            status: 'idle',
          });
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
        patchState(store, { current: null, messages: [], mediaParticipantIds: [] });
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
        patchState(store, { current: null, messages: [], mediaParticipantIds: [] });
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

      /** Apply a participant mute change pushed from the server (realtime). */
      applyMute(targetUserId: string, isMuted: boolean): void {
        const room = store.current();
        if (!room) {
          return;
        }
        patchState(store, {
          current: {
            ...room,
            participants: room.participants.map((p) =>
              p.userId === targetUserId ? { ...p, isMuted } : p,
            ),
          },
        });
      },

      /** Apply a room-message deletion pushed from the server (realtime). */
      applyMessageDeleted(messageId: number): void {
        patchState(store, {
          messages: store.messages().map((m) =>
            m.id === messageId ? { ...m, isDeleted: true, content: '' } : m,
          ),
        });
      },

      /** A participant joined the voice/video call (UserJoinedMedia). */
      applyMediaJoin(userId: string): void {
        if (store.mediaParticipantIds().includes(userId)) {
          return;
        }
        patchState(store, { mediaParticipantIds: [...store.mediaParticipantIds(), userId] });
      },

      /** A participant left the voice/video call (UserLeftMedia). */
      applyMediaLeave(userId: string): void {
        patchState(store, {
          mediaParticipantIds: store.mediaParticipantIds().filter((u) => u !== userId),
        });
      },

      /**
       * Host-only: end the room's voice/video session for everyone. The backend
       * terminates the LiveKit room (disconnecting all participants) and returns
       * 204, or 403 if the caller isn't the host. Reconcile the in-call set via
       * the status endpoint: LeftAt is set asynchronously by LiveKit webhooks,
       * so status may briefly still report active — in that case leave the set
       * for realtime (UserLeftMedia) to reconcile rather than flashing to zero.
       */
      async endCall(): Promise<void> {
        const id = store.current()?.id;
        if (id == null) {
          return;
        }
        await firstValueFrom(roomsApi.endMediaSession(id));
        try {
          const status = await firstValueFrom(roomsApi.getMediaStatus(id));
          if (!status.isActive) {
            patchState(store, { mediaParticipantIds: [] });
          }
        } catch {
          // Status query failed (e.g. 404) — the session is gone; clear locally.
          patchState(store, { mediaParticipantIds: [] });
        }
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
