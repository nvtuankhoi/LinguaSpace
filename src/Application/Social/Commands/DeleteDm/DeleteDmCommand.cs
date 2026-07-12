using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Commands.DeleteDm;

[Authorize]
public record DeleteDmCommand(int MessageId) : IRequest;

public class DeleteDmCommandHandler : IRequestHandler<DeleteDmCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    private readonly INotificationService _notificationService;

    public DeleteDmCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notificationService)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task Handle(DeleteDmCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DirectMessage message = await _context.DirectMessages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectMessage), request.MessageId);

        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException();
        }

        Conversation conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), message.ConversationId.ToString());

        string recipientId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        message.IsDeleted = true;
        message.Content = "[deleted]";

        await _context.SaveChangesAsync(cancellationToken);

        // Live-sync the deletion to the other participant.
        await _notificationService.NotifyAsync(
            recipientId,
            "DirectMessageDeleted",
            new { message.Id, message.ConversationId },
            cancellationToken);
    }
}
