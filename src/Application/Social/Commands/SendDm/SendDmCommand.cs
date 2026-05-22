using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Social.DTOs;

namespace LinguaSpace.Application.Social.Commands.SendDm;

public record SendDmCommand(string RecipientId, string Content) : IRequest<DirectMessageDto>;

public class SendDmCommandValidator : AbstractValidator<SendDmCommand>
{
    public SendDmCommandValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

[Authorize]
public class SendDmCommandHandler : IRequestHandler<SendDmCommand, DirectMessageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly INotificationService _notificationService;

    public SendDmCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notificationService)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<DirectMessageDto> Handle(SendDmCommand request, CancellationToken cancellationToken)
    {
        string senderId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (senderId == request.RecipientId)
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.RecipientId), "Cannot send a message to yourself.")
            ]);
        }

        // Enforce invariant: User1Id < User2Id lexicographically
        string user1Id = string.Compare(senderId, request.RecipientId, StringComparison.Ordinal) < 0
            ? senderId
            : request.RecipientId;
        string user2Id = user1Id == senderId ? request.RecipientId : senderId;

        Conversation? conversation = await _context.Conversations
            .FirstOrDefaultAsync(
                c => c.User1Id == user1Id && c.User2Id == user2Id,
                cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                User1Id = user1Id,
                User2Id = user2Id,
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync(cancellationToken);
        }

        DirectMessage dm = new()
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = request.Content,
            SentAt = DateTimeOffset.UtcNow,
        };

        _context.DirectMessages.Add(dm);

        conversation.LastMessageAt = dm.SentAt;

        // Increment unread count for the recipient
        if (senderId == conversation.User1Id)
        {
            conversation.UnreadCountUser2++;
        }
        else
        {
            conversation.UnreadCountUser1++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Real-time notification
        await _notificationService.NotifyAsync(
            request.RecipientId,
            "NewDirectMessage",
            new { dm.Id, dm.ConversationId, SenderId = senderId, dm.Content, dm.SentAt },
            cancellationToken);

        return new DirectMessageDto(dm.Id, dm.ConversationId, senderId, dm.Content, dm.SentAt, false, false, null);
    }
}
