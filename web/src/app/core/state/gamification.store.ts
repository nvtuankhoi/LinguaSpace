import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { GamificationApi } from '../api/gamification.api';
import {
  BadgeDto,
  LeaderboardEntryDto,
  LeaderboardPeriod,
  XpDailyDto,
  XpHistoryPeriod,
  XpSummaryDto,
} from '../models';

type Status = 'idle' | 'loading' | 'error';

interface GamificationState {
  leaderboard: LeaderboardEntryDto[];
  leaderboardPeriod: LeaderboardPeriod;
  leaderboardStatus: Status;
  myXp: XpSummaryDto | null;
  myBadges: BadgeDto[];
  xpHistory: XpDailyDto[];
  xpHistoryPeriod: XpHistoryPeriod;
  /** Covers myXp + myBadges + xpHistory — they load together as the "progress" surface. */
  progressStatus: Status;
}

const initialState: GamificationState = {
  leaderboard: [],
  leaderboardPeriod: 'all',
  leaderboardStatus: 'idle',
  myXp: null,
  myBadges: [],
  xpHistory: [],
  xpHistoryPeriod: 'week',
  progressStatus: 'idle',
};

export const GamificationStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, api = inject(GamificationApi)) => {
    return {
      async loadLeaderboard(period: LeaderboardPeriod = 'all'): Promise<void> {
        patchState(store, { leaderboardStatus: 'loading', leaderboardPeriod: period });
        try {
          const leaderboard = await firstValueFrom(api.getLeaderboard(period));
          patchState(store, { leaderboard, leaderboardStatus: 'idle' });
        } catch {
          patchState(store, { leaderboardStatus: 'error' });
        }
      },

      async loadProgress(period: XpHistoryPeriod = store.xpHistoryPeriod()): Promise<void> {
        patchState(store, { progressStatus: 'loading', xpHistoryPeriod: period });
        try {
          const [myXp, myBadges, xpHistory] = await Promise.all([
            firstValueFrom(api.getMyXp()),
            firstValueFrom(api.getMyBadges()),
            firstValueFrom(api.getMyXpHistory(period)),
          ]);
          patchState(store, { myXp, myBadges, xpHistory, progressStatus: 'idle' });
        } catch {
          patchState(store, { progressStatus: 'error' });
        }
      },

      async loadXpHistory(period: XpHistoryPeriod): Promise<void> {
        patchState(store, { xpHistoryPeriod: period });
        try {
          const xpHistory = await firstValueFrom(api.getMyXpHistory(period));
          patchState(store, { xpHistory });
        } catch {
          /* keep the previous history on failure */
        }
      },
    };
  }),
);
