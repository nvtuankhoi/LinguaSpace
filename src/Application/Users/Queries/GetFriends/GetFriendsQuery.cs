using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetFriends;

/// <summary>Returns accepted friends for the given userId.</summary>
[Authorize]
public record GetFriendsQuery(string UserId) : IRequest<IList<UserSummaryDto>>;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, IList<UserSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFriendsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<UserSummaryDto>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        // Friendships are stored with RequesterId + AddresseeId, either side can be the "friend"
        IList<string> friendUserIds = await _context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted
                     && (f.RequesterId == request.UserId || f.AddresseeId == request.UserId))
            .Select(f => f.RequesterId == request.UserId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(cancellationToken);

        IList<UserProfile> profiles = await _context.UserProfiles
            .Where(p => friendUserIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        return profiles
            .Select(p => new UserSummaryDto(p.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.IsOnline))
            .ToList();
    }
}
