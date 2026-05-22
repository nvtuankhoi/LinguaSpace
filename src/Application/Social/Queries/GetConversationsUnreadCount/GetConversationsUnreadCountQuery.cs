using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Social.Queries.GetConversationsUnreadCount;

[Authorize]
public record GetConversationsUnreadCountQuery : IRequest<ConversationsUnreadCountDto>;

public record ConversationsUnreadCountDto(int UnreadConversations, int TotalUnread);

public class GetConversationsUnreadCountQueryHandler
    : IRequestHandler<GetConversationsUnreadCountQuery, ConversationsUnreadCountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetConversationsUnreadCountQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ConversationsUnreadCountDto> Handle(
        GetConversationsUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        IList<int> unreadCounts = await _context.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => c.User1Id == userId ? c.UnreadCountUser1 : c.UnreadCountUser2)
            .Where(count => count > 0)
            .ToListAsync(cancellationToken);

        return new ConversationsUnreadCountDto(
            UnreadConversations: unreadCounts.Count,
            TotalUnread: unreadCounts.Sum());
    }
}
