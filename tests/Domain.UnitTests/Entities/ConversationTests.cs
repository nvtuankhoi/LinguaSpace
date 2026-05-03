using LinguaSpace.Domain.Entities;

namespace LinguaSpace.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for Conversation entity (Phase 2 Social DM domain).
/// Tests the User1Id &lt; User2Id invariant and defaults.
/// </summary>
public class ConversationTests
{
    [Test]
    public void NewConversation_HasExpectedDefaults()
    {
        Conversation conversation = new();

        conversation.User1Id.ShouldBe(string.Empty);
        conversation.User2Id.ShouldBe(string.Empty);
        conversation.UnreadCountUser1.ShouldBe(0);
        conversation.UnreadCountUser2.ShouldBe(0);
        conversation.LastMessageAt.ShouldBeNull();
        conversation.Messages.ShouldBeEmpty();
    }

    [Test]
    public void User1Id_LexicographicOrder_IsEnforced_BySendDmCommand()
    {
        // The SendDmCommand enforces User1Id < User2Id before creating.
        // This test simulates the result of that logic to document the invariant.

        string userA = "aaa-user";
        string userB = "zzz-user";

        string user1Id = string.Compare(userA, userB, StringComparison.Ordinal) < 0 ? userA : userB;
        string user2Id = string.Compare(userA, userB, StringComparison.Ordinal) < 0 ? userB : userA;

        Conversation conversation = new()
        {
            User1Id = user1Id,
            User2Id = user2Id,
        };

        string.Compare(conversation.User1Id, conversation.User2Id, StringComparison.Ordinal)
            .ShouldBeLessThan(0);
    }

    [Test]
    public void UnreadCount_IncrementForRecipient_Works()
    {
        Conversation conversation = new()
        {
            User1Id = "alice",
            User2Id = "bob",
        };

        // Simulate alice sending to bob: User2 unread++
        conversation.UnreadCountUser2++;

        conversation.UnreadCountUser2.ShouldBe(1);
        conversation.UnreadCountUser1.ShouldBe(0);
    }

    [Test]
    public void UnreadCount_ResetOnRead_Works()
    {
        Conversation conversation = new()
        {
            User1Id = "alice",
            User2Id = "bob",
            UnreadCountUser2 = 5,
        };

        conversation.UnreadCountUser2 = 0;

        conversation.UnreadCountUser2.ShouldBe(0);
    }

    [Test]
    public void LastMessageAt_UpdateOnNewMessage_Works()
    {
        Conversation conversation = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        conversation.LastMessageAt = now;

        conversation.LastMessageAt.ShouldBe(now);
    }
}
