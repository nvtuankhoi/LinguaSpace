import { inject, Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { TokenService } from '../auth/token.service';
import { DirectMessageDto, NotificationDto } from '../models';

/**
 * Payload pushed by the backend's PresenceHub "Notification" event
 * (NotificationCreatedEventHandler → INotificationService.NotifyAsync).
 * isRead is implicit (a freshly created notification is unread).
 */
interface NotificationPush {
  id: number;
  type: NotificationDto['type'];
  payload: Record<string, unknown> | null;
  createdAt: string;
}

/**
 * Payload pushed by PresenceHub's "NewDirectMessage" event
 * (SendDmCommandHandler → INotificationService.NotifyAsync(recipient, "NewDirectMessage", …)).
 * SignalR uses camelCase (same default as the RoomHub `ReceiveMessage` payload),
 * so `Id/ConversationId/SenderId/Content/SentAt` arrive lower-cased. The DM DTO
 * fields `isRead/isDeleted/editedAt` aren't part of the push and are filled in
 * by the consumer.
 */
interface DirectMessagePush {
  id: number;
  conversationId: number;
  senderId: string;
  content: string;
  sentAt: string;
}

/** Renews the Redis presence TTL (PresenceHub sets it to 10 min). */
const HEARTBEAT_INTERVAL_MS = 3 * 60 * 1000;

/**
 * Presence + realtime-notification client.
 *
 * Connects to PresenceHub once the session is authenticated, sends a Heartbeat
 * every 3 minutes to keep the server's presence key alive, and surfaces the
 * user-targeted "Notification" push and UserOnline/UserOffline presence changes.
 *
 * No-op in mock mode (environment.useMock) — realtime requires the live API.
 */
@Injectable({ providedIn: 'root' })
export class PresenceRealtimeService {
  private readonly tokens = inject(TokenService);

  private readonly onlineChange$ = new Subject<{ userId: string; online: boolean }>();
  private readonly notification$ = new Subject<NotificationDto>();
  private readonly directMessage$ = new Subject<DirectMessageDto>();
  private readonly newPost$ = new Subject<{ postId: number; authorId: string }>();

  /** User ids currently reported online by PresenceHub (UserOnline/UserOffline). */
  private readonly _online = signal<ReadonlySet<string>>(new Set());
  readonly online = this._online.asReadonly();

  readonly onOnlineChange = this.onlineChange$.asObservable();
  readonly onNotification = this.notification$.asObservable();
  /** Live incoming DM push from PresenceHub's "NewDirectMessage" event. */
  readonly onDirectMessage = this.directMessage$.asObservable();
  /** New post from a followed user (PostCreatedEventHandler fans "NewPost" to followers). */
  readonly onNewPost = this.newPost$.asObservable();

  private hub: HubConnection | null = null;
  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;

  isOnline(userId: string): boolean {
    return this._online().has(userId);
  }

  async connect(): Promise<void> {
    if (this.hub || environment.useMock) {
      return;
    }

    const hub = new HubConnectionBuilder()
      .withUrl(environment.hubs.presence, { accessTokenFactory: () => this.tokens.value ?? '' })
      .withAutomaticReconnect()
      .build();

    hub.on('UserOnline', (userId: string) => {
      this._online.update((set) => addId(set, userId));
      this.onlineChange$.next({ userId, online: true });
    });
    hub.on('UserOffline', (userId: string) => {
      this._online.update((set) => removeId(set, userId));
      this.onlineChange$.next({ userId, online: false });
    });
    hub.on('Notification', (ev: NotificationPush) =>
      this.notification$.next({ ...ev, isRead: false }),
    );
    // Incoming DMs are pushed user-targeted as "NewDirectMessage" (not via the
    // generic "Notification" event), so they need their own handler. The push
    // carries the message minus isRead/isDeleted/editedAt — fill those in so the
    // store can treat it like any other DirectMessageDto.
    hub.on('NewDirectMessage', (ev: DirectMessagePush) =>
      this.directMessage$.next({ ...ev, isRead: false, isDeleted: false, editedAt: null }),
    );
    // New posts from followed users → live "following" feed.
    hub.on('NewPost', (ev: { postId: number; authorId: string }) => this.newPost$.next(ev));

    try {
      await hub.start();
      this.hub = hub;
      this.heartbeatTimer = setInterval(() => void this.invokeHeartbeat(), HEARTBEAT_INTERVAL_MS);
    } catch (err) {
      console.error('[presence] connect failed', err);
    }
  }

  private async invokeHeartbeat(): Promise<void> {
    try {
      await this.hub?.invoke('Heartbeat');
    } catch {
      /* withAutomaticReconnect retries; ignore transient failures */
    }
  }

  async disconnect(): Promise<void> {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
    if (this.hub) {
      try {
        await this.hub.stop();
      } catch {
        /* ignore */
      }
      this.hub = null;
    }
    this._online.set(new Set());
  }
}

/** Immutable set helpers (signals require new references to trigger updates). */
function addId(set: ReadonlySet<string>, id: string): ReadonlySet<string> {
  if (set.has(id)) {
    return set;
  }
  return new Set(set).add(id);
}

function removeId(set: ReadonlySet<string>, id: string): ReadonlySet<string> {
  if (!set.has(id)) {
    return set;
  }
  const next = new Set(set);
  next.delete(id);
  return next;
}
