using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.Commands.JoinRoom;

[Authorize]
public record JoinRoomCommand(int RoomId, ParticipantRole Role = ParticipantRole.Speaker) : IRequest;

public class JoinRoomCommandHandler : IRequestHandler<JoinRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public JoinRoomCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.Status != RoomStatus.Active)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.RoomId), "Room is closed.")
            ]);
        }

        bool alreadyJoined = room.Participants.Any(p => p.UserId == userId);

        if (alreadyJoined)
        {
            return; // Idempotent — joining a room you're already in is a no-op
        }

        if (room.Participants.Count >= room.MaxParticipants)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.RoomId), "Room is full.")
            ]);
        }

        // Non-host can only join as Speaker or Listener
        ParticipantRole role = request.Role == ParticipantRole.Host ? ParticipantRole.Speaker : request.Role;

        RoomParticipant participant = new()
        {
            UserId = userId,
            Role = role,
            JoinedAt = _timeProvider.GetUtcNow(),
        };

        room.Participants.Add(participant);
        room.AddDomainEvent(new UserJoinedRoomEvent(room.Id, userId, role));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
