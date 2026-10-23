import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { AuthStore } from '../../core/auth/auth.store';
import { FeedStore, FeedTab } from '../../core/state/feed.store';
import { RoomStore } from '../../core/state/room.store';
import { GamificationStore } from '../../core/state/gamification.store';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { ComposerComponent } from './composer.component';
import { PostCardComponent } from './post-card.component';

@Component({
  selector: 'app-feed',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ComposerComponent, PostCardComponent, IconComponent, RouterLink],
  templateUrl: './feed.component.html',
  styleUrl: './feed.component.scss',
})
export class FeedComponent {
  private readonly feed = inject(FeedStore);
  private readonly roomStore = inject(RoomStore);
  private readonly gamificationStore = inject(GamificationStore);
  private readonly presence = inject(PresenceRealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly me = inject(AuthStore).user;

  /** Post groups currently joined so the list reflects live edits/deletes/reactions. */
  private readonly joined = new Set<number>();

  protected readonly items = this.feed.items;
  protected readonly status = this.feed.status;
  protected readonly tab = this.feed.tab;
  protected readonly hasMore = this.feed.hasMore;

  /** Show only active rooms with at least one participant in the sidebar */
  protected readonly activeRooms = computed(() =>
    this.roomStore.rooms()
      .filter((r) => r.participantCount > 0)
      .slice(0, 4),
  );

  /** Top 5 from leaderboard for the mini widget */
  protected readonly topLearners = computed(() =>
    this.gamificationStore.leaderboard().slice(0, 5),
  );

  constructor() {
    if (!this.feed.items().length) {
      void this.feed.loadFirst();
    }
    void this.roomStore.loadRooms();
    void this.gamificationStore.loadLeaderboard('all');

    // Live "following" feed: new posts from followed users appear without a reload.
    this.presence.onNewPost.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      void this.feed.prependNewPost(ev.postId);
    });

    // Join each loaded post's group so the list (not just the post-detail page)
    // reflects live edits/deletes/reactions/comment-counts. Idempotent: each id
    // joins once; all are left on destroy. (Stale joins across a tab switch are
    // harmless — groups are cheap — and cleared when the feed unloads.)
    effect(() => {
      for (const p of this.feed.items()) {
        if (!this.joined.has(p.id)) {
          this.joined.add(p.id);
          void this.presence.joinPostGroup(p.id);
        }
      }
    });

    // Live list updates from the joined post groups.
    this.presence.onPostEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.feed.applyLivePostEdit(ev.id, ev.content, ev.languageCode);
    });
    this.presence.onPostDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.feed.applyLivePostDelete(ev.id);
    });
    this.presence.onNewReaction.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.targetType === 'Post') {
        this.feed.applyLivePostReaction(ev.targetId, ev.likeCount);
      }
    });
    this.presence.onNewComment.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.feed.applyLiveCommentDelta(ev.postId, +1);
    });
    this.presence.onCommentDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.feed.applyLiveCommentDelta(ev.postId, -1);
    });

    this.destroyRef.onDestroy(() => {
      for (const id of this.joined) {
        void this.presence.leavePostGroup(id);
      }
      this.joined.clear();
    });
  }

  protected setTab(tab: FeedTab): void {
    void this.feed.setTab(tab);
  }

  protected loadMore(): void {
    void this.feed.loadMore();
  }

  protected retry(): void {
    void this.feed.loadFirst();
  }
}
