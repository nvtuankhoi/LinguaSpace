using LinguaSpace.Application.Common;
using LinguaSpace.Application.Common.Interfaces;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Feed.Commands.AddReaction;
using LinguaSpace.Application.Feed.Commands.CreateComment;
using LinguaSpace.Application.Feed.Commands.CreatePost;
using LinguaSpace.Application.Feed.Commands.DeleteComment;
using LinguaSpace.Application.Feed.Commands.DeletePost;
using LinguaSpace.Application.Feed.Commands.RemoveReaction;
using LinguaSpace.Application.Feed.Commands.UpdateComment;
using LinguaSpace.Application.Feed.Commands.UpdatePost;
using LinguaSpace.Application.Feed.DTOs;
using LinguaSpace.Application.Feed.Queries.GetExplore;
using LinguaSpace.Application.Feed.Queries.GetFeed;
using LinguaSpace.Application.Feed.Queries.GetPost;
using LinguaSpace.Application.Feed.Queries.GetPostComments;
using LinguaSpace.Application.Feed.Queries.GetPostReactions;
using LinguaSpace.Application.Feed.Queries.GetUserPosts;
using LinguaSpace.Application.Feed.Queries.SearchPosts;
using LinguaSpace.Web.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace LinguaSpace.Web.Endpoints;

/// <summary>
/// Social feed: posts, comments, reactions.
/// Route: /api/Feed
/// </summary>
public class Feed : IEndpointGroup
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetFeed).RequireAuthorization();
        group.MapGet(GetExplore, "explore");
        group.MapGet(SearchPosts, "search");
        group.MapGet(GetUserPosts, "users/{userId}");
        group.MapGet(GetPost, "posts/{postId}");
        group.MapGet(GetPostComments, "posts/{postId}/comments");
        group.MapGet(GetPostReactions, "posts/{postId}/reactions");

        group.MapPost(CreatePost, "posts").RequireAuthorization();
        group.MapPost(UploadPostMedia, "posts/media").RequireAuthorization();
        group.MapPut(UpdatePost, "posts/{postId}").RequireAuthorization();
        group.MapDelete(DeletePost, "posts/{postId}").RequireAuthorization();

        group.MapPost(CreateComment, "posts/{postId}/comments").RequireAuthorization();
        group.MapPut(UpdateComment, "comments/{commentId}").RequireAuthorization();
        group.MapDelete(DeleteComment, "comments/{commentId}").RequireAuthorization();

        group.MapPost(AddReaction, "posts/{postId}/reactions").RequireAuthorization();
        group.MapDelete(RemoveReaction, "posts/{postId}/reactions/{reactionType}").RequireAuthorization();
    }

    // ─── GET /api/Feed/explore ────────────────────────────────────────────────

    [EndpointSummary("Explore public posts")]
    [EndpointDescription("Returns public posts with optional language/type filter. No auth required.")]
    [ProducesResponseType(typeof(CursorPagedResult<PostSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<CursorPagedResult<PostSummaryDto>>> GetExplore(
        ISender sender,
        [FromQuery] string? languageCode = null,
        [FromQuery] string? postType = null,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 20)
    {
        CursorPagedResult<PostSummaryDto> posts = await sender.Send(
            new GetExploreQuery(languageCode, postType, beforeCursor, pageSize));
        return TypedResults.Ok(posts);
    }

    // ─── GET /api/Feed/search ──────────────────────────────────────────────────

    [EndpointSummary("Search posts")]
    [EndpointDescription("Search posts by content. No auth required.")]
    [ProducesResponseType(typeof(PaginatedResult<PostSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<PostSummaryDto>>> SearchPosts(
        ISender sender,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<PostSummaryDto> results = await sender.Send(new SearchPostsQuery(q ?? string.Empty, page, pageSize));
        return TypedResults.Ok(results);
    }

    // ─── GET /api/Feed/users/{userId} ─────────────────────────────────────────

    [EndpointSummary("Get posts by user")]
    [EndpointDescription("Returns posts by a specific user, cursor-paginated.")]
    [ProducesResponseType(typeof(CursorPagedResult<PostSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<CursorPagedResult<PostSummaryDto>>> GetUserPosts(
        [FromRoute] string userId,
        ISender sender,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 20)
    {
        CursorPagedResult<PostSummaryDto> posts = await sender.Send(
            new GetUserPostsQuery(userId, beforeCursor, pageSize));
        return TypedResults.Ok(posts);
    }

    // ─── GET /api/Feed ────────────────────────────────────────────────────────

    [EndpointSummary("Get social feed")]
    [EndpointDescription("Returns paginated posts from followed users. Use beforeCursor for pagination.")]
    [ProducesResponseType(typeof(CursorPagedResult<PostSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<CursorPagedResult<PostSummaryDto>>> GetFeed(
        ISender sender,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 20)
    {
        CursorPagedResult<PostSummaryDto> feed = await sender.Send(new GetFeedQuery(beforeCursor, pageSize));
        return TypedResults.Ok(feed);
    }

    // ─── GET /api/Feed/posts/{postId} ─────────────────────────────────────────

    [EndpointSummary("Get post details")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Results<Ok<PostDto>, NotFound>> GetPost(
        [FromRoute] int postId,
        ISender sender)
    {
        PostDto? post = await sender.Send(new GetPostQuery(postId));
        return post is null ? TypedResults.NotFound() : TypedResults.Ok(post);
    }

    // ─── GET /api/Feed/posts/{postId}/comments ────────────────────────────────

    [EndpointSummary("Get post comments")]
    [ProducesResponseType(typeof(PaginatedResult<CommentDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<CommentDto>>> GetPostComments(
        [FromRoute] int postId,
        ISender sender,
        [FromQuery] int? parentCommentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<CommentDto> comments = await sender.Send(
            new GetPostCommentsQuery(postId, parentCommentId, page, pageSize));
        return TypedResults.Ok(comments);
    }

    // ─── GET /api/Feed/posts/{postId}/reactions ───────────────────────────────

    [EndpointSummary("Get post reactions")]
    [EndpointDescription("Returns paginated list of users who reacted to a post, ordered by most recent.")]
    [ProducesResponseType(typeof(PaginatedResult<ReactionDetailDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<PaginatedResult<ReactionDetailDto>>> GetPostReactions(
        [FromRoute] int postId,
        ISender sender,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        PaginatedResult<ReactionDetailDto> reactions = await sender.Send(new GetPostReactionsQuery(postId, page, pageSize));
        return TypedResults.Ok(reactions);
    }

    // ─── POST /api/Feed/posts ─────────────────────────────────────────────────

    [EndpointSummary("Create a post")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<Created<int>> CreatePost(
        [FromBody] CreatePostCommand command,
        ISender sender)
    {
        int postId = await sender.Send(command);
        return TypedResults.Created($"/api/Feed/posts/{postId}", postId);
    }

    // ─── POST /api/Feed/posts/media ──────────────────────────────────────────

    [EndpointSummary("Upload media for a post")]
    [EndpointDescription(
        "Accepts up to 4 images or short videos (jpg, jpeg, png, webp, gif, mp4, webm). " +
        "Images ≤ 5 MB, videos ≤ 50 MB. Returns the public URLs to include in CreatePost.mediaUrls.")]
    [ProducesResponseType(typeof(IList<UploadedFile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(60_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
    public static async Task<Results<Ok<IList<UploadedFile>>, BadRequest<ProblemDetails>>> UploadPostMedia(
        IFormFileCollection files,
        IStorageService storage,
        IUser currentUser,
        CancellationToken cancellationToken)
    {
        if (files.Count is 0 or > 4)
        {
            return Reject("Provide between 1 and 4 files.");
        }

        string userId = currentUser.Id ?? throw new UnauthorizedAccessException();

        var uploaded = new List<UploadedFile>(files.Count);
        foreach (IFormFile file in files)
        {
            string? error = MediaUploadRules.ValidateMedia(file);
            if (error is not null)
            {
                return Reject(error);
            }

            await using Stream stream = file.OpenReadStream();
            UploadedFile stored = await storage.UploadAsync(
                "posts", userId, stream, file.FileName, file.ContentType, cancellationToken);
            uploaded.Add(stored);
        }

        return TypedResults.Ok<IList<UploadedFile>>(uploaded);
    }

    private static BadRequest<ProblemDetails> Reject(string detail) =>
        TypedResults.BadRequest(new ProblemDetails { Title = "Invalid upload", Detail = detail });

    // ─── PUT /api/Feed/posts/{postId} ─────────────────────────────────────────

    [EndpointSummary("Update a post")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UpdatePost(
        [FromRoute] int postId,
        [FromBody] UpdatePostBody body,
        ISender sender)
    {
        await sender.Send(new UpdatePostCommand(postId, body.Content, body.LanguageCode));
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Feed/posts/{postId} ─────────────────────────────────────

    [EndpointSummary("Delete a post")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> DeletePost(
        [FromRoute] int postId,
        ISender sender)
    {
        await sender.Send(new DeletePostCommand(postId));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Feed/posts/{postId}/comments ───────────────────────────────

    [EndpointSummary("Add a comment")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<Created<int>> CreateComment(
        [FromRoute] int postId,
        [FromBody] CreateCommentBody body,
        ISender sender)
    {
        int commentId = await sender.Send(new CreateCommentCommand(postId, body.Content, body.ParentCommentId));
        return TypedResults.Created($"/api/Feed/posts/{postId}/comments", commentId);
    }

    // ─── PUT /api/Feed/comments/{commentId} ──────────────────────────────────

    [EndpointSummary("Update a comment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> UpdateComment(
        [FromRoute] int commentId,
        [FromBody] UpdateCommentBody body,
        ISender sender)
    {
        await sender.Send(new UpdateCommentCommand(commentId, body.Content));
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Feed/comments/{commentId} ────────────────────────────────

    [EndpointSummary("Delete a comment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<NoContent> DeleteComment(
        [FromRoute] int commentId,
        ISender sender)
    {
        await sender.Send(new DeleteCommentCommand(commentId));
        return TypedResults.NoContent();
    }

    // ─── POST /api/Feed/posts/{postId}/reactions ──────────────────────────────

    [EndpointSummary("Add/toggle a reaction")]
    [EndpointDescription("Adds a reaction to a post. Toggling the same type is idempotent; changing type removes the old one.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<NoContent> AddReaction(
        [FromRoute] int postId,
        [FromBody] AddReactionBody body,
        ISender sender)
    {
        await sender.Send(new AddReactionCommand(postId, body.ReactionType));
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Feed/posts/{postId}/reactions/{reactionType} ─────────────

    [EndpointSummary("Remove a reaction")]
    [EndpointDescription("Removes the authenticated user's reaction of the given type from the post. Idempotent.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> RemoveReaction(
        [FromRoute] int postId,
        [FromRoute] string reactionType,
        ISender sender)
    {
        await sender.Send(new RemoveReactionCommand(postId, reactionType));
        return TypedResults.NoContent();
    }

    // ─── Request body records ─────────────────────────────────────────────────

    public record UpdatePostBody(string Content, string? LanguageCode);
    public record CreateCommentBody(string Content, int? ParentCommentId);
    public record UpdateCommentBody(string Content);
    public record AddReactionBody(string ReactionType);
}
