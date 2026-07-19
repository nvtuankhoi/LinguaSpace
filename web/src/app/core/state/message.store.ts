import { computed, inject } from '@angular/core';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { SocialApi } from '../api/social.api';
import { AuthStore } from '../auth/auth.store';
import { ConversationDto, DirectMessageDto } from '../models';
import { DmRealtimeService } from './dm-realtime.service';

type Status = 'idle' | 'loading' | 'error';

interface MessageState {
  conversations: ConversationDto[];
  active: ConversationDto | null;
  messages: DirectMessageDto[];
  searchResults: DirectMessageDto[];
  searchStatus: Status;
  status: Status;
}

const initialState: MessageState = {
  conversations: [],
  active: null,
  messages: [],
  searchResults: [],
  searchStatus: 'idle',
  status: 'idle',
};

export const MessageStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed(({ conversations }) => ({
    unreadTotal: computed(() => conversations().reduce((sum, c) => sum + (c.unreadCount ?? 0), 0)),
  })),
  withMethods((store, api = inject(SocialApi), realtime = inject(DmRealtimeService), auth = inject(AuthStore)) => {
    const loadConversations = async (): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const res = await firstValueFrom(api.getConversations());
        patchState(store, { conversations: res.items, status: 'idle' });
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    // The DM history endpoint returns pages newest-first (cursor pagination:
    // beforeCursor = older). A chat thread must read oldest→newest top→bottom,
    // so every page is reversed before it enters `messages`. The store therefore
    // always holds ascending order, and appending a live DM to the end is correct.
    const ascending = (items: DirectMessageDto[]): DirectMessageDto[] => [...items].reverse();

    const appendDm = (dm: DirectMessageDto): void => {
      if (store.messages().some((m) => m.id === dm.id)) {
        return;
      }
      patchState(store, { messages: [...store.messages(), dm] });
    };

    return {
      loadConversations,

      async openConversation(id: number): Promise<void> {
        patchState(store, { status: 'loading', active: null, messages: [], searchResults: [], searchStatus: 'idle' });
        try {
          if (!store.conversations().length) {
            await loadConversations();
          }
          const active = store.conversations().find((c) => c.id === id) ?? null;
          const res = await firstValueFrom(api.getMessages(id));
          patchState(store, { active, messages: ascending(res.items), status: 'idle' });
          // Mark as read and clear the badge for this conversation.
          await firstValueFrom(api.markRead(id)).catch(() => undefined);
          patchState(store, {
            conversations: store.conversations().map((c) => (c.id === id ? { ...c, unreadCount: 0 } : c)),
          });
          if (active && active.otherUserDisplayName) {
            await realtime.connect({
              conversationId: id,
              otherUserId: active.otherUserId,
              otherDisplayName: active.otherUserDisplayName,
            });
          }
        } catch {
          patchState(store, { status: 'error' });
        }
      },

      appendMessage: appendDm,

      /** Edits the current user's own DM. Server sets EditedAt; mirror it locally. */
      async editMessage(messageId: number, content: string): Promise<void> {
        const trimmed = content.trim();
        if (!trimmed) {
          return;
        }
        try {
          await firstValueFrom(api.editDm(messageId, { content: trimmed }));
          patchState(store, {
            messages: store.messages().map((m) =>
              m.id === messageId
                ? { ...m, content: trimmed, editedAt: new Date().toISOString() }
                : m,
            ),
          });
        } catch {
          /* surfaced elsewhere */
        }
      },

      /** Soft-deletes the current user's own DM (server sets IsDeleted + content). */
      async deleteMessage(messageId: number): Promise<void> {
        try {
          await firstValueFrom(api.deleteDm(messageId));
          patchState(store, {
            messages: store.messages().map((m) =>
              m.id === messageId ? { ...m, isDeleted: true, content: '[deleted]' } : m,
            ),
          });
        } catch {
          /* surfaced elsewhere */
        }
      },

      /** Searches the open conversation's messages server-side; results replace `searchResults`. */
      async searchConversation(term: string): Promise<void> {
        const active = store.active();
        if (!active) {
          return;
        }
        const q = term.trim();
        if (!q) {
          patchState(store, { searchResults: [], searchStatus: 'idle' });
          return;
        }
        patchState(store, { searchStatus: 'loading' });
        try {
          const res = await firstValueFrom(api.searchMessages(active.id, q));
          patchState(store, { searchResults: res.items, searchStatus: 'idle' });
        } catch {
          patchState(store, { searchStatus: 'error' });
        }
      },

      clearSearch(): void {
        patchState(store, { searchResults: [], searchStatus: 'idle' });
      },

      /**
       * Soft-deletes every message the current user sent in the open conversation
       * (server: ClearMyMessagesCommand). Optimistically drops my messages from the
       * thread; the other participant is live-synced per-message by the server.
       */
      async clearMyMessages(): Promise<void> {
        const active = store.active();
        if (!active) {
          return;
        }
        try {
          await firstValueFrom(api.clearMyMessages(active.id));
          const myId = auth.user()?.userId;
          patchState(store, {
            messages: store.messages().filter((m) => m.senderId !== myId),
          });
        } catch {
          /* surfaced elsewhere */
        }
      },

      /** Applies a live DM edit pushed to the other participant (PresenceHub). */
      applyEdited(messageId: number, content: string, editedAt: string): void {
        patchState(store, {
          messages: store.messages().map((m) =>
            m.id === messageId ? { ...m, content, editedAt } : m,
          ),
        });
      },

      /** Applies a live DM deletion pushed to the other participant (PresenceHub). */
      applyDeleted(messageId: number): void {
        patchState(store, {
          messages: store.messages().map((m) =>
            m.id === messageId ? { ...m, isDeleted: true, content: '[deleted]' } : m,
          ),
        });
      },

      /**
       * Handles a live incoming DM pushed by PresenceHub's "NewDirectMessage".
       * - Open conversation: append to the thread (ascending) and re-mark read so
       *   the badge stays clear while the user is viewing it.
       * - Other conversation: bump its unread badge + last message so the list
       *   updates without a reload. A brand-new conversation (not yet listed) is
       *   handled by reloading the conversation list.
       */
      receiveIncoming(dm: DirectMessageDto): void {
        const active = store.active();
        if (active && active.id === dm.conversationId) {
          appendDm(dm);
          void firstValueFrom(api.markRead(dm.conversationId)).catch(() => undefined);
          return;
        }

        const conversations = store.conversations();
        const exists = conversations.some((c) => c.id === dm.conversationId);
        if (!exists) {
          void loadConversations();
          return;
        }
        patchState(store, {
          conversations: conversations.map((c) =>
            c.id === dm.conversationId
              ? {
                  ...c,
                  lastMessage: dm.content,
                  lastMessageAt: dm.sentAt,
                  unreadCount: (c.unreadCount ?? 0) + 1,
                }
              : c,
          ),
        });
      },

      /**
       * Refetches the open conversation's messages (manual refresh / fallback).
       */
      async refreshActive(): Promise<void> {
        const active = store.active();
        if (!active) {
          return;
        }
        try {
          const res = await firstValueFrom(api.getMessages(active.id));
          patchState(store, { messages: ascending(res.items) });
        } catch {
          /* silent — leave the existing thread as-is */
        }
      },

      async send(content: string): Promise<void> {
        const trimmed = content.trim();
        if (!trimmed) {
          return;
        }
        await realtime.send(trimmed);
      },

      async closeConversation(): Promise<void> {
        await realtime.disconnect();
        patchState(store, { active: null, messages: [], searchResults: [], searchStatus: 'idle' });
      },
    };
  }),
);
