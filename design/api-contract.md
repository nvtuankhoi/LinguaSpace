# LinguaSpace — Frontend ↔ Backend API Contract

The Angular frontend must match this contract exactly. This is the source of truth
for TypeScript models, HTTP services, and SignalR/LiveKit clients. Generated from
the .NET 10 backend (`src/Domain`, `src/Application`, `src/Web`).

**Base URL (dev):** `https://localhost:7001`
**Docs:** Scalar at `/scalar`, OpenAPI at `/openapi/v1.json`

---

## Auth model (read first)

- **Access token** is returned in the **response body** (`AuthResponseDto.AccessToken`).
  Store it in **memory only** (Angular service), never `localStorage`. Attach as
  `Authorization: Bearer <token>`.
- **Refresh token** is an **HttpOnly, Secure, SameSite=Strict cookie** named
  `refresh_token`, path `/api/Auth`, 7-day expiry. The browser sends it automatically;
  Angular code never touches it.
- **Refresh flow:** on 401, call `POST /api/Auth/refresh` (no body) → new access token
  in body + rotated cookie. Use an Angular HTTP interceptor with a refresh queue
  (no parallel refreshes).
- **OAuth:** Google Sign-In SDK produces an `id_token`; send it to
  `POST /api/Auth/oauth/google`.
- **API base & CORS:** the Web project is a Minimal API; confirm CORS allows the
  Angular dev origin and `credentials: 'include'` so the refresh cookie is sent.

---

## Domain enums → TS unions

```ts
type LanguageType = 'Native' | 'Learning';          // 0 | 1
type LanguageLevel = 'A1'|'A2'|'B1'|'B2'|'C1'|'C2';  // CEFR, 0..5
type PostType      = 'Text'|'VocabCard'|'Poll';      // 0..2
type ReactionType  = 'Like'|'Love'|'Haha'|'Wow'|'Sad'|'Angry';
type RoomType      = 'TextOnly'|'VoiceOnly'|'VideoEnabled';
type RoomStatus    = 'Active'|'Closed';
type MessageType   = 'Text'|'System';
type ParticipantRole = 'Host'|'Speaker'|'Listener';
type FriendshipStatus = 'Pending'|'Accepted'|'Declined'|'Blocked';
type NotificationType = 'FriendRequest'|'FriendAccepted'|'NewFollower'|'RoomInvite'
  |'PostLike'|'PostComment'|'CommentLike'|'DirectMessage'|'BadgeEarned'|'SystemMessage';
type ReportStatus  = 'Pending'|'UnderReview'|'Resolved'|'Dismissed';
type DevicePlatform = 'Web'|'iOS'|'Android';
```

> Backend serializes enums as their **string name** (default ASP.NET Core JSON). Confirm
> via `/openapi/v1.json`; if integers, switch the serializer to strings or map in services.

---

## Common wrappers

```ts
interface PaginatedResult<T>     { items: T[]; totalCount: number; page: number; pageSize: number; hasMore: boolean; }
interface CursorPagedResult<T>   { items: T[]; hasMore: boolean; nextCursor: string | null; } // nextCursor is ISO date
```

---

## Auth (`/api/Auth`)

| Method | Path | Body / Query | Response |
|---|---|---|---|
| POST | `/register` | `{ email, password }` | `201 { userId, email }` |
| POST | `/login` | `{ email, password }` | `200 AuthResponseDto` + sets cookie |
| POST | `/refresh` | — (cookie) | `200 { accessToken, expiresIn }` + rotated cookie |
| POST | `/logout` | — | `204`, clears cookie |
| GET  | `/me` | — | `200 CurrentUserDto` |
| POST | `/verify-email` | `{ token }` | `204` |
| POST | `/resend-verification` | — | `204` |
| POST | `/change-password` | `{ currentPassword, newPassword }` | `204` |
| POST | `/change-email` | `{ newEmail, password }` | `204` |
| GET  | `/sessions` | — | `200 ActiveSessionDto[]` |
| DELETE | `/sessions` | — | `204` (revoke all) |
| DELETE | `/sessions/{sessionId}` | — | `204` |
| POST | `/forgot-password` | `{ email }` | `200` (always, no enumeration) |
| POST | `/reset-password` | `{ token, newPassword }` | `204` |
| POST | `/device-token` | `{ fcmToken, platform }` | `204` |
| POST | `/oauth/google` | `{ idToken }` | `200 AuthResponseDto` + cookie |

```ts
interface AuthResponseDto { accessToken: string; expiresIn: number; userId: string; email: string; }
interface CurrentUserDto  { userId: string; email: string; displayName: string; roles: string[]; avatarUrl: string|null; isEmailConfirmed: boolean; }
interface ActiveSessionDto{ id: number; deviceInfo: string|null; ipAddress: string|null; createdAt: string; lastActiveAt: string; }
```

## Users (`/api/Users`)

```ts
interface UserProfileDto {
  id: number; userId: string; displayName: string; bio: string|null; avatarUrl: string|null;
  timezone: string|null; isOnline: boolean; lastSeenAt: string|null;
  languages: UserLanguageDto[]; isFollowedByMe: boolean; isFriend: boolean;
  hasOutgoingFriendRequest: boolean; hasIncomingFriendRequest: boolean;
  followerCount: number; followingCount: number; friendCount: number;
}
interface UserSummaryDto { id: number; userId: string; displayName: string; avatarUrl: string|null; isOnline: boolean; }
interface UserLanguageDto { id: number; languageCode: string; type: LanguageType; level: LanguageLevel|null; }
interface FriendRequestDto { id: number; requesterId: string; requesterDisplayName: string; requesterAvatarUrl: string|null; addresseeId: string; addresseeDisplayName: string; addresseeAvatarUrl: string|null; createdAt: string; }
```

| Method | Path | Notes |
|---|---|---|
| GET | `/{userId}` | `UserProfileDto` |
| GET | `/?term=&languageCode=&page=&pageSize=` | `PaginatedResult<UserSummaryDto>` |
| PUT | `/me/profile` | `{ displayName, bio?, avatarUrl?, timezone? }` → `204` |
| PUT | `/me/avatar` | `{ avatarUrl }` → `204` |
| GET/POST | `/me/languages` | GET list; POST `{ languageCode, type, level? }` → `201 {id}` |
| PUT/DELETE | `/me/languages/{languageId}` | PUT `{ level? }`; DELETE |
| GET | `/me/friend-requests?page=&pageSize=` | `PaginatedResult<FriendRequestDto>` |
| POST | `/{userId}/friend-request` | `201 {requestId}` |
| PUT | `/friend-requests/{requestId}` | `{ accept: boolean }` → `204` |
| DELETE | `/friend-requests/{requestId}` | `204` |
| POST/DELETE | `/{userId}/follow` | `201` / `204` |
| POST/DELETE | `/{userId}/block` | `204` |
| GET | `/{userId}/friends\|followers\|following?page=&pageSize=` | `PaginatedResult<UserSummaryDto>` |
| DELETE | `/{userId}/friendship` | `204` |
| GET | `/me/blocked?page=&pageSize=` | `PaginatedResult<UserSummaryDto>` |

## Feed (`/api/Feed`)

```ts
interface PostDto {
  id: number; authorId: string; content: string; postType: PostType; languageCode: string|null;
  metadata: PostMetadataDto|null; likeCount: number; commentCount: number; createdAt: string;
  tags: string[]; mediaItems: MediaItemDto[]; comments: CommentDto[];
}
interface PostSummaryDto { /* same as PostDto minus mediaItems & comments */ }
interface CommentDto { id: number; postId: number; authorId: string; content: string; parentCommentId: number|null; likeCount: number; createdAt: string; }
interface PostMetadataDto { audioUrl: string|null; durationSeconds: number|null; thumbnailUrl: string|null; linkUrl: string|null; linkTitle: string|null; linkDescription: string|null; }
interface MediaItemDto { id: number; url: string; sortOrder: number; }
```

| Method | Path | Notes |
|---|---|---|
| GET | `/?beforeCursor=&pageSize=` | `CursorPagedResult<PostSummaryDto>` (following) |
| GET | `/explore?languageCode=&postType=&beforeCursor=&pageSize=` | public posts |
| GET | `/search?q=&page=&pageSize=` | `PaginatedResult<PostSummaryDto>` |
| GET | `/users/{userId}?beforeCursor=&pageSize=` | a user's posts |
| GET | `/posts/{postId}` | `PostDto` or `404` |
| GET | `/posts/{postId}/comments?parentCommentId=&page=&pageSize=` | `PaginatedResult<CommentDto>` |
| GET | `/posts/{postId}/reactions?page=&pageSize=` | `PaginatedResult<ReactionDetailDto>` |
| POST | `/posts` | `{ content, postType, languageCode?, metadata?, tags?, mediaUrls? }` → `201 {postId}` (max 4 media, 5 tags) |
| PUT/DELETE | `/posts/{postId}` | PUT `{ content, languageCode? }`; DELETE |
| POST | `/posts/{postId}/comments` | `{ content, parentCommentId? }` → `201 {commentId}` (one reply level) |
| PUT/DELETE | `/comments/{commentId}` | `{ content }` / `204` |
| POST/DELETE | `/posts/{postId}/reactions[/{reactionType}]` | POST `{ reactionType }`; DELETE by type |

## Rooms & in-room messages (`/api/Rooms`)

```ts
interface RoomDto { id: number; title: string; description: string|null; languageCode: string; maxParticipants: number; participantCount: number; status: RoomStatus; roomType: RoomType; hostId: string; created: string; participants: RoomParticipantDto[]; }
interface RoomSummaryDto { id: number; title: string; languageCode: string; maxParticipants: number; participantCount: number; roomType: RoomType; hostId: string; }
interface RoomParticipantDto { userId: string; displayName: string; avatarUrl: string|null; role: ParticipantRole; joinedAt: string; isMuted: boolean; }
interface MessageDto { id: number; senderId: string; senderDisplayName: string; content: string; type: MessageType; sentAt: string; isDeleted: boolean; }
```

| Method | Path | Notes |
|---|---|---|
| GET | `/?languageCode=&roomType=&q=&page=&pageSize=` | `PaginatedResult<RoomSummaryDto>` |
| GET | `/mine?page=&pageSize=` | my rooms |
| GET | `/{roomId}` | `RoomDto` or `404` |
| POST | `` | `{ title, description?, languageCode, maxParticipants, roomType }` → `201 {roomId}` |
| PUT/DELETE | `/{roomId}` | PUT `{ title, description?, maxParticipants }`; host-only delete |
| POST | `/{roomId}/join` \|\| `/leave` | `204` |
| POST | `/{roomId}/transfer-host/{targetUserId}` | `204` |
| POST | `/{roomId}/invite/{targetUserId}` | `204` |
| POST | `/{roomId}/mute/{targetUserId}` | `{ mute: boolean }` → `204` |
| DELETE | `/{roomId}/kick/{targetUserId}` | `204` |
| GET | `/{roomId}/messages?beforeCursor=&pageSize=` | `MessageDto[]` |
| POST | `/{roomId}/messages` | `{ content }` → `201 {messageId}` |
| DELETE | `/{roomId}/messages/{messageId}` | `204` |

## Direct messaging (`/api/Social`)

```ts
interface ConversationDto { id: number; otherUserId: string; otherUserDisplayName: string|null; otherUserAvatarUrl: string|null; lastMessage: string|null; lastMessageAt: string|null; unreadCount: number; }
interface DirectMessageDto { id: number; conversationId: number; senderId: string; content: string; sentAt: string; isRead: boolean; isDeleted: boolean; editedAt: string|null; }
interface ConversationsUnreadCountDto { unreadConversations: number; totalUnread: number; }
```

| Method | Path | Notes |
|---|---|---|
| GET | `/conversations?page=&pageSize=` | `PaginatedResult<ConversationDto>` |
| GET | `/conversations/unread-count` | `ConversationsUnreadCountDto` |
| GET | `/conversations/{conversationId}/messages?beforeCursor=&pageSize=` | `CursorPagedResult<DirectMessageDto>` |
| POST | `/dm` | `{ recipientId, content }` → `201 DirectMessageDto` |
| PUT/DELETE | `/messages/{messageId}` | `{ content }` / `204` |
| POST | `/conversations/{conversationId}/read` | `204` |

## Gamification (`/api/Gamification`)

```ts
interface XpSummaryDto { totalXp: number; currentStreak: number; longestStreak: number; lastActivityAt: string|null; badgeCount: number; rank: number; }
interface LeaderboardEntryDto { rank: number; userId: string; displayName: string; avatarUrl: string|null; totalXp: number; currentStreak: number; }
interface BadgeDto { badgeId: number; code: string; name: string; description: string|null; iconUrl: string|null; earnedAt: string; }
interface XpDailyDto { date: string; totalXp: number; transactions: XpTransactionDto[]; }   // date = YYYY-MM-DD
interface XpTransactionDto { amount: number; reason: string; earnedAt: string; }
```

| Method | Path | Notes |
|---|---|---|
| GET | `/leaderboard?period=&limit=` | period `all\|weekly\|monthly`, limit 1–50 → `LeaderboardEntryDto[]` |
| GET | `/me/xp` \|\| `/users/{userId}/xp` | `XpSummaryDto` |
| GET | `/me/badges` \|\| `/users/{userId}/badges` | `BadgeDto[]` |
| GET | `/me/xp/history?period=` | period `week\|month` → `XpDailyDto[]` |

## Notifications (`/api/Notifications`)

```ts
interface NotificationDto { id: number; type: NotificationType; payload: Record<string, unknown> | null; isRead: boolean; createdAt: string; }
```
Payload shapes by `type`: FriendRequest `{requesterId,requesterDisplayName,requestId}` ·
FriendAccepted `{acceptorId,acceptorDisplayName}` · NewFollower `{followerId,followerDisplayName}` ·
RoomInvite `{roomId,roomTitle,inviterId,inviterDisplayName}` · PostLike `{postId,likerId,likerDisplayName}` ·
PostComment `{postId,commentId,commenterId,commenterDisplayName,commentPreview}` ·
CommentLike `{commentId,postId,likerId,likerDisplayName}` · DirectMessage `{senderId,senderDisplayName,conversationId,messagePreview}` ·
BadgeEarned `{badgeId,badgeName,badgeIconUrl}` · SystemMessage `{message,actionUrl?}`.

| Method | Path | Notes |
|---|---|---|
| GET | `/?unreadOnly=&page=&pageSize=` | `PaginatedResult<NotificationDto>` |
| GET | `/unread-count` | `number` |
| POST | `/read` | `{ notificationIds?: number[] }` (empty/null = all) → `204` |
| POST | `/delete-batch` | `{ notificationIds?: number[] }` (empty/null = all) → `204` |

## Moderation (`/api/Moderation`) & Media (`/api/Rooms/{roomId}/media-*`)

```ts
interface ReportDto { id: number; reporterId: string; targetId: string; targetType: 'User'|'Post'|'Room'|'Message'; reason: string; status: ReportStatus; createdAt: string; resolvedAt: string|null; resolvedBy: string|null; }
interface MediaTokenDto { token: string; liveKitUrl: string; }
interface MediaSessionStatusDto { isActive: boolean; activeParticipantCount: number; }
```

- `POST /api/Moderation/report` `{ targetId, targetType, reason }` → `201 {reportId}`.
  Admin: `GET /reports`, `GET /reports/{id}`, `POST /reports/{id}/resolve { action: 0|1 }`,
  `POST /users/{userId}/ban { until? }`, `DELETE /users/{userId}/ban`.
- Media (host controls): `POST /api/Rooms/{roomId}/media-token` → `MediaTokenDto`;
  `GET .../media/participants`; `GET .../media/status`; `DELETE .../media`.

---

## Realtime — SignalR hubs

**Endpoint convention:** `/hubs/room`, `/hubs/presence`. Both require the JWT bearer.
Use `@microsoft/signalr` with an `HttpConnectionBuilder` that sets
`accessTokenFactory: () => this.auth.accessToken`.

### RoomHub (`/hubs/room`)
Client → server: `JoinRoomGroup(roomId)`, `LeaveRoomGroup(roomId)`, `SendMessage(roomId, content)`.
Server → client: `ReceiveMessage {messageId,senderId,content,sentAt}`, `UserJoinedRoom(userId,role)`,
`UserLeftRoom(userId)`, `UserJoinedMedia(userId)`, `UserLeftMedia(userId)`,
`ActiveSpeakerChanged(speakerIds[])`, `ScreenShareStarted(userId)`, `ScreenShareStopped(userId)`, `ParticipantMuted(userId,isMuted)`, `MessageDeleted(messageId)`.

### PresenceHub (`/hubs/presence`)
Call `Heartbeat()` every ~3 min. While viewing a feed post, call
`JoinPostGroup(postId)` to receive its live updates; call `LeavePostGroup(postId)`
on navigation away.

Server → client (user-targeted via `NotifyAsync`): `UserOnline(userId)`,
`UserOffline(userId)`, `Notification`, `NewDirectMessage`, `NewPost(postId,authorId)`,
`DirectMessageEdited(id,conversationId,content,editedAt)`, `DirectMessageDeleted(id,conversationId)`.

Server → client (post group `post-{postId}` via `NotifyPostGroupAsync`):
`NewComment(id,postId,authorId,content,parentCommentId,createdAt)`,
`NewReaction(targetId,targetType,likeCount)`, `CommentEdited(id,postId,content)`,
`CommentDeleted(id,postId)`, `PostEdited(id,content,languageCode)`, `PostDeleted(id)`.
A client only receives these while joined to that post's group.

---

## LiveKit (voice/video)

1. `POST /api/Rooms/{roomId}/media-token` → `{ token, liveKitUrl }`.
2. Connect with `livekit-client` (`Room.connect(liveKitUrl, token)`).
3. `roomType` drives UI: `VoiceOnly` (audio tracks), `VideoEnabled` (audio + video),
   `TextOnly` (no LiveKit; chat only via RoomHub).
4. Host webhooks (`/api/Webhooks/livekit`, HMAC-signed) update `RoomMediaSession` → XP.

---

## Notes for the Angular build

- **Models:** generate a `models/` barrel mirroring the interfaces above (one file per
  module) so services stay typed end-to-end.
- **Cursor pagination** uses ISO-date `beforeCursor`; store the last item's `createdAt`.
- **Reactions** support 6 types per post/comment — design a compact reaction picker,
  not a single like.
- **Replies** are one level deep (`parentCommentId`) — thread UI must reflect that.
- **Presence/online** is real-time via PresenceHub, not polling.
