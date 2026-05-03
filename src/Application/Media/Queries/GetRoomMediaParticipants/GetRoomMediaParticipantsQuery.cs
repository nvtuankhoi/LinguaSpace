using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Media.DTOs;

namespace LinguaSpace.Application.Media.Queries.GetRoomMediaParticipants;

[Authorize]
public record GetRoomMediaParticipantsQuery(int RoomId) : IRequest<IList<RoomMediaParticipantDto>>;

public class GetRoomMediaParticipantsQueryHandler
    : IRequestHandler<GetRoomMediaParticipantsQuery, IList<RoomMediaParticipantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRoomMediaParticipantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<RoomMediaParticipantDto>> Handle(
        GetRoomMediaParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.RoomMediaSessions
            .Where(s => s.RoomId == request.RoomId && s.LeftAt == null)
            .Select(s => new RoomMediaParticipantDto(
                s.UserId,
                s.JoinedAt,
                s.LeftAt,
                s.DurationSeconds,
                s.WasScreenSharing,
                s.LeftAt == null))
            .ToListAsync(cancellationToken);
    }
}
