using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

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

        // Verify caller is host of the room
        RoomParticipant? callerParticipant = await _context.RoomParticipants
            .FirstOrDefaultAsync(
                p => p.RoomId == request.RoomId && p.UserId == callerId,
                cancellationToken);

        if (callerParticipant is null || callerParticipant.Role != ParticipantRole.Host)
        {
            throw new ForbiddenAccessException();
        }

        RoomParticipant? target = await _context.RoomParticipants
            .FirstOrDefaultAsync(
                p => p.RoomId == request.RoomId && p.UserId == request.TargetUserId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(RoomParticipant), request.TargetUserId);

        target.IsMuted = request.Mute;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
