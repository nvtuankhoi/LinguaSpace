using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Users.DTOs;

namespace LinguaSpace.Application.Users.Queries.GetBlockedUsers;

[Authorize]
public record GetBlockedUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserSummaryDto>>;

public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, PaginatedResult<UserSummaryDto>>
{
    private const int MaxPageSize = 50;
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetBlockedUsersQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<UserSummaryDto>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        int pageSize = Math.Min(request.PageSize, MaxPageSize);
        int skip = (request.Page - 1) * pageSize;

        IQueryable<UserSummaryDto> query =
            from block in _context.UserBlocks.AsNoTracking()
            join profile in _context.UserProfiles.AsNoTracking() on block.BlockedId equals profile.UserId
            where block.BlockerId == userId
            orderby block.Created descending
            select new UserSummaryDto(profile.Id, profile.UserId, profile.DisplayName, profile.AvatarUrl, profile.IsOnline);

        int totalCount = await query.CountAsync(cancellationToken);
        IList<UserSummaryDto> items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return new PaginatedResult<UserSummaryDto>(items, totalCount, request.Page, pageSize, skip + items.Count < totalCount);
    }
}
