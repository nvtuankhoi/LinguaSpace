export interface XpSummaryDto {
  totalXp: number;
  currentStreak: number;
  longestStreak: number;
  lastActivityAt: string | null;
  badgeCount: number;
  rank: number;
}

export interface LeaderboardEntryDto {
  rank: number;
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  totalXp: number;
  currentStreak: number;
}

export interface BadgeDto {
  badgeId: number;
  code: string;
  name: string;
  description: string | null;
  iconUrl: string | null;
  earnedAt: string;
}

export interface XpTransactionDto {
  amount: number;
  reason: string;
  earnedAt: string;
}

export interface XpDailyDto {
  date: string; // YYYY-MM-DD
  totalXp: number;
  transactions: XpTransactionDto[];
}
