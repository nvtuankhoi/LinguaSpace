import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

import { NotificationStore } from '../../core/state/notification.store';
import { FriendsStore } from '../../core/state/friends.store';
import { relativeTime } from '../../core/util/time';
import { NotificationDto, NotificationType } from '../../core/models';
import { IconComponent } from '../../shared/ui/icon/icon.component';

interface NotificationMeta {
  icon: string;
  text: string;
  route: string | null;
}

@Component({
  selector: 'app-notifications',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent {
  private readonly store = inject(NotificationStore);
  private readonly friends = inject(FriendsStore);
  private readonly router = inject(Router);

  protected readonly items = this.store.items;
  protected readonly status = this.store.status;
  protected readonly unreadCount = this.store.unreadCount;

  constructor() {
    void this.store.load();
  }

  protected meta(n: NotificationDto): NotificationMeta {
    const p = n.payload ?? {};
    switch (n.type) {
      case 'FriendRequest':
        return {
          icon: 'user-plus',
          text: `${p['requesterDisplayName']} sent you a friend request`,
          route: null,
        };
      case 'FriendAccepted':
        return {
          icon: 'user-check',
          text: `${p['acceptorDisplayName']} accepted your friend request`,
          route: null,
        };
      case 'NewFollower':
        return {
          icon: 'profile',
          text: `${p['followerDisplayName']} started following you`,
          route: null,
        };
      case 'RoomInvite':
        return {
          icon: 'rooms',
          text: `${p['inviterDisplayName']} invited you to "${p['roomTitle']}"`,
          route: `/app/rooms/${p['roomId']}`,
        };
      case 'PostLike':
        return {
          icon: 'heart-filled',
          text: `${p['likerDisplayName']} liked your post`,
          route: '/app/feed',
        };
      case 'PostComment':
        return {
          icon: 'messages',
          text: `${p['commenterDisplayName']} commented: "${p['commentPreview']}"`,
          route: '/app/feed',
        };
      case 'CommentLike':
        return {
          icon: 'heart',
          text: `${p['likerDisplayName']} liked your comment`,
          route: '/app/feed',
        };
      case 'DirectMessage':
        return {
          icon: 'messages',
          text: `${p['senderDisplayName']}: "${p['messagePreview']}"`,
          route: `/app/messages/${p['conversationId']}`,
        };
      case 'BadgeEarned':
        return {
          icon: 'award',
          text: `You earned the "${p['badgeName']}" badge`,
          route: '/app/profile',
        };
      case 'SystemMessage':
        return {
          icon: 'info',
          text: `${p['message']}`,
          route: (p['actionUrl'] as string) ?? null,
        };
      default:
        return {
          icon: 'notifications',
          text: 'New notification',
          route: null,
        };
    }
  }

  protected time(iso: string): string {
    return relativeTime(iso);
  }

  protected markAllRead(): void {
    void this.store.markAllRead();
  }

  protected clearAll(): void {
    const ids = this.items().map(n => n.id);
    if (ids.length) {
      void this.store.deleteBatch(ids);
    }
  }

  protected async acceptFriend(event: Event, n: NotificationDto): Promise<void> {
    event.stopPropagation();
    const requestId = this.friendRequestId(n);
    if (requestId != null) {
      await this.friends.respondToRequest(requestId, true);
    }
    void this.store.deleteBatch([n.id]);
  }

  protected async declineFriend(event: Event, n: NotificationDto): Promise<void> {
    event.stopPropagation();
    const requestId = this.friendRequestId(n);
    if (requestId != null) {
      await this.friends.respondToRequest(requestId, false);
    }
    void this.store.deleteBatch([n.id]);
  }

  private friendRequestId(n: NotificationDto): number | null {
    const raw = n.payload?.['requestId'];
    const id = typeof raw === 'number' ? raw : Number(raw);
    return Number.isFinite(id) ? id : null;
  }

  protected onClick(n: NotificationDto): void {
    if (!n.isRead) {
      void this.store.markRead([n.id]);
    }
    const m = this.meta(n);
    if (m.route) {
      void this.router.navigate([m.route]);
    }
  }

  protected deleteOne(event: Event, n: NotificationDto): void {
    event.stopPropagation();
    void this.store.deleteBatch([n.id]);
  }

  protected retry(): void {
    void this.store.load();
  }
}
