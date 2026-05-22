namespace LinguaSpace.Application.Users.DTOs;

public record UserLanguageDto(int Id, string LanguageCode, string Type, string? Level);

public record UserProfileDto(
    int Id,
    string UserId,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? Timezone,
    bool IsOnline,
    DateTimeOffset? LastSeenAt,
    IList<UserLanguageDto> Languages,
    bool IsFollowedByMe,
    bool IsFriend,
    /// <summary>Current user sent a friend request to this user (outgoing — can Cancel).</summary>
    bool HasOutgoingFriendRequest,
    /// <summary>This user sent a friend request to the current user (incoming — can Accept/Reject).</summary>
    bool HasIncomingFriendRequest,
    int FollowerCount,
    int FollowingCount,
    int FriendCount);
