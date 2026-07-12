import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { FeedApi } from '../../core/api/feed.api';
import { PostDto } from '../../core/models';
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
export class PostDetailComponent implements OnInit {
  private readonly feedApi = inject(FeedApi);

  /** Bound from the :id route param via withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly post = signal<PostDto | null>(null);
  protected readonly status = signal<Status>('idle');

  async ngOnInit(): Promise<void> {
    this.status.set('loading');
    try {
      const post = await firstValueFrom(this.feedApi.getPost(Number(this.id())));
      this.post.set(post);
      this.status.set('idle');
    } catch {
      this.status.set('error');
    }
  }
}
