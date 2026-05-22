using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetPostComments;

public record GetPostCommentsQuery(
    int PostId,
    int? ParentCommentId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<CommentDto>>;

public class GetPostCommentsQueryHandler : IRequestHandler<GetPostCommentsQuery, PaginatedResult<CommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPostCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<CommentDto>> Handle(
        GetPostCommentsQuery request,
        CancellationToken cancellationToken)
    {
        int skip = (request.Page - 1) * request.PageSize;

        IQueryable<Comment> query = _context.Comments
            .Where(c => c.PostId == request.PostId
                     && !c.IsDeleted
                     && c.ParentCommentId == request.ParentCommentId);

        int totalCount = await query.CountAsync(cancellationToken);

        IList<CommentDto> items = await query
            .OrderBy(c => c.Created)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(c => new CommentDto(
                c.Id,
                c.PostId,
                c.AuthorId,
                c.Content,
                c.ParentCommentId,
                c.LikeCount,
                c.Created))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<CommentDto>(items, totalCount, request.Page, request.PageSize, skip + items.Count < totalCount);
    }
}
