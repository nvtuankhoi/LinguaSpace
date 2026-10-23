import { inject, Injectable } from '@angular/core';
import { Subject, firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SocialApi } from '../api/social.api';
import { DirectMessageDto } from '../models';

export interface DmRealtimeContext {
  conversationId: number;
  otherUserId: string;
  otherDisplayName: string;
}

/**
 * Direct-message realtime. In mock mode the other participant replies once,
 * a few seconds after you send — mimicking a real 1:1 exchange (no idle
 * spamming). Against the live API this would subscribe to the DirectMessage
 * SignalR event instead.
 */
@Injectable({ providedIn: 'root' })
export class DmRealtimeService {
  private readonly api = inject(SocialApi);

  private readonly message$ = new Subject<DirectMessageDto>();
  readonly onMessage = this.message$.asObservable();

  private ctx: DmRealtimeContext | null = null;
  private replyTimer: ReturnType<typeof setTimeout> | null = null;

  async connect(ctx: DmRealtimeContext): Promise<void> {
    await this.disconnect();
    this.ctx = ctx;
  }

  async send(content: string): Promise<void> {
    const ctx = this.ctx;
    if (!ctx) {
      return;
    }
    const dm = await firstValueFrom(this.api.sendDm({ recipientId: ctx.otherUserId, content }));
    this.message$.next(dm);

    if (environment.useMock) {
      this.scheduleReply();
    }
  }

  async disconnect(): Promise<void> {
    if (this.replyTimer) {
      clearTimeout(this.replyTimer);
      this.replyTimer = null;
    }
    this.ctx = null;
  }

  /** One reply from the other participant, ~6s later (single, non-repeating). */
  private scheduleReply(): void {
    if (this.replyTimer) {
      clearTimeout(this.replyTimer);
    }
    this.replyTimer = setTimeout(() => {
      const ctx = this.ctx;
      this.replyTimer = null;
      if (!ctx) {
        return;
      }
      this.message$.next({
        id: Math.floor(Math.random() * 1_000_000),
        conversationId: ctx.conversationId,
        senderId: ctx.otherUserId,
        content: this.replyLine(),
        sentAt: new Date().toISOString(),
        isRead: true,
        isDeleted: false,
        editedAt: null,
      });
    }, 6_000);
  }

  private replyLine(): string {
    const lines = [
      'Yes, exactly that. 😊',
      'Haha, good one.',
      'Let me try to say it back to you…',
      'ありがとう！That really helped.',
      'Same time tomorrow?',
      'I wrote it down to review later.',
    ];
    return lines[Math.floor(Math.random() * lines.length)];
  }
}
