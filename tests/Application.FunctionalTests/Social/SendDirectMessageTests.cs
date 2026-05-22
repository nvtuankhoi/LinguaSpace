using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Social.Commands.DeleteDm;
using LinguaSpace.Application.Social.Commands.EditDm;
using LinguaSpace.Application.Social.Commands.MarkDmsRead;
using LinguaSpace.Application.Social.Commands.SendDm;
using LinguaSpace.Application.Social.DTOs;
using LinguaSpace.Application.Social.Queries.GetConversations;
using LinguaSpace.Application.Social.Queries.GetDmHistory;
using LinguaSpace.Domain.Entities;

namespace LinguaSpace.Application.FunctionalTests.Social;

/// <summary>
/// Functional tests for SendDmCommand, GetConversationsQuery, GetDmHistoryQuery.
/// </summary>
public class SendDirectMessageTests : TestBase
{
    [Test]
    public async Task ShouldCreateConversationOnFirstMessage()
    {
        string senderId = await TestApp.RegisterAndSetCurrentUserAsync("sender@local");
        string recipientId = await TestApp.RunAsUserAsync("recipient@local", "Testing1234!", []);

        // Switch back to sender
        await TestApp.RegisterAndSetCurrentUserAsync("sender@local");

        DirectMessageDto dm = await TestApp.SendAsync(new SendDmCommand(recipientId, "Hello!"));

        dm.ShouldNotBeNull();
        dm.Content.ShouldBe("Hello!");
        dm.SenderId.ShouldBe(senderId);

        Conversation? convo = await TestApp.FindAsync<Conversation>(dm.ConversationId);
        convo.ShouldNotBeNull();
    }

    [Test]
    public async Task SecondMessageShouldReuseExistingConversation()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice@local");
        string bobId = await TestApp.RunAsUserAsync("bob@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice@local");
        DirectMessageDto first = await TestApp.SendAsync(new SendDmCommand(bobId, "Hi Bob"));

        await TestApp.RegisterAndSetCurrentUserAsync("alice@local");
        DirectMessageDto second = await TestApp.SendAsync(new SendDmCommand(bobId, "How are you?"));

        second.ConversationId.ShouldBe(first.ConversationId);
    }

    [Test]
    public async Task ShouldIncrementUnreadCountForRecipient()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice2@local");
        string bobId = await TestApp.RunAsUserAsync("bob2@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice2@local");
        DirectMessageDto dm = await TestApp.SendAsync(new SendDmCommand(bobId, "Unread test"));

        Conversation? convo = await TestApp.FindAsync<Conversation>(dm.ConversationId);
        convo.ShouldNotBeNull();

        // Bob is User2 or User1 depending on lexicographic order; unread count for recipient should be 1
        int unreadForBob = convo.User1Id == bobId ? convo.UnreadCountUser1 : convo.UnreadCountUser2;
        unreadForBob.ShouldBe(1);
    }

    [Test]
    public async Task GetConversationsShouldReturnConversation()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice3@local");
        string bobId = await TestApp.RunAsUserAsync("bob3@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice3@local");
        await TestApp.SendAsync(new SendDmCommand(bobId, "Convo list test"));

        PaginatedResult<ConversationDto> convos = await TestApp.SendAsync(new GetConversationsQuery());

        convos.TotalCount.ShouldBe(1);
        convos.Items.Count.ShouldBe(1);
        convos.Items[0].LastMessage.ShouldBe("Convo list test");
        convos.Items[0].OtherUserId.ShouldBe(bobId);
    }

    [Test]
    public async Task GetDmHistoryShouldReturnMessages()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice4@local");
        string bobId = await TestApp.RunAsUserAsync("bob4@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice4@local");
        DirectMessageDto first = await TestApp.SendAsync(new SendDmCommand(bobId, "First"));
        await TestApp.SendAsync(new SendDmCommand(bobId, "Second"));

        CursorPagedResult<DirectMessageDto> history = await TestApp.SendAsync(
            new GetDmHistoryQuery(first.ConversationId, null, 20));

        history.Items.Count.ShouldBe(2);
    }

    [Test]
    public async Task EditDmShouldUpdateContentAndEditedAt()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice-edit@local");
        string bobId = await TestApp.RunAsUserAsync("bob-edit@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice-edit@local");
        DirectMessageDto dm = await TestApp.SendAsync(new SendDmCommand(bobId, "Original"));

        await TestApp.SendAsync(new EditDmCommand(dm.Id, "Updated"));

        DirectMessage? updated = await TestApp.FindAsync<DirectMessage>(dm.Id);
        updated.ShouldNotBeNull();
        updated.Content.ShouldBe("Updated");
        updated.EditedAt.ShouldNotBeNull();

        CursorPagedResult<DirectMessageDto> history = await TestApp.SendAsync(
            new GetDmHistoryQuery(dm.ConversationId, null, 20));

        history.Items.Count.ShouldBe(1);
        history.Items[0].Content.ShouldBe("Updated");
        history.Items[0].EditedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task DeleteDmShouldHideMessageFromHistory()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice-delete@local");
        string bobId = await TestApp.RunAsUserAsync("bob-delete@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice-delete@local");
        DirectMessageDto dm = await TestApp.SendAsync(new SendDmCommand(bobId, "Delete me"));

        await TestApp.SendAsync(new DeleteDmCommand(dm.Id));

        DirectMessage? deleted = await TestApp.FindAsync<DirectMessage>(dm.Id);
        deleted.ShouldNotBeNull();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.Content.ShouldBe("[deleted]");

        CursorPagedResult<DirectMessageDto> history = await TestApp.SendAsync(
            new GetDmHistoryQuery(dm.ConversationId, null, 20));

        history.Items.Count.ShouldBe(0);
    }

    [Test]
    public async Task MarkDmsReadShouldResetUnreadCount()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("alice5@local");
        string bobId = await TestApp.RunAsUserAsync("bob5@local", "Testing1234!", []);

        await TestApp.RegisterAndSetCurrentUserAsync("alice5@local");
        DirectMessageDto dm = await TestApp.SendAsync(new SendDmCommand(bobId, "Mark read test"));

        // Bob marks conversation as read
        await TestApp.RunAsUserAsync("bob5@local", "Testing1234!", []);
        await TestApp.SendAsync(new MarkDmsReadCommand(dm.ConversationId));

        Conversation? convo = await TestApp.FindAsync<Conversation>(dm.ConversationId);
        convo.ShouldNotBeNull();
        int unreadForBob = convo.User1Id == bobId ? convo.UnreadCountUser1 : convo.UnreadCountUser2;
        unreadForBob.ShouldBe(0);
    }

    [Test]
    public async Task ShouldThrowValidationExceptionWhenSendingToSelf()
    {
        string userId = await TestApp.RegisterAndSetCurrentUserAsync("selfmsg@local");

        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new SendDmCommand(userId, "Talking to myself")));

        ex.Errors.ShouldContainKey("RecipientId");
    }

    [Test]
    public async Task ShouldThrowForbiddenWhenNotAuthenticated()
    {
        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new SendDmCommand("some-user", "Hello")));
    }
}
