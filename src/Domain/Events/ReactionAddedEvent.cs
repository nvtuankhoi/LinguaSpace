namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a reaction is added to a post or comment.
/// Handlers: notify target owner, increment LikeCount on the target.
/// </summary>
public class ReactionAddedEvent : BaseEvent
{
    public ReactionAddedEvent(int reactionId, int targetId, string targetType, string reactorId)
    {
        ReactionId = reactionId;
        TargetId = targetId;
        TargetType = targetType;
        ReactorId = reactorId;
    }

    public int ReactionId { get; }

    public int TargetId { get; }

    /// <summary>"Post" or "Comment".</summary>
    public string TargetType { get; }

    /// <summary>UserId of the user who reacted.</summary>
    public string ReactorId { get; }
}
