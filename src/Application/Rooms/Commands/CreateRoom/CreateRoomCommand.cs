using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Rooms.Commands.CreateRoom;

[Authorize]
public record CreateRoomCommand(
    string Title,
    string? Description,
    string LanguageCode,
    int MaxParticipants,
    RoomType RoomType) : IRequest<int>;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateRoomCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        string hostId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = new()
        {
            Title = request.Title,
            Description = request.Description,
            LanguageCode = request.LanguageCode,
            MaxParticipants = request.MaxParticipants,
            RoomType = request.RoomType,
            Status = RoomStatus.Active,
            HostId = hostId,
        };

        // Host joins automatically as Host role
        RoomParticipant hostParticipant = new()
        {
            UserId = hostId,
            Role = ParticipantRole.Host,
            JoinedAt = _timeProvider.GetUtcNow(),
        };

        room.Participants.Add(hostParticipant);

        // Domain event: dispatch after SaveChanges via DispatchDomainEventsInterceptor
        room.AddDomainEvent(new UserJoinedRoomEvent(0, hostId, ParticipantRole.Host));

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync(cancellationToken);

        return room.Id;
    }
}
