import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { UsersApi } from '../../core/api/users.api';
import { PostSummaryDto, UserSummaryDto } from '../../core/models';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';
import { PostCardComponent } from '../feed/post-card.component';

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
  private readonly destroyRef = inject(DestroyRef);

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
