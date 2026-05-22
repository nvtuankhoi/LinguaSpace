using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Security;
using LinguaSpace.Domain.Events;

namespace LinguaSpace.Application.Feed.Commands.AddReaction;

public record AddReactionCommand(
    int PostId,
    string ReactionType) : IRequest;

public class AddReactionCommandValidator : AbstractValidator<AddReactionCommand>
{
    public AddReactionCommandValidator()
    {
        RuleFor(x => x.ReactionType).Must(t =>
            Enum.TryParse<ReactionType>(t, ignoreCase: true, out _))
            .WithMessage("Invalid ReactionType.");
    }
}

[Authorize]
public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AddReactionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id ?? throw new UnauthorizedAccessException();

        ReactionTargetType targetType = ReactionTargetType.Post;
        Enum.TryParse(request.ReactionType, ignoreCase: true, out ReactionType reactionType);

        // Idempotent: if same type already exists, do nothing
        bool exists = await _context.Reactions.AnyAsync(
            r => r.TargetId == request.PostId
              && r.TargetType == targetType
              && r.UserId == userId
              && r.Type == reactionType,
            cancellationToken);

        if (exists)
        {
            return;
        }

        // Remove any previous reaction of a different type on the same target (toggle)
        Reaction? existing = await _context.Reactions.FirstOrDefaultAsync(
            r => r.TargetId == request.PostId
              && r.TargetType == targetType
              && r.UserId == userId,
            cancellationToken);

        if (existing is not null)
        {
            _context.Reactions.Remove(existing);
            await DecrementLikeCountAsync(request.PostId, cancellationToken);
        }

        Reaction reaction = new()
        {
            TargetId = request.PostId,
            TargetType = targetType,
            UserId = userId,
            Type = reactionType,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.Reactions.Add(reaction);
        await _context.SaveChangesAsync(cancellationToken);

        reaction.AddDomainEvent(new ReactionAddedEvent(reaction.Id, request.PostId, targetType.ToString(), userId));
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task DecrementLikeCountAsync(int postId, CancellationToken cancellationToken)
    {
        Post? post = await _context.Posts.FindAsync([postId], cancellationToken);
        if (post is not null && post.LikeCount > 0)
        {
            post.LikeCount--;
        }
    }
}
