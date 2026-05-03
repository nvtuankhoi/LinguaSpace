using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Commands.MarkDmsRead;

/// <summary>Mark all unread messages in a conversation as read for the current user.</summary>
[Authorize]
public record MarkDmsReadCommand(int ConversationId) : IRequest;

public class MarkDmsReadCommandHandler : IRequestHandler<MarkDmsReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public MarkDmsReadCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(MarkDmsReadCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        Conversation conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Conversation), request.ConversationId.ToString());

        if (conversation.User1Id != userId && conversation.User2Id != userId)
        {
            throw new ForbiddenAccessException();
        }

        // Mark individual messages
        await _context.DirectMessages
            .Where(m => m.ConversationId == request.ConversationId
                     && m.SenderId != userId
                     && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), cancellationToken);

        // Reset unread counter
        if (userId == conversation.User1Id)
        {
            conversation.UnreadCountUser1 = 0;
        }
        else
        {
            conversation.UnreadCountUser2 = 0;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
