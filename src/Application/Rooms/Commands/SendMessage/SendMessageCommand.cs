using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.SendMessage;

[Authorize]
public record SendMessageCommand(int RoomId, string Content, MessageType Type = MessageType.Text) : IRequest<int>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<int> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Room room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId.ToString());

        if (room.Status != RoomStatus.Active)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.RoomId), "Cannot send messages to a closed room.")
            ]);
        }

        RoomParticipant? participant = await _context.RoomParticipants
            .FirstOrDefaultAsync(p => p.RoomId == request.RoomId && p.UserId == userId, cancellationToken);

        if (participant is null)
        {
            throw new ForbiddenAccessException();
        }

        if (participant.IsMuted)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.Content), "You have been muted in this room.")
            ]);
        }

        Message message = new()
        {
            RoomId = request.RoomId,
            SenderId = userId,
            Content = request.Content,
            Type = request.Type,
            SentAt = _timeProvider.GetUtcNow(),
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}
