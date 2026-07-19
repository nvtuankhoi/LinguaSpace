import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  BadgeDto,
  LeaderboardEntryDto,
  LeaderboardPeriod,
  XpDailyDto,
  XpHistoryPeriod,
  XpSummaryDto,
} from '../models';

@Injectable({ providedIn: 'root' })
export class GamificationApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getLeaderboard(period: LeaderboardPeriod = 'all', limit = 20) {
    const params = new HttpParams().set('period', period).set('limit', String(limit));
    return this.http.get<LeaderboardEntryDto[]>(`${this.base}/Gamification/leaderboard`, {
      params,
      withCredentials: true,
    });
  }

  getMyXp() {
    return this.http.get<XpSummaryDto>(`${this.base}/Gamification/me/xp`, { withCredentials: true });
  }

  getUserXp(userId: string) {
    return this.http.get<XpSummaryDto>(`${this.base}/Gamification/users/${userId}/xp`, {
      withCredentials: true,
    });
  }

  getMyBadges() {
    return this.http.get<BadgeDto[]>(`${this.base}/Gamification/me/badges`, { withCredentials: true });
  }

  getUserBadges(userId: string) {
    return this.http.get<BadgeDto[]>(`${this.base}/Gamification/users/${userId}/badges`, {
      withCredentials: true,
    });
  }

  getMyXpHistory(period: XpHistoryPeriod = 'week') {
    const params = new HttpParams().set('period', period);
    return this.http.get<XpDailyDto[]>(`${this.base}/Gamification/me/xp/history`, {
      params,
      withCredentials: true,
    });
  }
}
