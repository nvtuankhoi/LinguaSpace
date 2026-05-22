using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Social.DTOs;

namespace LinguaSpace.Application.Social.Queries.GetConversations;

[Authorize]
public record GetConversationsQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<ConversationDto>>;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, PaginatedResult<ConversationDto>>
{
    private const int MaxPageSize = 50;
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public GetConversationsQueryHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedResult<ConversationDto>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();
        int pageSize = Math.Min(request.PageSize, MaxPageSize);
        int skip = (request.Page - 1) * pageSize;

        IQueryable<Conversation> query = _context.Conversations
            .AsNoTracking()
            .Where(c => c.User1Id == userId || c.User2Id == userId);

        int totalCount = await query.CountAsync(cancellationToken);

        IList<ConversationProjection> conversations = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(c => new ConversationProjection(
                c.Id,
                c.User1Id == userId ? c.User2Id : c.User1Id,
                c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Content)
                    .FirstOrDefault(),
                c.LastMessageAt,
                c.User1Id == userId ? c.UnreadCountUser1 : c.UnreadCountUser2))
            .ToListAsync(cancellationToken);

        List<string> otherUserIds = conversations
            .Select(c => c.OtherUserId)
            .Distinct()
            .ToList();

        Dictionary<string, UserProfile> profiles = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => otherUserIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        IList<ConversationDto> items = conversations
            .Select(c =>
            {
                profiles.TryGetValue(c.OtherUserId, out UserProfile? profile);
                return new ConversationDto(
                    c.Id,
                    c.OtherUserId,
                    profile?.DisplayName,
                    profile?.AvatarUrl,
                    c.LastMessage,
                    c.LastMessageAt,
                    c.UnreadCount);
            })
            .ToList();

        bool hasMore = skip + items.Count < totalCount;
        return new PaginatedResult<ConversationDto>(items, totalCount, request.Page, pageSize, hasMore);
    }
}

internal sealed record ConversationProjection(
    int Id,
    string OtherUserId,
    string? LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);
