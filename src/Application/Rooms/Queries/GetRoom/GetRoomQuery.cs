using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Rooms.DTOs;

namespace LinguaSpace.Application.Rooms.Queries.GetRoom;

[Authorize]
public record GetRoomQuery(int RoomId) : IRequest<RoomDto>;

public class GetRoomQueryHandler : IRequestHandler<GetRoomQuery, RoomDto>
{
    private readonly IApplicationDbContext _context;

    public GetRoomQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RoomDto> Handle(GetRoomQuery request, CancellationToken cancellationToken)
    {
        Room room = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        // Build participant display names from UserProfiles in a single query
        List<string> userIds = room.Participants.Select(p => p.UserId).ToList();

        Dictionary<string, (string DisplayName, string? AvatarUrl)> profileMap = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToDictionaryAsync(
                p => p.UserId,
                p => (p.DisplayName, p.AvatarUrl),
                cancellationToken);

        IList<RoomParticipantDto> participants = room.Participants
            .Select(p =>
            {
                (string displayName, string? avatarUrl) = profileMap.TryGetValue(p.UserId, out (string DisplayName, string? AvatarUrl) info)
                    ? info
                    : ("Unknown", null);

                return new RoomParticipantDto(p.UserId, displayName, avatarUrl, p.Role.ToString(), p.JoinedAt, p.IsMuted);
            })
            .ToList();

        return new RoomDto(
            room.Id,
            room.Title,
            room.Description,
            room.LanguageCode,
            room.MaxParticipants,
            participants.Count,
            room.Status,
            room.RoomType,
            room.HostId,
            room.Created,
            participants);
    }
}
