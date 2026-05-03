using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.RemoveReaction;

[Authorize]
public record RemoveReactionCommand(int TargetId, string TargetType) : IRequest;

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

        if (!Enum.TryParse(request.TargetType, ignoreCase: true, out ReactionTargetType targetType))
        {
            throw new ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.TargetType), "Invalid TargetType.")
            ]);
        }

        Reaction? reaction = await _context.Reactions.FirstOrDefaultAsync(
            r => r.TargetId == request.TargetId && r.TargetType == targetType && r.UserId == userId,
            cancellationToken);

        if (reaction is null)
        {
            return; // Idempotent
        }

        _context.Reactions.Remove(reaction);

        // Decrement count on parent
        if (targetType == ReactionTargetType.Post)
        {
            Post? post = await _context.Posts.FindAsync([request.TargetId], cancellationToken);
            if (post is not null && post.LikeCount > 0)
            {
                post.LikeCount--;
            }
        }
        else
        {
            Comment? comment = await _context.Comments.FindAsync([request.TargetId], cancellationToken);
            if (comment is not null && comment.LikeCount > 0)
            {
                comment.LikeCount--;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
