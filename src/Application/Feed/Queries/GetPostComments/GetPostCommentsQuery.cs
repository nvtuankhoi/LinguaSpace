using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetPostComments;

[Authorize]
public record GetPostCommentsQuery(
    int PostId,
    int? ParentCommentId,
    int Page = 1,
    int PageSize = 20) : IRequest<IList<CommentDto>>;

public class GetPostCommentsQueryHandler : IRequestHandler<GetPostCommentsQuery, IList<CommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPostCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<CommentDto>> Handle(
        GetPostCommentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Comments
            .Where(c => c.PostId == request.PostId
                     && !c.IsDeleted
                     && c.ParentCommentId == request.ParentCommentId)
            .OrderBy(c => c.Created)
            .Skip((request.Page - 1) * request.PageSize)
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
    }
}
