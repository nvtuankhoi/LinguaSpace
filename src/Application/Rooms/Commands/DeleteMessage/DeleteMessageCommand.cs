using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Rooms.Commands.DeleteMessage;

[Authorize]
public record DeleteMessageCommand(int MessageId) : IRequest;

public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteMessageCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Message message = await _context.Messages
            .Include(m => m.Room)
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId.ToString());

        bool isOwner = message.SenderId == userId;
        bool isHost = message.Room.HostId == userId;

        if (!isOwner && !isHost)
        {
            throw new ForbiddenAccessException();
        }

        // Soft delete: preserve message in chat history with [deleted] placeholder
        message.IsDeleted = true;
        message.Content = string.Empty;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
