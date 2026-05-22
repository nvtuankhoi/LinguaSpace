using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Media.DTOs;

namespace LinguaSpace.Application.Media.Queries.GetMediaSessionStatus;

[Authorize]
public record GetMediaSessionStatusQuery(int RoomId) : IRequest<MediaSessionStatusDto>;

public class GetMediaSessionStatusQueryHandler : IRequestHandler<GetMediaSessionStatusQuery, MediaSessionStatusDto>
{
    private readonly IApplicationDbContext _context;

    public GetMediaSessionStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MediaSessionStatusDto> Handle(GetMediaSessionStatusQuery request, CancellationToken cancellationToken)
    {
        int activeParticipantCount = await _context.RoomMediaSessions
            .AsNoTracking()
            .CountAsync(session => session.RoomId == request.RoomId && session.LeftAt == null, cancellationToken);

        return new MediaSessionStatusDto(activeParticipantCount > 0, activeParticipantCount);
    }
}
