import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, OnDestroy, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { PostDto } from '../../core/models';
import { PresenceRealtimeService } from '../../core/state/presence-realtime.service';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PostCardComponent } from './post-card.component';

type Status = 'idle' | 'loading' | 'error';

@Component({
  selector: 'app-post-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PostCardComponent, IconComponent],
  templateUrl: './post-detail.component.html',
  styleUrl: './post-detail.component.scss',
})
export class PostDetailComponent implements OnInit, OnDestroy {
  private readonly feedApi = inject(FeedApi);
  private readonly realtime = inject(PresenceRealtimeService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Bound from the :id route param via withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly post = signal<PostDto | null>(null);
  protected readonly status = signal<Status>('idle');

  async ngOnInit(): Promise<void> {
    const postId = Number(this.id());

    // Live post-level updates from the post group. Set up before the fetch so
    // they're ready, then join the group once the post is confirmed to exist.
    this.realtime.onPostEdited.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.id !== postId) return;
      this.post.update((p) => (p ? { ...p, content: ev.content, languageCode: ev.languageCode } : p));
    });
    this.realtime.onPostDeleted.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.id !== postId) return;
      void this.router.navigate(['/app/feed']);
    });
    this.realtime.onNewReaction.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ev) => {
      if (ev.targetType !== 'Post' || ev.targetId !== postId) return;
      this.post.update((p) => (p ? { ...p, likeCount: ev.likeCount } : p));
    });

    this.status.set('loading');
    try {
      const post = await firstValueFrom(this.feedApi.getPost(postId));
      this.post.set(post);
      this.status.set('idle');
      void this.realtime.joinPostGroup(postId);
    } catch {
      this.status.set('error');
    }
  }

  ngOnDestroy(): void {
    void this.realtime.leavePostGroup(Number(this.id()));
  }
}
