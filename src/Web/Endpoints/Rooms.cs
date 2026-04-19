using LinguaSpace.Application.Rooms.Commands.CloseRoom;
using LinguaSpace.Application.Rooms.Commands.CreateRoom;
using LinguaSpace.Application.Rooms.Commands.JoinRoom;
using LinguaSpace.Application.Rooms.Commands.LeaveRoom;
using LinguaSpace.Application.Rooms.Commands.UpdateRoom;
using LinguaSpace.Application.Rooms.DTOs;
using LinguaSpace.Application.Rooms.Queries.GetRoom;
using LinguaSpace.Application.Rooms.Queries.GetRooms;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Room endpoints: browse, create, join, leave, update, close.
/// </summary>
public class Rooms : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        // List and get require auth — all app features require authentication
        group.MapGet(GetRooms).RequireAuthorization();
        group.MapGet(GetRoom, "{roomId}").RequireAuthorization();

        group.MapPost(CreateRoom).RequireAuthorization();
        group.MapPut(UpdateRoom, "{roomId}").RequireAuthorization();
        group.MapPost(JoinRoom, "{roomId}/join").RequireAuthorization();
        group.MapPost(LeaveRoom, "{roomId}/leave").RequireAuthorization();
        group.MapDelete(CloseRoom, "{roomId}").RequireAuthorization();
    }

    // ─── GET /api/Rooms?languageCode=&roomType=&page=&pageSize= ─────────────

    [EndpointSummary("List active rooms")]
    [EndpointDescription("Returns paginated list of active rooms. Filter by language and room type.")]
    [ProducesResponseType(typeof(IList<RoomSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<RoomSummaryDto>>> GetRooms(
        ISender sender,
        [FromQuery] string? languageCode = null,
        [FromQuery] string? roomType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IList<RoomSummaryDto> results = await sender.Send(
            new GetRoomsQuery(languageCode, roomType, page, pageSize));

        return TypedResults.Ok(results);
    }

    // ─── GET /api/Rooms/{roomId} ─────────────────────────────────────────────

    [EndpointSummary("Get room details")]
    [EndpointDescription("Returns room details including current participants.")]
    [ProducesResponseType(typeof(RoomDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Results<Ok<RoomDto>, NotFound>> GetRoom(
        [FromRoute] int roomId,
        ISender sender)
    {
        RoomDto? dto = await sender.Send(new GetRoomQuery(roomId));

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    // ─── POST /api/Rooms ─────────────────────────────────────────────────────

    [EndpointSummary("Create a room")]
    [EndpointDescription("Creates a new room. The creator automatically joins as Host.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Created<int>> CreateRoom(
        [FromBody] CreateRoomCommand command,
        ISender sender)
    {
        int roomId = await sender.Send(command);
        return TypedResults.Created($"/api/Rooms/{roomId}", roomId);
    }

    // ─── PUT /api/Rooms/{roomId} ─────────────────────────────────────────────

    [EndpointSummary("Update room")]
    [EndpointDescription("Updates room metadata. Only the host can update a room.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UpdateRoom(
        [FromRoute] int roomId,
        [FromBody] UpdateRoomBody body,
        ISender sender)
    {
        await sender.Send(new UpdateRoomCommand(roomId, body.Title, body.Description, body.MaxParticipants));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Rooms/{roomId}/join ───────────────────────────────────────

    [EndpointSummary("Join a room")]
    [EndpointDescription("Joins the specified room. Idempotent. Fails if room is full or closed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> JoinRoom(
        [FromRoute] int roomId,
        ISender sender)
    {
        await sender.Send(new JoinRoomCommand(roomId));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Rooms/{roomId}/leave ─────────────────────────────────────

    [EndpointSummary("Leave a room")]
    [EndpointDescription("Leaves the specified room. Idempotent. If host leaves, room is closed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> LeaveRoom(
        [FromRoute] int roomId,
        ISender sender)
    {
        await sender.Send(new LeaveRoomCommand(roomId));
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Rooms/{roomId} ──────────────────────────────────────────

    [EndpointSummary("Close a room")]
    [EndpointDescription("Permanently closes the room. Only the host can close it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> CloseRoom(
        [FromRoute] int roomId,
        ISender sender)
    {
        await sender.Send(new CloseRoomCommand(roomId));
        return TypedResults.NoContent();
    }

    // ─── Request body records ─────────────────────────────────────────────────

    /// <summary>
    /// Body for UpdateRoom. Separate from UpdateRoomCommand because roomId comes from route.
    /// </summary>
    public record UpdateRoomBody(string Title, string? Description, int MaxParticipants);
}
