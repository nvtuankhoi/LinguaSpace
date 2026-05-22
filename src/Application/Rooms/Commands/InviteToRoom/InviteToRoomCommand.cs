using System.Text.Json;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.InviteToRoom;

[Authorize]
public record InviteToRoomCommand(int RoomId, string TargetUserId) : IRequest;

public class InviteToRoomCommandHandler : IRequestHandler<InviteToRoomCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public InviteToRoomCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(InviteToRoomCommand request, CancellationToken cancellationToken)
    {
        string inviterId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .Include(candidate => candidate.Participants)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.RoomId && candidate.Status == RoomStatus.Active,
                cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        bool isParticipant = room.Participants.Any(participant => participant.UserId == inviterId);

        if (!isParticipant)
        {
            throw new ForbiddenAccessException();
        }

        bool targetAlreadyInRoom = room.Participants.Any(participant => participant.UserId == request.TargetUserId);

        if (targetAlreadyInRoom)
        {
            return;
        }

        Notification notification = new()
        {
            RecipientId = request.TargetUserId,
            Type = NotificationType.RoomInvite,
            Payload = JsonSerializer.Serialize(new
            {
                roomId = room.Id,
                inviterId,
                roomTitle = room.Title,
            }),
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
