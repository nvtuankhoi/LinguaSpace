namespace LinguaSpace.Domain.Events;

/// <summary>
/// Raised by RegisterCommand after ApplicationUser is successfully created.
/// Handler: creates the corresponding UserProfile entity.
/// </summary>
public class UserRegisteredEvent : BaseEvent
{
    public UserRegisteredEvent(string userId, string email)
    {
        UserId = userId;
        Email = email;
    }

    public string UserId { get; }

    public string Email { get; }
}
