namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a new comment is created on a post.
/// Handlers: notify post author, increment Post.CommentCount.
/// </summary>
public class CommentCreatedEvent : BaseEvent
{
    public CommentCreatedEvent(int commentId, int postId, string authorId)
    {
        CommentId = commentId;
        PostId = postId;
        AuthorId = authorId;
    }

    public int CommentId { get; }

    public int PostId { get; }

    /// <summary>UserId of the comment author.</summary>
    public string AuthorId { get; }
}
