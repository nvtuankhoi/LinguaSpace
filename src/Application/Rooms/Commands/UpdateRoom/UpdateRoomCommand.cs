using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.UpdateRoom;

[Authorize]
public record UpdateRoomCommand(
    int RoomId,
    string Title,
    string? Description,
    int MaxParticipants) : IRequest;

public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateRoomCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.HostId != userId)
        {
            throw new ForbiddenAccessException();
        }

        room.Title = request.Title;
        room.Description = request.Description;
        room.MaxParticipants = request.MaxParticipants;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
