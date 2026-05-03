using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Social.DTOs;

namespace LinguaSpace.Application.Social.Queries.GetConversations;

[Authorize]
public record GetConversationsQuery : IRequest<IList<ConversationDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, IList<ConversationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetConversationsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<ConversationDto>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        return await _context.Conversations
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ConversationDto(
                c.Id,
                c.User1Id == userId ? c.User2Id : c.User1Id,
                c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Content).FirstOrDefault(),
                c.LastMessageAt,
                c.User1Id == userId ? c.UnreadCountUser1 : c.UnreadCountUser2))
            .ToListAsync(cancellationToken);
    }
}
