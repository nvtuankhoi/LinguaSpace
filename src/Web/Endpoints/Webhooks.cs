using LinguaSpace.Application.Media.Commands.HandleLiveKitWebhook;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Webhook endpoints — no JWT auth; integrity is verified via HMAC inside the handler.
/// </summary>
public class Webhooks : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        // No RequireAuthorization — LiveKit server cannot provide a user JWT.
        group.MapPost(LiveKit, "livekit");
    }

    /// <summary>
    /// Receives LiveKit server-side webhook events (participant joined/left, room finished, etc.).
    /// HMAC-SHA256 signature is verified inside <see cref="HandleLiveKitWebhookCommand"/>.
    /// Content-Type is application/webhook+json (LiveKit-specific).
    /// </summary>
    [EndpointSummary("LiveKit webhook receiver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public static async Task<IResult> LiveKit(
        HttpRequest httpRequest,
        ISender sender)
    {
        // Read raw body as string for HMAC verification — do NOT bind to a model
        using StreamReader reader = new(httpRequest.Body);
        string rawBody = await reader.ReadToEndAsync();

        string authHeader = httpRequest.Headers.Authorization.ToString();

        await sender.Send(new HandleLiveKitWebhookCommand(rawBody, authHeader));

        return Results.Ok();
    }
}
