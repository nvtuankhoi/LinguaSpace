import { inject, Injectable } from '@angular/core';
import { Subject, firstValueFrom } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import { environment } from '../../../environments/environment';
import { RoomsApi } from '../api/rooms.api';
import { AuthStore } from '../auth/auth.store';
import { TokenService } from '../auth/token.service';

export interface RoomMessageEvent {
  messageId: number;
  senderId: string;
  /**
   * NOT included in the RoomHub `ReceiveMessage` payload
   * ({ messageId, senderId, content, sentAt }). Resolved client-side from the
   * room participants in RoomStore.appendMessage; present when emitted locally
   * (own sends / mock bots).
   */
  senderDisplayName?: string;
  content: string;
  sentAt: string;
}

/** A participant joined or left the connected room (UserJoinedRoom/UserLeftRoom). */
export interface RoomParticipantEvent {
  roomId: number;
  userId: string;
  joined: boolean;
}

/** A participant's text-chat mute state changed (ParticipantMuted). */
export interface RoomMuteEvent {
  roomId: number;
  userId: string;
  isMuted: boolean;
}

/** A room message was deleted (MessageDeleted). */
export interface RoomMessageDeletedEvent {
  roomId: number;
  messageId: number;
}

/** A participant joined or left the room's voice/video media session. */
export interface RoomMediaEvent {
  roomId: number;
  userId: string;
}

const BOT_NAMES = ['Sora', 'Marco', 'Lena', 'Jin'];
const BOT_LINES = [
  'Has anyone tried shadowing for pronunciation?',
  'I keep mixing these up 😅',
  'Let me try a full sentence…',
  'Good point, I did not think of that.',
  'Can someone correct my intonation here?',
  'This is fun — let’s do it again next week.',
];

/**
 * Room realtime. In mock mode it simulates live chat (persisted via REST +
 * occasional "other participant" messages). Against the live API it connects
 * to the RoomHub SignalR endpoint and listens for ReceiveMessage.
 */
@Injectable({ providedIn: 'root' })
export class RoomRealtimeService {
  private readonly api = inject(RoomsApi);
  private readonly auth = inject(AuthStore);
  private readonly tokens = inject(TokenService);

  private readonly message$ = new Subject<RoomMessageEvent>();
  readonly onMessage = this.message$.asObservable();

  private readonly participantChange$ = new Subject<RoomParticipantEvent>();
  /** Fires when a user joins or leaves the connected room. */
  readonly onParticipantChange = this.participantChange$.asObservable();

  private readonly muteChange$ = new Subject<RoomMuteEvent>();
  /** Fires when a participant's mute state changes in the connected room. */
  readonly onMuteChange = this.muteChange$.asObservable();

  private readonly messageDeleted$ = new Subject<RoomMessageDeletedEvent>();
  /** Fires when a room message is deleted in the connected room. */
  readonly onMessageDeleted = this.messageDeleted$.asObservable();

  private readonly mediaJoin$ = new Subject<RoomMediaEvent>();
  /** Fires when a participant joins the room's voice/video session. */
  readonly onMediaJoin = this.mediaJoin$.asObservable();

  private readonly mediaLeave$ = new Subject<RoomMediaEvent>();
  /** Fires when a participant leaves the room's voice/video session. */
  readonly onMediaLeave = this.mediaLeave$.asObservable();

  private roomId: number | null = null;
  private botTimer: ReturnType<typeof setInterval> | null = null;
  private hub: HubConnection | null = null;

  async connect(roomId: number): Promise<void> {
    await this.disconnect();
    this.roomId = roomId;

    if (environment.useMock) {
      this.startBots();
      return;
    }

    this.hub = new HubConnectionBuilder()
      .withUrl(environment.hubs.room, { accessTokenFactory: () => this.tokens.value ?? '' })
      .withAutomaticReconnect()
      .build();
    // The hub sends a single object ({ messageId, senderId, content, sentAt });
    // senderDisplayName is resolved later from the room participants.
    this.hub.on('ReceiveMessage', (ev: RoomMessageEvent) => this.message$.next(ev));
    // Participant membership changes (join/leave) — the store refreshes the
    // room so the participant list + count update without a manual reload.
    this.hub.on('UserJoinedRoom', (ev: { roomId: number; userId: string }) =>
      this.participantChange$.next({ ...ev, joined: true }),
    );
    this.hub.on('UserLeftRoom', (ev: { roomId: number; userId: string }) =>
      this.participantChange$.next({ ...ev, joined: false }),
    );
    // Participant mute changes — applied locally so the host's toggle and the
    // affected user's composer both react without a full room refetch.
    this.hub.on('ParticipantMuted', (ev: RoomMuteEvent) => this.muteChange$.next(ev));
    // Message deletions — applied locally so every participant's chat reflects
    // soft-deletes without a manual reload.
    this.hub.on('MessageDeleted', (ev: RoomMessageDeletedEvent) => this.messageDeleted$.next(ev));
    // Media-session presence — who's in the voice/video call. Emitted server-side
    // from the LiveKit webhook; lets non-AV participants see who's in the call.
    this.hub.on('UserJoinedMedia', (ev: RoomMediaEvent) => this.mediaJoin$.next(ev));
    this.hub.on('UserLeftMedia', (ev: RoomMediaEvent) => this.mediaLeave$.next(ev));
    await this.hub.start();
    await this.hub.invoke('JoinRoomGroup', roomId);
  }

  async send(roomId: number, content: string): Promise<void> {
    if (environment.useMock) {
      const messageId = await firstValueFrom(this.api.sendMessage(roomId, { content }));
      this.message$.next({
        messageId,
        senderId: this.auth.user()?.userId ?? 'me',
        senderDisplayName: this.auth.user()?.displayName ?? 'You',
        content,
        sentAt: new Date().toISOString(),
      });
      return;
    }
    await this.hub?.invoke('SendMessage', roomId, content);
  }

  async disconnect(): Promise<void> {
    if (this.botTimer) {
      clearInterval(this.botTimer);
      this.botTimer = null;
    }
    if (this.hub) {
      try {
        await this.hub.stop();
      } catch {
        /* ignore */
      }
      this.hub = null;
    }
    this.roomId = null;
  }

  private startBots(): void {
    this.botTimer = setInterval(() => {
      if (this.roomId == null) {
        return;
      }
      const name = BOT_NAMES[Math.floor(Math.random() * BOT_NAMES.length)];
      const line = BOT_LINES[Math.floor(Math.random() * BOT_LINES.length)];
      this.message$.next({
        messageId: Math.floor(Math.random() * 1_000_000),
        senderId: 'bot',
        senderDisplayName: name,
        content: line,
        sentAt: new Date().toISOString(),
      });
    }, 18_000);
  }
}
