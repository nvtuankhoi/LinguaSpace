import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, session } from '../db';
import type {
  BadgeDto,
  LeaderboardEntryDto,
  LeaderboardPeriod,
  XpDailyDto,
  XpHistoryPeriod,
  XpSummaryDto,
} from '../../models';

const BASE = environment.apiBaseUrl;

/** Rank is derived from totalXp — mock has no persisted leaderboard table. */
const ranked = (): LeaderboardEntryDto[] =>
  db.users
    .map((u) => ({
      rank: 0,
      userId: u.userId,
      displayName: u.displayName,
      avatarUrl: u.avatarUrl,
      totalXp: u.totalXp,
      currentStreak: u.currentStreak,
    }))
    .sort((a, b) => b.totalXp - a.totalXp)
    .map((e, i) => ({ ...e, rank: i + 1 }));

const leaderboard = (limit: number): LeaderboardEntryDto[] =>
  ranked().slice(0, Math.min(Math.max(limit, 1), 50));

const xpSummary = (userId: string): XpSummaryDto | null => {
  const u = db.users.find((x) => x.userId === userId);
  if (!u) return null;
  return {
    totalXp: u.totalXp,
    currentStreak: u.currentStreak,
    longestStreak: u.longestStreak,
    lastActivityAt: u.lastActivityAt,
    badgeCount: u.badges.length,
    rank: ranked().find((e) => e.userId === userId)?.rank ?? 0,
  };
};

const dayLabel = (offsetDays: number): string =>
  new Date(Date.now() - offsetDays * 86_400_000).toISOString().slice(0, 10);

// A varied, deterministic-ish week so the chart looks natural (some rest days).
const pattern = [40, 0, 65, 30, 0, 55, 25, 15, 70, 20, 0, 45, 35, 10, 60, 0, 25, 50, 30, 0, 40, 55, 20, 15, 0, 35, 60, 25, 45, 10];

const xpHistory = (period: XpHistoryPeriod): XpDailyDto[] => {
  const days = period === 'week' ? 7 : 30;
  const start = pattern.length - days;
  return Array.from({ length: days }, (_, i) => {
    const offset = days - 1 - i;
    const amount = pattern[start + i] ?? 0;
    return {
      date: dayLabel(offset),
      totalXp: amount,
      transactions: amount > 0
        ? [{ amount, reason: 'Practised in a room', earnedAt: new Date(Date.now() - offset * 86_400_000).toISOString() }]
        : [],
    };
  });
};

export const gamificationHandlers = [
  http.get(`${BASE}/Gamification/leaderboard`, ({ request }) => {
    const url = new URL(request.url);
    const limit = Number(url.searchParams.get('limit') ?? 20);
    // period is accepted but the mock XP pool is period-agnostic; rank order is unchanged.
    const _period = url.searchParams.get('period') as LeaderboardPeriod | null;
    void _period;
    return HttpResponse.json(leaderboard(limit));
  }),

  http.get(`${BASE}/Gamification/me/xp`, () => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const xp = xpSummary(session.userId);
    return xp ? HttpResponse.json(xp) : HttpResponse.json({ detail: 'Not found.' }, { status: 404 });
  }),

  http.get(`${BASE}/Gamification/users/:userId/xp`, ({ params }) => {
    const xp = xpSummary(String(params['userId']));
    return xp ? HttpResponse.json(xp) : HttpResponse.json({ detail: 'Not found.' }, { status: 404 });
  }),

  http.get(`${BASE}/Gamification/me/badges`, () => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const u = db.users.find((x) => x.userId === session.userId);
    return HttpResponse.json(u ? (u.badges as BadgeDto[]) : []);
  }),

  http.get(`${BASE}/Gamification/users/:userId/badges`, ({ params }) => {
    const u = db.users.find((x) => x.userId === String(params['userId']));
    return HttpResponse.json(u ? (u.badges as BadgeDto[]) : []);
  }),

  http.get(`${BASE}/Gamification/me/xp/history`, ({ request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const url = new URL(request.url);
    const period = (url.searchParams.get('period') ?? 'week') as XpHistoryPeriod;
    return HttpResponse.json(xpHistory(period));
  }),
];
