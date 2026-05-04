using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.KickParticipant;

/// <summary>
/// Removes a participant from the room via LiveKit SFU and deletes DB membership.
/// Only the room host can kick other participants.
/// </summary>
[Authorize]
public record KickParticipantCommand(int RoomId, string TargetUserId) : IRequest;

public class KickParticipantCommandHandler : IRequestHandler<KickParticipantCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ISfuService _sfuService;

    public KickParticipantCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        ISfuService sfuService)
    {
        _context = context;
        _currentUser = currentUser;
        _sfuService = sfuService;
    }

    public async Task Handle(KickParticipantCommand request, CancellationToken cancellationToken)
    {
        string callerId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        // Caller must be host
        RoomParticipant? caller = await _context.RoomParticipants
            .FirstOrDefaultAsync(
                p => p.RoomId == request.RoomId && p.UserId == callerId,
                cancellationToken);

        if (caller is null || caller.Role != ParticipantRole.Host)
        {
            throw new ForbiddenAccessException();
        }

        RoomParticipant? target = await _context.RoomParticipants
            .Include(p => p.Room)
            .FirstOrDefaultAsync(
                p => p.RoomId == request.RoomId && p.UserId == request.TargetUserId,
                cancellationToken)
            ?? throw new NotFoundException(nameof(RoomParticipant), request.TargetUserId);

        // Kick from LiveKit SFU first (best-effort — ignore if room not in SFU or text-only)
        string livekitRoomName = target.Room.LiveKitRoomName ?? $"room-{request.RoomId}";
        try
        {
            await _sfuService.RemoveParticipantAsync(livekitRoomName, request.TargetUserId, cancellationToken);
        }
        catch
        {
            // LiveKit room may not exist; proceed with DB removal
        }

        _context.RoomParticipants.Remove(target);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
