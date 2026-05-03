using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetPost;

[Authorize]
public record GetPostQuery(int PostId) : IRequest<PostDto?>;

public class GetPostQueryHandler : IRequestHandler<GetPostQuery, PostDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPostQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostDto?> Handle(GetPostQuery request, CancellationToken cancellationToken)
    {
        Post? post = await _context.Posts
            .Include(p => p.Tags)
            .Include(p => p.MediaItems)
            .Include(p => p.Comments.Where(c => !c.IsDeleted && c.ParentCommentId == null))
            .FirstOrDefaultAsync(p => p.Id == request.PostId && !p.IsDeleted, cancellationToken);

        if (post is null)
        {
            return null;
        }

        return new PostDto(
            post.Id,
            post.AuthorId,
            post.Content,
            post.PostType.ToString(),
            post.LanguageCode,
            post.Metadata,
            post.LikeCount,
            post.CommentCount,
            post.Created,
            post.Tags.Select(t => t.Tag).ToList(),
            post.MediaItems.OrderBy(m => m.SortOrder).Select(m => new MediaItemDto(m.Id, m.Url, m.SortOrder)).ToList(),
            post.Comments.Select(c => new CommentDto(c.Id, c.PostId, c.AuthorId, c.Content, c.ParentCommentId, c.LikeCount, c.Created)).ToList());
    }
}
