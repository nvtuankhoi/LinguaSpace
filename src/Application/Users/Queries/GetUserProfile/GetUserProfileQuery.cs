using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetUserProfile;

[Authorize]
public record GetUserProfileQuery(string UserId) : IRequest<UserProfileDto>;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetUserProfileQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        UserProfile profile = await _context.UserProfiles
            .AsNoTracking()
            .Include(p => p.Languages)
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), request.UserId);

        int followerCount = await _context.Follows
            .AsNoTracking()
            .CountAsync(f => f.FolloweeId == request.UserId, cancellationToken);

        int followingCount = await _context.Follows
            .AsNoTracking()
            .CountAsync(f => f.FollowerId == request.UserId, cancellationToken);

        int friendCount = await _context.Friendships
            .AsNoTracking()
            .CountAsync(
                f => (f.RequesterId == request.UserId || f.AddresseeId == request.UserId)
                    && f.Status == FriendshipStatus.Accepted,
                cancellationToken);

        string? currentUserId = _currentUser.Id;
        bool isFollowedByMe = false;
        bool isFriend = false;
        bool hasOutgoingFriendRequest = false;
        bool hasIncomingFriendRequest = false;

        if (!string.IsNullOrEmpty(currentUserId) && currentUserId != request.UserId)
        {
            isFollowedByMe = await _context.Follows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == request.UserId, cancellationToken);

            // Fetch friendship including direction (RequesterId = who initiated)
            Friendship? friendship = await _context.Friendships
                .AsNoTracking()
                .FirstOrDefaultAsync(f =>
                    (f.RequesterId == currentUserId && f.AddresseeId == request.UserId)
                    || (f.RequesterId == request.UserId && f.AddresseeId == currentUserId),
                    cancellationToken);

            if (friendship is not null)
            {
                isFriend = friendship.Status == FriendshipStatus.Accepted;
                if (friendship.Status == FriendshipStatus.Pending)
                {
                    hasOutgoingFriendRequest = friendship.RequesterId == currentUserId;
                    hasIncomingFriendRequest = friendship.RequesterId == request.UserId;
                }
            }
        }

        return MapToDto(
            profile,
            isFollowedByMe,
            isFriend,
            hasOutgoingFriendRequest,
            hasIncomingFriendRequest,
            followerCount,
            followingCount,
            friendCount);
    }

    private static UserProfileDto MapToDto(
        UserProfile profile,
        bool isFollowedByMe,
        bool isFriend,
        bool hasOutgoingFriendRequest,
        bool hasIncomingFriendRequest,
        int followerCount,
        int followingCount,
        int friendCount) => new(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Bio,
            profile.AvatarUrl,
            profile.Timezone,
            profile.IsOnline,
            profile.LastSeenAt,
            profile.Languages.Select(l => new UserLanguageDto(l.Id, l.LanguageCode, l.Type.ToString(), l.Level?.ToString())).ToList(),
            isFollowedByMe,
            isFriend,
            hasOutgoingFriendRequest,
            hasIncomingFriendRequest,
            followerCount,
            followingCount,
            friendCount);
}
