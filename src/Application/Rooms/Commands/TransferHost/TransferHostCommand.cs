using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.TransferHost;

[Authorize]
public record TransferHostCommand(int RoomId, string TargetUserId) : IRequest;

public class TransferHostCommandHandler : IRequestHandler<TransferHostCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public TransferHostCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(TransferHostCommand request, CancellationToken cancellationToken)
    {
        string currentUserId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(candidate => candidate.Participants)
            .FirstOrDefaultAsync(candidate => candidate.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.HostId != currentUserId)
        {
            throw new ForbiddenAccessException();
        }

        RoomParticipant currentHostParticipant = room.Participants
            .FirstOrDefault(participant => participant.UserId == currentUserId)
            ?? throw new NotFoundException(nameof(RoomParticipant), currentUserId);

        RoomParticipant targetParticipant = room.Participants
            .FirstOrDefault(participant => participant.UserId == request.TargetUserId)
            ?? throw new NotFoundException(nameof(RoomParticipant), request.TargetUserId);

        room.HostId = request.TargetUserId;
        currentHostParticipant.Role = ParticipantRole.Speaker;
        targetParticipant.Role = ParticipantRole.Host;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
