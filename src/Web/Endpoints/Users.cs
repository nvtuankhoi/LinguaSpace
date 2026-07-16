using LinguaSpace.Application.Common;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Users.Commands.AddLanguage;
using LinguaSpace.Application.Users.Commands.BlockUser;
using LinguaSpace.Application.Users.Commands.CancelFriendRequest;
using LinguaSpace.Application.Users.Commands.FollowUser;
using LinguaSpace.Application.Users.Commands.RemoveLanguage;
using LinguaSpace.Application.Users.Commands.RespondFriendRequest;
using LinguaSpace.Application.Users.Commands.SendFriendRequest;
using LinguaSpace.Application.Users.Commands.UnblockUser;
using LinguaSpace.Application.Users.Commands.UnfollowUser;
using LinguaSpace.Application.Users.Commands.UnfriendUser;
using LinguaSpace.Application.Users.Commands.UpdateAvatar;
using LinguaSpace.Application.Users.Commands.UpdateLanguage;
using LinguaSpace.Application.Users.Commands.UpdateProfile;
using LinguaSpace.Application.Users.DTOs;
using LinguaSpace.Application.Users.Queries.GetBlockedUsers;
using LinguaSpace.Application.Users.Queries.GetFriendRequests;
using LinguaSpace.Application.Users.Queries.GetFollowers;
using LinguaSpace.Application.Users.Queries.GetFollowing;
using LinguaSpace.Application.Users.Queries.GetFriends;
using LinguaSpace.Application.Users.Queries.GetMyLanguages;
using LinguaSpace.Application.Users.Queries.GetUserProfile;
using LinguaSpace.Application.Users.Queries.SearchUsers;
using LinguaSpace.Domain.Enums;
using LinguaSpace.Web.Services;
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
        group.MapGet(GetUserProfile, "{userId}");
        group.MapGet(SearchUsers);
        group.MapGet(GetBlockedUsers, "me/blocked").RequireAuthorization();
        group.MapGet(GetMyLanguages, "me/languages").RequireAuthorization();

        group.MapPut(UpdateProfile, "me/profile").RequireAuthorization();
        group.MapPut(UpdateAvatar, "me/avatar").RequireAuthorization();
        group.MapPost(UploadAvatar, "me/avatar/upload").RequireAuthorization();
        group.MapPost(AddLanguage, "me/languages").RequireAuthorization();
        group.MapPut(UpdateLanguage, "me/languages/{languageId}").RequireAuthorization();
        group.MapDelete(RemoveLanguage, "me/languages/{languageId}").RequireAuthorization();

        group.MapGet(GetFriendRequests, "me/friend-requests").RequireAuthorization();
        group.MapPost(SendFriendRequest, "{userId}/friend-request").RequireAuthorization();
        group.MapPut(RespondFriendRequest, "friend-requests/{requestId}").RequireAuthorization();
        group.MapDelete(CancelFriendRequest, "friend-requests/{requestId}").RequireAuthorization();
        group.MapPost(FollowUser, "{userId}/follow").RequireAuthorization();
        group.MapDelete(UnfollowUser, "{userId}/follow").RequireAuthorization();
        group.MapPost(BlockUser, "{userId}/block").RequireAuthorization();
        group.MapDelete(UnblockUser, "{userId}/block").RequireAuthorization();

        group.MapGet(GetFriends, "{userId}/friends").RequireAuthorization();
        group.MapGet(GetFollowers, "{userId}/followers").RequireAuthorization();
        group.MapGet(GetFollowing, "{userId}/following").RequireAuthorization();
        group.MapDelete(UnfriendUser, "{userId}/friendship").RequireAuthorization();
    }

    [EndpointSummary("Get user profile")]
    [EndpointDescription("Returns public profile for the given userId.")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Results<Ok<UserProfileDto>, NotFound>> GetUserProfile(
        [FromRoute] string userId,
        ISender sender)
    {
        UserProfileDto dto = await sender.Send(new GetUserProfileQuery(userId));
        return TypedResults.Ok(dto);
    }

    [EndpointSummary("Search users")]
    [EndpointDescription("Search users by display name and/or language. Offset-based pagination.")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<UserSummaryDto>>> SearchUsers(
        ISender sender,
        [FromQuery] string? term = null,
        [FromQuery] string? languageCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<UserSummaryDto> results = await sender.Send(
            new SearchUsersQuery(term, languageCode, page, pageSize));

        return TypedResults.Ok(results);
    }

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

    [EndpointSummary("Upload avatar image")]
    [EndpointDescription(
        "Stores a single avatar image (jpg, jpeg, png, webp, gif; ≤ 5 MB) and returns its public URL. " +
        "Pass the returned url to UpdateProfile.avatarUrl to persist it on the profile.")]
    [ProducesResponseType(typeof(UploadedFile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    public static async Task<Results<Ok<UploadedFile>, BadRequest<ProblemDetails>>> UploadAvatar(
        IFormFile file,
        IStorageService storage,
        IUser currentUser,
        CancellationToken cancellationToken)
    {
        string? error = MediaUploadRules.ValidateAvatar(file);
        if (error is not null)
        {
            return Reject(error);
        }

        string userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        await using Stream stream = file.OpenReadStream();
        UploadedFile stored = await storage.UploadAsync(
            "avatars", userId, stream, file.FileName, file.ContentType, cancellationToken);
        return TypedResults.Ok(stored);
    }

    private static BadRequest<ProblemDetails> Reject(string detail) =>
        TypedResults.BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = detail });

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

    [EndpointSummary("Update language level")]
    [EndpointDescription("Updates the proficiency level for one of the authenticated user's languages.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UpdateLanguage(
        [FromRoute] int languageId,
        [FromBody] UpdateLanguageBody body,
        ISender sender)
    {
        await sender.Send(new UpdateLanguageCommand(languageId, body.Level));
        return TypedResults.NoContent();
    }

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

    [EndpointSummary("Get blocked users")]
    [EndpointDescription("Returns a paginated list of users the current user has blocked.")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<UserSummaryDto>>> GetBlockedUsers(
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<UserSummaryDto> result = await sender.Send(new GetBlockedUsersQuery(page, pageSize));
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get my languages")]
    [EndpointDescription("Returns the authenticated user's native and learning languages.")]
    [ProducesResponseType(typeof(IList<UserLanguageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Ok<IList<UserLanguageDto>>> GetMyLanguages(ISender sender)
    {
        IList<UserLanguageDto> result = await sender.Send(new GetMyLanguagesQuery());
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get my friend requests")]
    [EndpointDescription("Returns incoming and outgoing pending friend requests for the authenticated user.")]
    [ProducesResponseType(typeof(IList<FriendRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Ok<PaginatedResult<FriendRequestDto>>> GetFriendRequests(
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<FriendRequestDto> requests = await sender.Send(new GetFriendRequestsQuery(page, pageSize));
        return TypedResults.Ok(requests);
    }

    [EndpointSummary("Send friend request")]
    [EndpointDescription("Sends a friend request to the specified user. Returns the requestId so the client can cancel it later. Fails if request already exists.")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<Created<int>> SendFriendRequest(
        [FromRoute] string userId,
        ISender sender)
    {
        int requestId = await sender.Send(new SendFriendRequestCommand(userId));
        return TypedResults.Created($"/api/Users/friend-requests/{requestId}", requestId);
    }

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

    [EndpointSummary("Cancel friend request")]
    [EndpointDescription("Cancels a pending friend request created by the authenticated user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> CancelFriendRequest(
        [FromRoute] int requestId,
        ISender sender)
    {
        await sender.Send(new CancelFriendRequestCommand(requestId));
        return TypedResults.NoContent();
    }

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

    [EndpointSummary("Block a user")]
    [EndpointDescription("Blocks a user, hiding their content from your feed.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<NoContent> BlockUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new BlockUserCommand(userId));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Unblock a user")]
    [EndpointDescription("Removes the block on a user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public static async Task<NoContent> UnblockUser(
        [FromRoute] string userId,
        ISender sender)
    {
        await sender.Send(new UnblockUserCommand(userId));
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get friends list")]
    [EndpointDescription("Returns accepted friends for the specified user.")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<UserSummaryDto>>> GetFriends(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<UserSummaryDto> friends = await sender.Send(new GetFriendsQuery(userId, page, pageSize));
        return TypedResults.Ok(friends);
    }

    [EndpointSummary("Get followers list")]
    [EndpointDescription("Returns users who follow the specified user.")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<UserSummaryDto>>> GetFollowers(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<UserSummaryDto> followers = await sender.Send(new GetFollowersQuery(userId, page, pageSize));
        return TypedResults.Ok(followers);
    }

    [EndpointSummary("Get following list")]
    [EndpointDescription("Returns users that the specified user is following.")]
    [ProducesResponseType(typeof(PaginatedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<UserSummaryDto>>> GetFollowing(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<UserSummaryDto> following = await sender.Send(new GetFollowingQuery(userId, page, pageSize));
        return TypedResults.Ok(following);
    }

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

    public record RespondFriendRequestBody(bool Accept);

    public record UpdateLanguageBody(LanguageLevel? Level);
}
