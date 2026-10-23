import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { UsersApi } from '../../core/api/users.api';
import { GamificationApi } from '../../core/api/gamification.api';
import { FeedApi } from '../../core/api/feed.api';
import { SocialApi } from '../../core/api/social.api';
import { BadgeDto, PostSummaryDto, UserProfileDto, XpSummaryDto } from '../../core/models';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PostCardComponent } from '../feed/post-card.component';
import { patchCommentDelta, patchPostDelete, patchPostEdit, patchPostReaction } from '../feed/live-post-patch';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { AuthStore } from '../../core/auth/auth.store';
import { ReportDialogComponent } from '../../shared/ui/report-dialog/report-dialog.component';

@Component({
  selector: 'app-public-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, IconComponent, PostCardComponent, ReportDialogComponent],
  templateUrl: './public-profile.component.html',
  styleUrl: './public-profile.component.scss',
})
export class PublicProfileComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly usersApi = inject(UsersApi);
  private readonly gamificationApi = inject(GamificationApi);
  private readonly feedApi = inject(FeedApi);
  private readonly socialApi = inject(SocialApi);
  private readonly authStore = inject(AuthStore);
  private readonly presence = inject(PresenceRealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  /** Post groups currently joined so the profile reflects live edits/deletes/reactions. */
  private readonly joined = new Set<number>();

  protected readonly profile = signal<UserProfileDto | null>(null);
  protected readonly xp = signal<XpSummaryDto | null>(null);
  protected readonly badges = signal<BadgeDto[]>([]);
  protected readonly posts = signal<PostSummaryDto[]>([]);
  protected readonly status = signal<'idle' | 'loading' | 'error'>('idle');
  protected readonly showReport = signal(false);

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(async (params) => {
      const id = params.get('userId');
      if (!id) return;
      if (id === this.authStore.user()?.userId) {
        // If it's my own profile, redirect to my private profile page
        void this.router.navigate(['/app/profile'], { replaceUrl: true });
        return;
      }
      await this.load(id);
    });

    // Join each loaded post's group so the profile reflects live
    // edits/deletes/reactions/comment-counts (mirrors the feed list). Each id
    // joins once; all are left on destroy.
    effect(() => {
      for (const p of this.posts()) {
        if (!this.joined.has(p.id)) {
          this.joined.add(p.id);
          void this.presence.joinPostGroup(p.id);
        }
      }
    });

    this.presence.onPostEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.posts.update((items) => patchPostEdit(items, ev.id, ev.content, ev.languageCode));
    });
    this.presence.onPostDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.posts.update((items) => patchPostDelete(items, ev.id));
    });
    this.presence.onNewReaction.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.targetType === 'Post') {
        this.posts.update((items) => patchPostReaction(items, ev.targetId, ev.likeCount));
      }
    });
    this.presence.onNewComment.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.posts.update((items) => patchCommentDelta(items, ev.postId, +1));
    });
    this.presence.onCommentDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.posts.update((items) => patchCommentDelta(items, ev.postId, -1));
    });

    this.destroyRef.onDestroy(() => {
      for (const id of this.joined) {
        void this.presence.leavePostGroup(id);
      }
      this.joined.clear();
    });
  }

  private async load(userId: string): Promise<void> {
    this.status.set('loading');
    try {
      const [p, x, b, f] = await Promise.all([
        firstValueFrom(this.usersApi.getUser(userId)),
        firstValueFrom(this.gamificationApi.getUserXp(userId)).catch(() => null),
        firstValueFrom(this.gamificationApi.getUserBadges(userId)).catch(() => []),
        firstValueFrom(this.feedApi.getUserPosts(userId)).catch(() => ({ items: [] })),
      ]);
      this.profile.set(p);
      this.xp.set(x);
      this.badges.set(b);
      this.posts.set(f.items);
      this.status.set('idle');
    } catch {
      this.status.set('error');
    }
  }

  async follow(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, isFollowedByMe: true, followerCount: p.followerCount + 1 });
    await firstValueFrom(this.usersApi.followUser(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async unfollow(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, isFollowedByMe: false, followerCount: p.followerCount - 1 });
    await firstValueFrom(this.usersApi.unfollowUser(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async addFriend(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, hasOutgoingFriendRequest: true });
    await firstValueFrom(this.usersApi.sendFriendRequest(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async removeFriend(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, isFriend: false, friendCount: p.friendCount - 1 });
    await firstValueFrom(this.usersApi.removeFriend(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async blockUser(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, isBlockedByMe: true, isFriend: false, isFollowedByMe: false });
    await firstValueFrom(this.usersApi.blockUser(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async unblockUser(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    this.profile.set({ ...p, isBlockedByMe: false });
    await firstValueFrom(this.usersApi.unblockUser(p.userId)).catch(() => {
      this.profile.set(p); // rollback
    });
  }

  async message(): Promise<void> {
    const p = this.profile();
    if (!p) return;
    // To open DM, we could fetch conversations and find the matching one,
    // or just navigate to messages and let it handle it. Since we lack a
    // direct route /messages/new?to=userId, we'll create a dummy DM to ensure
    // a conversation exists, then navigate to it.
    // In a real app we'd have a specific way to start a conversation.
    try {
      const dm = await firstValueFrom(this.socialApi.sendDm({ recipientId: p.userId, content: '👋' }));
      void this.router.navigate(['/app/messages', dm.conversationId]);
    } catch {
      console.error('Failed to start conversation');
    }
  }
}
