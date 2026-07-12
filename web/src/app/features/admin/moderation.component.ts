import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ReportAction, ReportDto } from '../../core/models';
import { ModerationStore, ReportFilter } from '../../core/state/moderation.store';
import { relativeTime } from '../../core/util/time';
import { IconComponent } from '../../shared/ui/icon/icon.component';

@Component({
  selector: 'app-moderation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, IconComponent],
  templateUrl: './moderation.component.html',
  styleUrl: './moderation.component.scss',
})
export class ModerationComponent {
  protected readonly store = inject(ModerationStore);

  protected readonly reports = this.store.reports;
  protected readonly totalCount = this.store.totalCount;
  protected readonly page = this.store.page;
  protected readonly pageSize = this.store.pageSize;
  protected readonly hasMore = this.store.hasMore;
  protected readonly filter = this.store.filter;
  protected readonly status = this.store.status;
  protected readonly pendingAction = this.store.pendingAction;

  protected readonly filters: readonly ReportFilter[] = [
    'Pending',
    'UnderReview',
    'Resolved',
    'Dismissed',
    'All',
  ];

  /** Resolve = 0, Dismiss = 1 — named for readable template bindings. */
  protected readonly ACTION_RESOLVE: ReportAction = 0;
  protected readonly ACTION_DISMISS: ReportAction = 1;

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  constructor() {
    void this.store.load();
  }

  protected time(iso: string): string {
    return relativeTime(iso);
  }

  /** Truncates a GUID id to a readable prefix. */
  protected short(id: string): string {
    return id.length > 8 ? `${id.slice(0, 8)}…` : id;
  }

  protected isActive(r: ReportDto): boolean {
    return r.status === 'Pending' || r.status === 'UnderReview';
  }

  protected isPendingReport(id: number): boolean {
    return this.pendingAction() === `report:${id}`;
  }

  protected isPendingUser(userId: string): boolean {
    return this.pendingAction() === `user:${userId}`;
  }

  protected setFilter(f: ReportFilter): void {
    this.store.setFilter(f);
  }

  protected reload(): void {
    void this.store.load();
  }

  protected prev(): void {
    this.store.setPage(this.page() - 1);
  }

  protected next(): void {
    this.store.setPage(this.page() + 1);
  }

  protected async resolve(id: number, action: ReportAction): Promise<void> {
    const label = action === this.ACTION_RESOLVE ? 'Resolve' : 'Dismiss';
    if (!confirm(`${label} this report?`)) {
      return;
    }
    await this.store.resolveReport(id, action);
  }

  protected async ban(userId: string): Promise<void> {
    if (!confirm('Ban this user permanently? They will be locked out of LinguaSpace.')) {
      return;
    }
    await this.store.banUser(userId);
  }
}
