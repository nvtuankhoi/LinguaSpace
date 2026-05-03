using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Media.Commands.EndMediaSession;

[Authorize]
public record EndMediaSessionCommand(int RoomId) : IRequest;

public class EndMediaSessionCommandHandler : IRequestHandler<EndMediaSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ISfuService _sfuService;

    public EndMediaSessionCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        ISfuService sfuService)
    {
        _context = context;
        _currentUser = currentUser;
        _sfuService = sfuService;
    }

    public async Task Handle(EndMediaSessionCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.HostId != userId)
        {
            throw new ForbiddenAccessException();
        }

        string livekitRoomName = room.LiveKitRoomName ?? $"room-{room.Id}";
        await _sfuService.EndRoomAsync(livekitRoomName, cancellationToken);
    }
}
