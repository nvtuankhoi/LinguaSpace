import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { ModerationApi } from '../api/moderation.api';
import { ReportAction, ReportDto, ReportStatus } from '../models';

type LoadStatus = 'idle' | 'loading' | 'error';
/** 'All' = no status filter; the backend otherwise filters by ReportStatus name. */
export type ReportFilter = ReportStatus | 'All';

interface ModerationState {
  reports: ReportDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
  filter: ReportFilter;
  status: LoadStatus;
  /** Key of the report/user being acted on (`report:<id>` / `user:<id>`), to disable its button. */
  pendingAction: string | null;
}

const initialState: ModerationState = {
  reports: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  hasMore: false,
  filter: 'Pending',
  status: 'idle',
  pendingAction: null,
};

export const ModerationStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, api = inject(ModerationApi)) => {
    const load = async (): Promise<void> => {
      patchState(store, { status: 'loading' });
      try {
        const res = await firstValueFrom(
          api.getReports({
            page: store.page(),
            pageSize: store.pageSize(),
            status: store.filter() === 'All' ? null : store.filter(),
          }),
        );
        patchState(store, {
          reports: res.items,
          totalCount: res.totalCount,
          hasMore: res.hasMore,
          status: 'idle',
        });
      } catch {
        patchState(store, { status: 'error' });
      }
    };

    return {
      load,

      setFilter(filter: ReportFilter): void {
        patchState(store, { filter, page: 1 });
        void load();
      },

      setPage(page: number): void {
        patchState(store, { page: Math.max(1, page) });
        void load();
      },

      async resolveReport(reportId: number, action: ReportAction): Promise<void> {
        const key = `report:${reportId}`;
        patchState(store, { pendingAction: key });
        try {
          await firstValueFrom(api.resolveReport(reportId, action));
          const resolvedStatus: ReportStatus = action === 0 ? 'Resolved' : 'Dismissed';
          patchState(store, {
            reports: store.reports().map((r) =>
              r.id === reportId ? { ...r, status: resolvedStatus, resolvedAt: new Date().toISOString() } : r,
            ),
          });
        } finally {
          patchState(store, { pendingAction: null });
        }
      },

      async banUser(userId: string): Promise<void> {
        const key = `user:${userId}`;
        patchState(store, { pendingAction: key });
        try {
          await firstValueFrom(api.banUser(userId));
        } finally {
          patchState(store, { pendingAction: null });
        }
      },

      async unbanUser(userId: string): Promise<void> {
        const key = `user:${userId}`;
        patchState(store, { pendingAction: key });
        try {
          await firstValueFrom(api.unbanUser(userId));
        } finally {
          patchState(store, { pendingAction: null });
        }
      },
    };
  }),
);
