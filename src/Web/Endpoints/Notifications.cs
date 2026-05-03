using LinguaSpace.Application.Notifications.Commands.MarkNotificationsRead;
using LinguaSpace.Application.Notifications.DTOs;
using LinguaSpace.Application.Notifications.Queries.GetNotifications;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// In-app notification endpoints.
/// </summary>
public class Notifications : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetNotifications).RequireAuthorization();
        group.MapPost(MarkAsRead, "read").RequireAuthorization();
    }

    [EndpointSummary("Get notifications")]
    [EndpointDescription("Returns paginated notifications for the current user.")]
    [ProducesResponseType(typeof(IList<NotificationDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<NotificationDto>>> GetNotifications(
        ISender sender,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        IList<NotificationDto> notifications = await sender.Send(
            new GetNotificationsQuery(unreadOnly, page, pageSize));
        return TypedResults.Ok(notifications);
    }

    [EndpointSummary("Mark notifications as read")]
    [EndpointDescription("Mark specific notifications (by IDs) as read, or all if no IDs provided.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> MarkAsRead(
        [FromBody] MarkNotificationsReadCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }
}
