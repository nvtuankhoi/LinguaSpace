using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.RemoveReaction;

[Authorize]
public record RemoveReactionCommand(int PostId, string ReactionType) : IRequest;

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public RemoveReactionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        if (!Enum.TryParse(request.ReactionType, ignoreCase: true, out ReactionType reactionType))
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.ReactionType), "Invalid ReactionType.")
            ]);
        }

        Reaction? reaction = await _context.Reactions.FirstOrDefaultAsync(
            r => r.TargetId == request.PostId
              && r.TargetType == ReactionTargetType.Post
              && r.UserId == userId
              && r.Type == reactionType,
            cancellationToken);

        if (reaction is null)
        {
            return; // Idempotent
        }

        _context.Reactions.Remove(reaction);

        Post? post = await _context.Posts.FindAsync([request.PostId], cancellationToken);
        if (post is not null && post.LikeCount > 0)
        {
            post.LikeCount--;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
