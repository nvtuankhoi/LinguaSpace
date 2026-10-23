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

/** Payload pushed by PresenceHub's "DirectMessageEdited" (other participant edited a DM). */
interface DirectMessageEditedPush {
  id: number;
  conversationId: number;
  content: string;
  editedAt: string;
}

/** Payload pushed by PresenceHub's "DirectMessageDeleted" (other participant deleted a DM). */
interface DirectMessageDeletedPush {
  id: number;
  conversationId: number;
}

/** Live comment pushed to viewers of a post (PresenceHub post-group "NewComment"). */
interface NewCommentPush {
  id: number;
  postId: number;
  authorId: string;
  content: string;
  parentCommentId: number | null;
  createdAt: string;
}

/** Live reaction-count change pushed to viewers (PresenceHub post-group "NewReaction"). */
interface NewReactionPush {
  targetId: number;
  targetType: string; // "Post" | "Comment"
  likeCount: number;
}

/** A comment on the viewed post was edited (PresenceHub post-group "CommentEdited"). */
interface CommentEditedPush {
  id: number;
  postId: number;
  content: string;
}

/** A comment on the viewed post was deleted (PresenceHub post-group "CommentDeleted"). */
interface CommentDeletedPush {
  id: number;
  postId: number;
}

/** The viewed post was edited (PresenceHub post-group "PostEdited"). */
interface PostEditedPush {
  id: number;
  content: string;
  languageCode: string | null;
}

/** The viewed post was deleted (PresenceHub post-group "PostDeleted"). */
interface PostDeletedPush {
  id: number;
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
  private readonly dmEdited$ = new Subject<DirectMessageEditedPush>();
  private readonly dmDeleted$ = new Subject<DirectMessageDeletedPush>();
  private readonly newPost$ = new Subject<{ postId: number; authorId: string }>();
  private readonly newComment$ = new Subject<NewCommentPush>();
  private readonly newReaction$ = new Subject<NewReactionPush>();
  private readonly commentEdited$ = new Subject<CommentEditedPush>();
  private readonly commentDeleted$ = new Subject<CommentDeletedPush>();
  private readonly postEdited$ = new Subject<PostEditedPush>();
  private readonly postDeleted$ = new Subject<PostDeletedPush>();

  /** User ids currently reported online by PresenceHub (UserOnline/UserOffline). */
  private readonly _online = signal<ReadonlySet<string>>(new Set());
  readonly online = this._online.asReadonly();

  readonly onOnlineChange = this.onlineChange$.asObservable();
  readonly onNotification = this.notification$.asObservable();
  /** Live incoming DM push from PresenceHub's "NewDirectMessage" event. */
  readonly onDirectMessage = this.directMessage$.asObservable();
  /** A DM was edited by the other participant (PresenceHub "DirectMessageEdited"). */
  readonly onDmEdited = this.dmEdited$.asObservable();
  /** A DM was deleted by the other participant (PresenceHub "DirectMessageDeleted"). */
  readonly onDmDeleted = this.dmDeleted$.asObservable();
  /** New post from a followed user (PostCreatedEventHandler fans "NewPost" to followers). */
  readonly onNewPost = this.newPost$.asObservable();
  /** Live comment on a post being viewed (post-group "NewComment"). */
  readonly onNewComment = this.newComment$.asObservable();
  /** Live reaction-count change on a viewed post/comment (post-group "NewReaction"). */
  readonly onNewReaction = this.newReaction$.asObservable();
  /** A comment on a viewed post was edited (post-group "CommentEdited"). */
  readonly onCommentEdited = this.commentEdited$.asObservable();
  /** A comment on a viewed post was deleted (post-group "CommentDeleted"). */
  readonly onCommentDeleted = this.commentDeleted$.asObservable();
  /** A viewed post was edited (post-group "PostEdited"). */
  readonly onPostEdited = this.postEdited$.asObservable();
  /** A viewed post was deleted (post-group "PostDeleted"). */
  readonly onPostDeleted = this.postDeleted$.asObservable();

  private hub: HubConnection | null = null;
  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;

  isOnline(userId: string): boolean {
    return this._online().has(userId);
  }

  /** Subscribe to live updates for a feed post (call when viewing it). */
  async joinPostGroup(postId: number): Promise<void> {
    try {
      await this.hub?.invoke('JoinPostGroup', postId);
    } catch (err) {
      console.error('[presence] joinPostGroup failed', err);
    }
  }

  /** Stop receiving live updates for a feed post (call on navigation away). */
  async leavePostGroup(postId: number): Promise<void> {
    try {
      await this.hub?.invoke('LeavePostGroup', postId);
    } catch {
      /* ignore — leaving is best-effort */
    }
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
    hub.on('DirectMessageEdited', (ev: DirectMessageEditedPush) => this.dmEdited$.next(ev));
    hub.on('DirectMessageDeleted', (ev: DirectMessageDeletedPush) => this.dmDeleted$.next(ev));
    // New posts from followed users → live "following" feed.
    hub.on('NewPost', (ev: { postId: number; authorId: string }) => this.newPost$.next(ev));

    // Feed post-group events (a client only receives these after JoinPostGroup).
    // Note: the backend also sends author-targeted "NewComment"/"NewReaction"
    // pings with a different (incomplete) shape; ignore those and keep only the
    // well-formed post-group broadcasts.
    hub.on('NewComment', (ev: NewCommentPush) => {
      if (typeof ev?.id !== 'number' || typeof ev?.content !== 'string') return;
      this.newComment$.next(ev);
    });
    hub.on('NewReaction', (ev: NewReactionPush) => {
      if (typeof ev?.likeCount !== 'number') return;
      this.newReaction$.next(ev);
    });
    hub.on('CommentEdited', (ev: CommentEditedPush) => this.commentEdited$.next(ev));
    hub.on('CommentDeleted', (ev: CommentDeletedPush) => this.commentDeleted$.next(ev));
    hub.on('PostEdited', (ev: PostEditedPush) => this.postEdited$.next(ev));
    hub.on('PostDeleted', (ev: PostDeletedPush) => this.postDeleted$.next(ev));

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
