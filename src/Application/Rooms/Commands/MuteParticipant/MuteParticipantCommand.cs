using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.Commands.MuteParticipant;

/// <summary>
/// Toggles the mute state of a participant in a room.
/// Only the host (ParticipantRole.Host) can mute/unmute others.
/// This controls text-chat mute (IsMuted). Voice/video mute is handled by LiveKit in Phase 2.
/// </summary>
[Authorize]
public record MuteParticipantCommand(int RoomId, string TargetUserId, bool Mute) : IRequest;

public class MuteParticipantCommandHandler : IRequestHandler<MuteParticipantCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public MuteParticipantCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(MuteParticipantCommand request, CancellationToken cancellationToken)
    {
        string callerId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        RoomParticipant? caller = room.Participants.FirstOrDefault(p => p.UserId == callerId);

        if (caller is null || caller.Role != ParticipantRole.Host)
        {
            throw new ForbiddenAccessException();
        }

        RoomParticipant target = room.Participants.FirstOrDefault(p => p.UserId == request.TargetUserId)
            ?? throw new NotFoundException(nameof(RoomParticipant), request.TargetUserId);

        target.IsMuted = request.Mute;

        // Fan out the mute change to everyone in the room (including the affected
        // user) so client mute state stays in sync. Dispatched after SaveChanges
        // via DispatchDomainEventsInterceptor.
        room.AddDomainEvent(new ParticipantMutedEvent(room.Id, target.UserId, request.Mute));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
