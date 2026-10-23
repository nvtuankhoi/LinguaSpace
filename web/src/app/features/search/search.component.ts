import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { UsersApi } from '../../core/api/users.api';
import { PostSummaryDto, UserSummaryDto } from '../../core/models';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PostCardComponent } from '../feed/post-card.component';
import { patchCommentDelta, patchPostDelete, patchPostEdit, patchPostReaction } from '../feed/live-post-patch';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';

@Component({
  selector: 'app-search',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AvatarComponent, IconComponent, PostCardComponent],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly usersApi = inject(UsersApi);
  private readonly feedApi = inject(FeedApi);
  private readonly presence = inject(PresenceRealtimeService);
  private readonly destroyRef = inject(DestroyRef);

  /** Post groups currently joined so the Posts tab reflects live edits/deletes/reactions. */
  private readonly joined = new Set<number>();

  protected readonly query = signal('');
  protected readonly tab = signal<'users' | 'posts'>('users');
  protected readonly results = signal<UserSummaryDto[]>([]);
  protected readonly postResults = signal<PostSummaryDto[]>([]);
  protected readonly status = signal<'idle' | 'loading' | 'error'>('idle');

  constructor() {
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const q = params['q'] ?? '';
      this.query.set(q);
      if (q) {
        void this.search(q, this.tab());
      } else {
        this.results.set([]);
        this.postResults.set([]);
        this.status.set('idle');
      }
    });

    // Join each loaded result's post group so the Posts tab reflects live
    // edits/deletes/reactions/comment-counts (mirrors the feed list). Each id
    // joins once; all are left on destroy.
    effect(() => {
      for (const p of this.postResults()) {
        if (!this.joined.has(p.id)) {
          this.joined.add(p.id);
          void this.presence.joinPostGroup(p.id);
        }
      }
    });

    this.presence.onPostEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.postResults.update((items) => patchPostEdit(items, ev.id, ev.content, ev.languageCode));
    });
    this.presence.onPostDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.postResults.update((items) => patchPostDelete(items, ev.id));
    });
    this.presence.onNewReaction.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.targetType === 'Post') {
        this.postResults.update((items) => patchPostReaction(items, ev.targetId, ev.likeCount));
      }
    });
    this.presence.onNewComment.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.postResults.update((items) => patchCommentDelta(items, ev.postId, +1));
    });
    this.presence.onCommentDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      this.postResults.update((items) => patchCommentDelta(items, ev.postId, -1));
    });

    this.destroyRef.onDestroy(() => {
      for (const id of this.joined) {
        void this.presence.leavePostGroup(id);
      }
      this.joined.clear();
    });
  }

  protected setTab(tab: 'users' | 'posts'): void {
    if (this.tab() === tab) {
      return;
    }
    this.tab.set(tab);
    if (this.query()) {
      void this.search(this.query(), tab);
    }
  }

  private async search(q: string, tab: 'users' | 'posts'): Promise<void> {
    this.status.set('loading');
    try {
      if (tab === 'users') {
        const res = await firstValueFrom(this.usersApi.searchUsers(q));
        this.results.set(res.items);
        this.postResults.set([]);
      } else {
        const res = await firstValueFrom(this.feedApi.searchPosts(q));
        this.postResults.set(res.items);
        this.results.set([]);
      }
      this.status.set('idle');
    } catch {
      this.status.set('error');
    }
  }
}
