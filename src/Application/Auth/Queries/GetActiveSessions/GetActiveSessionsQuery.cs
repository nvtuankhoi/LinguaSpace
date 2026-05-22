using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Auth.Queries.GetActiveSessions;

[Authorize]
public record GetActiveSessionsQuery : IRequest<IList<ActiveSessionDto>>;

public record ActiveSessionDto(
    int Id,
    string? DeviceInfo,
    string? IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActiveAt);

public class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, IList<ActiveSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetActiveSessionsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<ActiveSessionDto>> Handle(
        GetActiveSessionsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        return await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new ActiveSessionDto(
                s.Id,
                s.DeviceInfo,
                s.IpAddress,
                s.CreatedAt,
                s.LastActiveAt))
            .ToListAsync(cancellationToken);
    }
}
