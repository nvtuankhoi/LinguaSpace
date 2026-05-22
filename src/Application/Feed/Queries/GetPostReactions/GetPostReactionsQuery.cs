using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Application.Feed.DTOs;

namespace LinguaSpace.Application.Feed.Queries.GetPostReactions;

[Authorize]
public record GetPostReactionsQuery(int PostId) : IRequest<IList<ReactionDetailDto>>;

public class GetPostReactionsQueryHandler : IRequestHandler<GetPostReactionsQuery, IList<ReactionDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPostReactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<ReactionDetailDto>> Handle(
        GetPostReactionsQuery request,
        CancellationToken cancellationToken)
    {
        IList<ReactionDetailDto> reactions = await (
            from reaction in _context.Reactions.AsNoTracking()
            join userProfile in _context.UserProfiles.AsNoTracking()
                on reaction.UserId equals userProfile.UserId into userProfiles
            from userProfile in userProfiles.DefaultIfEmpty()
            where reaction.TargetType == ReactionTargetType.Post
                && reaction.TargetId == request.PostId
            orderby reaction.CreatedAt descending
            select new ReactionDetailDto(
                reaction.UserId,
                userProfile != null ? userProfile.DisplayName : reaction.UserId,
                userProfile != null ? userProfile.AvatarUrl : null,
                reaction.Type.ToString(),
                reaction.CreatedAt))
            .ToListAsync(cancellationToken);

        return reactions;
    }
}
