using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Commands.EditDm;

[Authorize]
public record EditDmCommand(int MessageId, string NewContent) : IRequest;

public class EditDmCommandValidator : AbstractValidator<EditDmCommand>
{
    public EditDmCommandValidator()
    {
        RuleFor(x => x.NewContent).NotEmpty().MaximumLength(2000);
    }
}

public class EditDmCommandHandler : IRequestHandler<EditDmCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    private readonly INotificationService _notificationService;

    public EditDmCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notificationService)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task Handle(EditDmCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        DirectMessage message = await _context.DirectMessages
            .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(DirectMessage), request.MessageId);

        if (message.SenderId != userId)
        {
            throw new ForbiddenAccessException();
        }

        Conversation conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == message.ConversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), message.ConversationId.ToString());

        string recipientId = conversation.User1Id == userId ? conversation.User2Id : conversation.User1Id;

        message.Content = request.NewContent;
        message.EditedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Live-sync the edit to the other participant.
        await _notificationService.NotifyAsync(
            recipientId,
            "DirectMessageEdited",
            new { message.Id, message.ConversationId, message.Content, message.EditedAt },
            cancellationToken);
    }
}
