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
    await this.hub.start();
    await this.hub.invoke('JoinRoomGroup', roomId);
  }

  async send(roomId: number, content: string): Promise<void> {
    if (environment.useMock) {
      const res = await firstValueFrom(this.api.sendMessage(roomId, { content }));
      this.message$.next({
        messageId: res.messageId,
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
