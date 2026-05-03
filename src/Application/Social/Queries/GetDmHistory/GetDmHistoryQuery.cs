using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Social.DTOs;

namespace LinguaSpace.Application.Social.Queries.GetDmHistory;

[Authorize]
public record GetDmHistoryQuery(
    int ConversationId,
    DateTimeOffset? BeforeCursor,
    int PageSize = 30) : IRequest<IList<DirectMessageDto>>;

public class GetDmHistoryQueryHandler : IRequestHandler<GetDmHistoryQuery, IList<DirectMessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetDmHistoryQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IList<DirectMessageDto>> Handle(
        GetDmHistoryQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        // Verify membership
        bool isMember = await _context.Conversations.AnyAsync(
            c => c.Id == request.ConversationId
              && (c.User1Id == userId || c.User2Id == userId),
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenAccessException();
        }

        IQueryable<DirectMessage> query = _context.DirectMessages
            .Where(m => m.ConversationId == request.ConversationId);

        if (request.BeforeCursor.HasValue)
        {
            query = query.Where(m => m.SentAt < request.BeforeCursor.Value);
        }

        return await query
            .OrderByDescending(m => m.SentAt)
            .Take(request.PageSize)
            .Select(m => new DirectMessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                m.SentAt,
                m.IsRead))
            .ToListAsync(cancellationToken);
    }
}
