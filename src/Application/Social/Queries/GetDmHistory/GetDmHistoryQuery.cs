using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Social.DTOs;

namespace LinguaSpace.Application.Social.Queries.GetDmHistory;

[Authorize]
public record GetDmHistoryQuery(
    int ConversationId,
    DateTimeOffset? BeforeCursor,
    int PageSize = 30) : IRequest<CursorPagedResult<DirectMessageDto>>;

public class GetDmHistoryQueryHandler : IRequestHandler<GetDmHistoryQuery, CursorPagedResult<DirectMessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetDmHistoryQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CursorPagedResult<DirectMessageDto>> Handle(
        GetDmHistoryQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        bool isMember = await _context.Conversations.AnyAsync(
            c => c.Id == request.ConversationId
              && (c.User1Id == userId || c.User2Id == userId),
            cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenAccessException();
        }

        IQueryable<DirectMessage> query = _context.DirectMessages
            .Where(m => m.ConversationId == request.ConversationId && !m.IsDeleted);

        if (request.BeforeCursor.HasValue)
        {
            query = query.Where(m => m.SentAt < request.BeforeCursor.Value);
        }

        IList<DirectMessageDto> raw = await query
            .OrderByDescending(m => m.SentAt)
            .Take(request.PageSize + 1)
            .Select(m => new DirectMessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Content,
                m.SentAt,
                m.IsRead,
                false,
                m.EditedAt))
            .ToListAsync(cancellationToken);

        bool hasMore = raw.Count > request.PageSize;
        IList<DirectMessageDto> items = hasMore ? raw.Take(request.PageSize).ToList() : raw;
        DateTimeOffset? nextCursor = hasMore ? items[^1].SentAt : null;

        return new CursorPagedResult<DirectMessageDto>(items, hasMore, nextCursor);
    }
}
