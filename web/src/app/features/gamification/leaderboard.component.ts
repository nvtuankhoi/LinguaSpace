import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AuthStore } from '../../core/auth/auth.store';
import { GamificationStore } from '../../core/state/gamification.store';
import { LeaderboardPeriod } from '../../core/models';
import { AvatarComponent } from '../../shared/ui/avatar/avatar.component';
import { IconComponent } from '../../shared/ui/icon/icon.component';

@Component({
  selector: 'app-leaderboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, IconComponent],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent {
  private readonly store = inject(GamificationStore);
  private readonly auth = inject(AuthStore);

  protected readonly entries = this.store.leaderboard;
  protected readonly status = this.store.leaderboardStatus;
  protected readonly period = this.store.leaderboardPeriod;
  protected readonly me = this.auth.user;

  protected readonly periods: readonly { value: LeaderboardPeriod; label: string }[] = [
    { value: 'all', label: 'All time' },
    { value: 'weekly', label: 'This week' },
    { value: 'monthly', label: 'This month' },
  ];

  /** Top 3 entries for the podium */
  protected readonly topThree = computed(() => this.entries().slice(0, 3));

  /** Rest of the list (rank 4+) */
  protected readonly restEntries = computed(() => this.entries().slice(3));

  /** The current user's entry in the leaderboard */
  protected readonly myEntry = computed(() =>
    this.entries().find((e) => e.userId === this.me()?.userId) ?? null,
  );

  /** Whether the current user is in the visible portion (top 10) */
  protected readonly myInTop = computed(() => {
    const my = this.myEntry();
    return my ? my.rank <= this.entries().length : false;
  });

  constructor() {
    void this.store.loadLeaderboard('all');
  }

  protected setPeriod(p: LeaderboardPeriod): void {
    void this.store.loadLeaderboard(p);
  }

  protected retry(): void {
    void this.store.loadLeaderboard(this.period());
  }

  protected fmt(n: number): string {
    return n.toLocaleString();
  }
}
