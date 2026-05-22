using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Notifications.Commands.DeleteNotifications;
using LinguaSpace.Application.Notifications.Commands.MarkNotificationsRead;
using LinguaSpace.Application.Notifications.DTOs;
using LinguaSpace.Application.Notifications.Queries.GetNotifications;
using LinguaSpace.Application.Notifications.Queries.GetUnreadCount;
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
        group.MapGet(GetUnreadCount, "unread-count").RequireAuthorization();
        group.MapPost(MarkAsRead, "read").RequireAuthorization();
        group.MapPost(DeleteNotifications, "delete-batch").RequireAuthorization();
    }

    [EndpointSummary("Get notifications")]
    [EndpointDescription("Returns paginated notifications for the current user.")]
    [ProducesResponseType(typeof(PaginatedResult<NotificationDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<NotificationDto>>> GetNotifications(
        ISender sender,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        PaginatedResult<NotificationDto> notifications = await sender.Send(
            new GetNotificationsQuery(unreadOnly, page, pageSize));
        return TypedResults.Ok(notifications);
    }

    [EndpointSummary("Get unread notification count")]
    [EndpointDescription("Returns the count of unread notifications for the current user.")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public static async Task<Ok<int>> GetUnreadCount(ISender sender)
    {
        int count = await sender.Send(new GetUnreadCountQuery());
        return TypedResults.Ok(count);
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

    [EndpointSummary("Delete notifications (batch)")]
    [EndpointDescription("Deletes specific notifications by IDs, or all notifications if no IDs provided. POST is used instead of DELETE to allow a request body.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> DeleteNotifications(
        ISender sender,
        [Microsoft.AspNetCore.Mvc.FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteNotificationsBody? body = null)
    {
        await sender.Send(new DeleteNotificationsCommand(body?.NotificationIds));
        return TypedResults.NoContent();
    }

    public record DeleteNotificationsBody(IList<int>? NotificationIds = null);
}
