using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.CloseRoom;

[Authorize]
public record CloseRoomCommand(int RoomId) : IRequest;

public class CloseRoomCommandHandler : IRequestHandler<CloseRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CloseRoomCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(CloseRoomCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.HostId != userId)
        {
            throw new ForbiddenAccessException();
        }

        if (room.Status == RoomStatus.Closed)
        {
            return; // Already closed — idempotent
        }

        room.Status = RoomStatus.Closed;
        room.Participants.Clear();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
