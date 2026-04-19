using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.Commands.LeaveRoom;

[Authorize]
public record LeaveRoomCommand(int RoomId) : IRequest;

public class LeaveRoomCommandHandler : IRequestHandler<LeaveRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public LeaveRoomCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        RoomParticipant? participant = room.Participants.FirstOrDefault(p => p.UserId == userId);

        if (participant is null)
        {
            return; // Idempotent — leaving a room you're not in is a no-op
        }

        bool wasHost = participant.Role == ParticipantRole.Host;

        room.Participants.Remove(participant);
        room.AddDomainEvent(new UserLeftRoomEvent(room.Id, userId, wasHost));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
