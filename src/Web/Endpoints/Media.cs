using LinguaSpace.Application.Media.Commands.EndMediaSession;
using LinguaSpace.Application.Media.Commands.GenerateMediaToken;
using LinguaSpace.Application.Media.DTOs;
using LinguaSpace.Application.Media.Queries.GetRoomMediaParticipants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// LiveKit media session endpoints nested under rooms.
/// Route prefix: /api/Rooms
/// </summary>
public class Media : IEndpointGroup
{
    public static string? RoutePrefix => "/api/Rooms";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(GenerateToken, "{roomId}/media-token").RequireAuthorization();
        group.MapGet(GetParticipants, "{roomId}/media/participants").RequireAuthorization();
        group.MapDelete(EndSession, "{roomId}/media").RequireAuthorization();
    }

    [EndpointSummary("Generate LiveKit media token")]
    [EndpointDescription("Returns a LiveKit JWT and server URL. Client uses these to connect to the SFU.")]
    [ProducesResponseType(typeof(MediaTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Ok<MediaTokenDto>> GenerateToken(
        [FromRoute] int roomId,
        ISender sender)
    {
        MediaTokenDto token = await sender.Send(new GenerateMediaTokenCommand(roomId));
        return TypedResults.Ok(token);
    }

    [EndpointSummary("Get active media participants")]
    [EndpointDescription("Returns users currently in the voice/video session for the room.")]
    [ProducesResponseType(typeof(IList<RoomMediaParticipantDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<RoomMediaParticipantDto>>> GetParticipants(
        [FromRoute] int roomId,
        ISender sender)
    {
        IList<RoomMediaParticipantDto> participants = await sender.Send(
            new GetRoomMediaParticipantsQuery(roomId));
        return TypedResults.Ok(participants);
    }

    [EndpointSummary("End media session")]
    [EndpointDescription("Host-only: terminates the LiveKit room and disconnects all participants.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> EndSession(
        [FromRoute] int roomId,
        ISender sender)
    {
        await sender.Send(new EndMediaSessionCommand(roomId));
        return TypedResults.NoContent();
    }
}
