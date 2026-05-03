namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised when a friend request is sent.
/// Handlers: create a Notification for the addressee.
/// </summary>
public class FriendRequestSentEvent : BaseEvent
{
    public FriendRequestSentEvent(int friendshipId, string requesterId, string addresseeId)
    {
        FriendshipId = friendshipId;
        RequesterId = requesterId;
        AddresseeId = addresseeId;
    }

    public int FriendshipId { get; }

    public string RequesterId { get; }

    public string AddresseeId { get; }
}
