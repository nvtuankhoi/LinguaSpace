using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;

namespace LinguaSpace.Application.Feed.Commands.RemoveReaction;

[Authorize]
public record RemoveReactionCommand(int PostId, string ReactionType) : IRequest;

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly INotificationService _notifications;

    public RemoveReactionCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _notifications = notifications;
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

        // Broadcast the decremented count live to everyone viewing this post.
        // Mirrors AddReaction's "NewReaction" push; the FE applies likeCount
        // absolutely, so the same event serves both add and remove.
        if (post is not null)
        {
            await _notifications.NotifyPostGroupAsync(
                request.PostId,
                "NewReaction",
                new { TargetId = request.PostId, TargetType = ReactionTargetType.Post.ToString(), LikeCount = post.LikeCount },
                cancellationToken);
        }
    }
}
