// Domain enums — mirror src/Domain enums. Backend serializes as the string name.

export type LanguageType = 'Native' | 'Learning';
/** CEFR proficiency. */
export type LanguageLevel = 'A1' | 'A2' | 'B1' | 'B2' | 'C1' | 'C2';

export type PostType = 'Text' | 'VocabCard' | 'Poll';
export type ReactionType = 'Like' | 'Love' | 'Haha' | 'Wow' | 'Sad' | 'Angry';
export type ReactionTargetType = 'Post' | 'Comment';

export type RoomType = 'TextOnly' | 'VoiceOnly' | 'VideoEnabled';
export type RoomStatus = 'Active' | 'Closed';
export type MessageType = 'Text' | 'System';
export type ParticipantRole = 'Host' | 'Speaker' | 'Listener';

export type FriendshipStatus = 'Pending' | 'Accepted' | 'Declined' | 'Blocked';

export type NotificationType =
  | 'FriendRequest'
  | 'FriendAccepted'
  | 'NewFollower'
  | 'RoomInvite'
  | 'PostLike'
  | 'PostComment'
  | 'CommentLike'
  | 'DirectMessage'
  | 'BadgeEarned'
  | 'SystemMessage';

export type ReportStatus = 'Pending' | 'UnderReview' | 'Resolved' | 'Dismissed';
export type ReportTargetType = 'User' | 'Post' | 'Room' | 'Message';
/** Resolve = 0, Dismiss = 1 (matches the backend ReportAction enum). */
export type ReportAction = 0 | 1;

export type DevicePlatform = 'Web' | 'iOS' | 'Android';

export type LeaderboardPeriod = 'all' | 'weekly' | 'monthly';
export type XpHistoryPeriod = 'week' | 'month';
