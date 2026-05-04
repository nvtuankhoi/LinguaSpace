using LinguaSpace.Application.Users.Commands.AddLanguage;
using LinguaSpace.Application.Users.Commands.FollowUser;
using LinguaSpace.Application.Users.Commands.RemoveLanguage;
using LinguaSpace.Application.Users.Commands.RespondFriendRequest;
using LinguaSpace.Application.Users.Commands.SendFriendRequest;
using LinguaSpace.Application.Users.Commands.UnfollowUser;
using LinguaSpace.Application.Users.Commands.UnfriendUser;
using LinguaSpace.Application.Users.Commands.UpdateAvatar;
using LinguaSpace.Application.Users.Commands.UpdateProfile;
using LinguaSpace.Application.Users.DTOs;
using LinguaSpace.Application.Users.Queries.GetFollowers;
using LinguaSpace.Application.Users.Queries.GetFollowing;
using LinguaSpace.Application.Users.Queries.GetFriends;
using LinguaSpace.Application.Users.Queries.GetUserProfile;
using LinguaSpace.Application.Users.Queries.SearchUsers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Users endpoints: profile management, social features (follow, friend requests), search.
/// All write endpoints require authentication.
/// Public GET endpoints are available to anonymous users.
/// </summary>
public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        // All endpoints require authentication
        group.MapGet(GetUserProfile, "{userId}").RequireAuthorization();
        group.MapGet(SearchUsers).RequireAuthorization();

        // ─── Profile management (own account) ────────────────────────────────
        group.MapPut(UpdateProfile, "me/profile").RequireAuthorization();
        group.MapPut(UpdateAvatar, "me/avatar").RequireAuthorization();
        group.MapPost(AddLanguage, "me/languages").RequireAuthorization();
        group.MapDelete(RemoveLanguage, "me/languages/{languageId}").RequireAuthorization();

        // ─── Social features ──────────────────────────────────────────────────
        group.MapPost(SendFriendRequest, "{userId}/friend-request").RequireAuthorization();
        group.MapPut(RespondFriendRequest, "friend-requests/{requestId}").RequireAuthorization();
        group.MapPost(FollowUser, "{userId}/follow").RequireAuthorization();
        group.MapDelete(UnfollowUser, "{userId}/follow").RequireAuthorization();

        group.MapGet(GetFriends, "{userId}/friends").RequireAuthorization();
        group.MapGet(GetFollowers, "{userId}/followers").RequireAuthorization();
        group.MapGet(GetFollowing, "{userId}/following").RequireAuthorization();
        group.MapDelete(UnfriendUser, "{userId}/friendship").RequireAuthorization();
    }

    // ─── GET /api/Users/{userId} ─────────────────────────────────────────────

    [EndpointSummary("Get user profile")]
    [EndpointDescription("Returns public profile for the given userId.")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Results<Ok<UserProfileDto>, NotFound>> GetUserProfile(
        [FromRoute] string userId,
        ISender sender)
    {
        UserProfileDto? dto = await sender.Send(new GetUserProfileQuery(userId));

        if (dto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(dto);
    }

    // ─── GET /api/Users?term=&languageCode=&page=&pageSize= ─────────────────

    [EndpointSummary("Search users")]
    [EndpointDescription("Search users by display name and/or language. Offset-based pagination.")]
    [ProducesResponseType(typeof(IList<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<UserSummaryDto>>> SearchUsers(
        ISender sender,
        [FromQuery] string? term = null,
        [FromQuery] string? languageCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IList<UserSummaryDto> results = await sender.Send(
            new SearchUsersQuery(term, languageCode, page, pageSize));

        return TypedResults.Ok(results);
    }

    // ─── PUT /api/Users/me/profile ───────────────────────────────────────────

    [EndpointSummary("Update own profile")]
    [EndpointDescription("Updates the authenticated user's display name, bio, avatar, and timezone.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<NoContent> UpdateProfile(
        [FromBody] UpdateProfileCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // ─── PUT /api/Users/me/avatar ────────────────────────────────────────────

    [EndpointSummary("Update avatar URL")]
    [EndpointDescription(
        "Sets the avatar URL for the current user's profile. " +
        "The client is responsible for uploading the image and providing the final URL.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UpdateAvatar(
        [FromBody] UpdateAvatarCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // ─── POST /api/Users/me/languages ────────────────────────────────────────

    [EndpointSummary("Add language to profile")]
    [EndpointDescription("Adds a language with proficiency level to the authenticated user's profile.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Created<int>> AddLanguage(
        [FromBody] AddLanguageCommand command,
        ISender sender)
    {
        int languageId = await sender.Send(command);
        return TypedResults.Created($"/api/Users/me/languages/{languageId}", languageId);
    }

    // ─── DELETE /api/Users/me/languages/{languageCode} ──────────────────────

    [EndpointSummary("Remove language from profile")]
    [EndpointDescription("Removes the specified language entry from the authenticated user's profile.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> RemoveLanguage(
        [FromRoute] int languageId,
        ISender sender)
    {
        await sender.Send(new RemoveLanguageCommand(languageId));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Users/{userId}/friend-request ─────────────────────────────

    [EndpointSummary("Send friend request")]
    [EndpointDescription("Sends a friend request to the specified user. Fails if request already exists.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Created> SendFriendRequest(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new SendFriendRequestCommand(userId));
        return TypedResults.Created();
    }

    // ─── PUT /api/Users/friend-requests/{requestId} ──────────────────────────

    [EndpointSummary("Respond to friend request")]
    [EndpointDescription("Accept or decline a pending friend request. Only the recipient can respond.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> RespondFriendRequest(
        [FromRoute] int requestId,
        [FromBody] RespondFriendRequestBody body,
        ISender sender)
    {
        await sender.Send(new RespondFriendRequestCommand(requestId, body.Accept));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Users/{userId}/follow ─────────────────────────────────────

    [EndpointSummary("Follow a user")]
    [EndpointDescription("Follows the specified user. Idempotent — no error if already following.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Created> FollowUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new FollowUserCommand(userId));
        return TypedResults.Created();
    }

    // ─── DELETE /api/Users/{userId}/follow ───────────────────────────────────

    [EndpointSummary("Unfollow a user")]
    [EndpointDescription("Unfollows the specified user. Idempotent — no error if not following.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<NoContent> UnfollowUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new UnfollowUserCommand(userId));
        return TypedResults.NoContent();
    }

    // ─── GET /api/Users/{userId}/friends ─────────────────────────────────────

    [EndpointSummary("Get friends list")]
    [EndpointDescription("Returns accepted friends for the specified user.")]
    [ProducesResponseType(typeof(IList<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<UserSummaryDto>>> GetFriends(
        [FromRoute] string userId,
        ISender sender)
    {
        IList<UserSummaryDto> friends = await sender.Send(new GetFriendsQuery(userId));
        return TypedResults.Ok(friends);
    }

    // ─── GET /api/Users/{userId}/followers ────────────────────────────────────

    [EndpointSummary("Get followers list")]
    [EndpointDescription("Returns users who follow the specified user.")]
    [ProducesResponseType(typeof(IList<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<UserSummaryDto>>> GetFollowers(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IList<UserSummaryDto> followers = await sender.Send(new GetFollowersQuery(userId, page, pageSize));
        return TypedResults.Ok(followers);
    }

    // ─── GET /api/Users/{userId}/following ────────────────────────────────────

    [EndpointSummary("Get following list")]
    [EndpointDescription("Returns users that the specified user is following.")]
    [ProducesResponseType(typeof(IList<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<UserSummaryDto>>> GetFollowing(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IList<UserSummaryDto> following = await sender.Send(new GetFollowingQuery(userId, page, pageSize));
        return TypedResults.Ok(following);
    }

    // ─── DELETE /api/Users/{userId}/friendship ────────────────────────────────

    [EndpointSummary("Remove friendship")]
    [EndpointDescription("Deletes the accepted friendship between the current user and the specified user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UnfriendUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new UnfriendUserCommand(userId));
        return TypedResults.NoContent();
    }

    // ─── Request body record ──────────────────────────────────────────────────

    /// <summary>
    /// Body for RespondFriendRequest endpoint.
    /// A simple wrapper because RespondFriendRequestCommand has both route param (requestId)
    /// and body param (accept), so we can't bind the entire command from a single source.
    /// </summary>
    public record RespondFriendRequestBody(bool Accept);
}
