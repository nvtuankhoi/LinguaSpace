import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { MessageStore } from '../../core/state/message.store';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { relativeTime } from '../../core/util/time';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';

@Component({
  selector: 'app-messages-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, IconComponent],
  templateUrl: './messages-list.component.html',
  styleUrl: './messages-list.component.scss',
})
export class MessagesListComponent {
  protected readonly store = inject(MessageStore);
  private readonly router = inject(Router);
  private readonly presence = inject(PresenceRealtimeService);

  protected readonly conversations = this.store.conversations;
  protected readonly status = this.store.status;

  protected isOnline(userId: string): boolean {
    return this.presence.isOnline(userId);
  }

  constructor() {
    void this.store.loadConversations();
  }

  open(id: number): void {
    void this.router.navigate(['/app/messages', id]);
  }

  protected time(iso: string | null): string {
    return iso ? relativeTime(iso) : '';
  }
}
