using LinguaSpace.Application.Rooms.Commands.DeleteMessage;
using LinguaSpace.Application.Rooms.Commands.SendMessage;
using LinguaSpace.Application.Rooms.DTOs;
using LinguaSpace.Application.Rooms.Queries.GetRoomMessages;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Messages endpoints — nested under /api/Rooms/{roomId}/messages.
/// Note: Real-time messaging is handled via SignalR RoomHub (/hubs/room).
/// These HTTP endpoints are for history retrieval and REST fallback.
/// </summary>
public class Messages : IEndpointGroup
{
    // Override the default /api/Messages prefix to use the nested path.
    // {roomId} here is a route parameter captured by ASP.NET Core model binding.
    public static string? RoutePrefix => "/api/Rooms/{roomId}/messages";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetRoomMessages).RequireAuthorization();
        group.MapPost(SendMessage).RequireAuthorization();
        group.MapDelete(DeleteMessage, "{messageId}").RequireAuthorization();
    }

    // ─── GET /api/Rooms/{roomId}/messages ────────────────────────────────────

    [EndpointSummary("Get room message history")]
    [EndpointDescription(
        "Returns messages for the room. Cursor-based pagination: pass beforeCursor (ISO 8601 timestamp) " +
        "to get messages older than that point. Returns in ascending order (oldest first). " +
        "Same cursor direction as DM history — both paginate backwards from newest.")]
    [ProducesResponseType(typeof(IList<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Ok<IList<MessageDto>>> GetRoomMessages(
        [FromRoute] int roomId,
        ISender sender,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 50)
    {
        IList<MessageDto> messages = await sender.Send(
            new GetRoomMessagesQuery(roomId, beforeCursor, pageSize));

        return TypedResults.Ok(messages);
    }

    // ─── POST /api/Rooms/{roomId}/messages ───────────────────────────────────

    [EndpointSummary("Send a message (REST fallback)")]
    [EndpointDescription(
        "Sends a message to the room via HTTP. Prefer the SignalR hub for real-time. " +
        "User must be a participant. Room must be active.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Created<int>> SendMessage(
        [FromRoute] int roomId,
        [FromBody] SendMessageBody body,
        ISender sender)
    {
        int messageId = await sender.Send(new SendMessageCommand(roomId, body.Content));
        return TypedResults.Created($"/api/Rooms/{roomId}/messages", messageId);
    }

    // ─── DELETE /api/Rooms/{roomId}/messages/{messageId} ─────────────────────

    [EndpointSummary("Delete a message")]
    [EndpointDescription("Soft-deletes a message. Owner or room host can delete. Content is cleared.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> DeleteMessage(
        [FromRoute] int roomId,
        [FromRoute] int messageId,
        ISender sender)
    {
        await sender.Send(new DeleteMessageCommand(messageId));
        return TypedResults.NoContent();
    }

    // ─── Request body records ─────────────────────────────────────────────────

    public record SendMessageBody(string Content);
}
