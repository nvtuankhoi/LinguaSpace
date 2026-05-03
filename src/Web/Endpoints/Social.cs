using LinguaSpace.Application.Social.Commands.MarkDmsRead;
using LinguaSpace.Application.Social.Commands.SendDm;
using LinguaSpace.Application.Social.DTOs;
using LinguaSpace.Application.Social.Queries.GetConversations;
using LinguaSpace.Application.Social.Queries.GetDmHistory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Social DM endpoints: conversations and direct messages.
/// </summary>
public class Social : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetConversations, "conversations").RequireAuthorization();
        group.MapGet(GetDmHistory, "conversations/{conversationId}/messages").RequireAuthorization();
        group.MapPost(SendDm, "dm").RequireAuthorization();
        group.MapPost(MarkDmsRead, "conversations/{conversationId}/read").RequireAuthorization();
    }

    [EndpointSummary("List conversations")]
    [EndpointDescription("Returns all DM conversations for the current user, ordered by most recent.")]
    [ProducesResponseType(typeof(IList<ConversationDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<ConversationDto>>> GetConversations(ISender sender)
    {
        IList<ConversationDto> conversations = await sender.Send(new GetConversationsQuery());
        return TypedResults.Ok(conversations);
    }

    [EndpointSummary("Get DM history")]
    [EndpointDescription("Returns messages in a conversation. Cursor-based pagination using beforeCursor.")]
    [ProducesResponseType(typeof(IList<DirectMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public static async Task<Ok<IList<DirectMessageDto>>> GetDmHistory(
        [FromRoute] int conversationId,
        ISender sender,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 30)
    {
        IList<DirectMessageDto> messages = await sender.Send(
            new GetDmHistoryQuery(conversationId, beforeCursor, pageSize));
        return TypedResults.Ok(messages);
    }

    [EndpointSummary("Send a direct message")]
    [EndpointDescription("Creates or reuses a conversation and sends a message. Returns the message.")]
    [ProducesResponseType(typeof(DirectMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<Created<DirectMessageDto>> SendDm(
        [FromBody] SendDmCommand command,
        ISender sender)
    {
        DirectMessageDto dm = await sender.Send(command);
        return TypedResults.Created($"/api/Social/conversations/{dm.ConversationId}/messages", dm);
    }

    [EndpointSummary("Mark messages as read")]
    [EndpointDescription("Marks all unread messages in a conversation as read and resets the unread counter.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public static async Task<NoContent> MarkDmsRead(
        [FromRoute] int conversationId,
        ISender sender)
    {
        await sender.Send(new MarkDmsReadCommand(conversationId));
        return TypedResults.NoContent();
    }
}
