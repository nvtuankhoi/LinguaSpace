namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a new post is created.
/// Handlers: notify followers, award XP.
/// </summary>
public class PostCreatedEvent : BaseEvent
{
    public PostCreatedEvent(int postId, string authorId)
    {
        PostId = postId;
        AuthorId = authorId;
    }

    public int PostId { get; }

    public string AuthorId { get; }
}
