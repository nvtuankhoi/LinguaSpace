import type { LanguageLevel, LanguageType } from './enums';

export interface UserLanguageDto {
  id: number;
  languageCode: string;
  type: LanguageType;
  level: LanguageLevel | null;
}

export interface UserProfileDto {
  id: number;
  userId: string;
  displayName: string;
  bio: string | null;
  avatarUrl: string | null;
  timezone: string | null;
  isOnline: boolean;
  lastSeenAt: string | null;
  languages: UserLanguageDto[];
  isFollowedByMe: boolean;
  isFriend: boolean;
  hasOutgoingFriendRequest: boolean;
  hasIncomingFriendRequest: boolean;
  followerCount: number;
  followingCount: number;
  friendCount: number;
  isBlockedByMe: boolean;
}

export interface UserSummaryDto {
  id: number;
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  isOnline: boolean;
}

export interface FriendRequestDto {
  id: number;
  requesterId: string;
  requesterDisplayName: string;
  requesterAvatarUrl: string | null;
  addresseeId: string;
  addresseeDisplayName: string;
  addresseeAvatarUrl: string | null;
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  timezone?: string | null;
}

export interface AddLanguageRequest {
  languageCode: string;
  type: LanguageType;
  level?: LanguageLevel | null;
}

export interface UpdateLanguageRequest {
  level?: LanguageLevel | null;
}

export interface AcceptFriendRequestRequest {
  accept: boolean;
}
