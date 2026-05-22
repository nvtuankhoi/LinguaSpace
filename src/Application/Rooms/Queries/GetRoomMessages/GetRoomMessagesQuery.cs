using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Rooms.DTOs;

namespace LinguaSpace.Application.Rooms.Queries.GetRoomMessages;

/// <summary>
/// Cursor-based pagination for room messages.
/// Pass BeforeCursor (a SentAt DateTimeOffset) to get messages older than that timestamp.
/// Null BeforeCursor returns the latest PageSize messages.
/// Matches DM history cursor direction (both load newest-first, paginate backwards).
/// </summary>
[Authorize]
public record GetRoomMessagesQuery(int RoomId, DateTimeOffset? BeforeCursor, int PageSize = 50)
    : IRequest<IList<MessageDto>>;

public class GetRoomMessagesQueryHandler : IRequestHandler<GetRoomMessagesQuery, IList<MessageDto>>
{
    private const int MaxPageSize = 100;
    private readonly IApplicationDbContext _context;

    public GetRoomMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<MessageDto>> Handle(GetRoomMessagesQuery request, CancellationToken cancellationToken)
    {
        bool roomExists = await _context.Rooms.AnyAsync(r => r.Id == request.RoomId, cancellationToken);

        if (!roomExists)
        {
            throw new NotFoundException(nameof(Room), request.RoomId.ToString());
        }

        int pageSize = Math.Min(request.PageSize, MaxPageSize);

        IQueryable<Message> query = _context.Messages
            .AsNoTracking()
            .Where(m => m.RoomId == request.RoomId);

        if (request.BeforeCursor.HasValue)
        {
            query = query.Where(m => m.SentAt < request.BeforeCursor.Value);
        }

        List<Message> messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Fetch sender display names in one query
        List<string> senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

        Dictionary<string, string> senderNames = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => senderIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, p => p.DisplayName, cancellationToken);

        // Return in ascending order (oldest first) for chat display
        return messages
            .OrderBy(m => m.SentAt)
            .Select(m => new MessageDto(
                m.Id,
                m.SenderId,
                senderNames.GetValueOrDefault(m.SenderId, "Unknown"),
                m.Content,
                m.Type,
                m.SentAt,
                m.IsDeleted))
            .ToList();
    }
}
