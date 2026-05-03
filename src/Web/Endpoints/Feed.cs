using LinguaSpace.Application.Feed.Commands.AddReaction;
using LinguaSpace.Application.Feed.Commands.CreateComment;
using LinguaSpace.Application.Feed.Commands.CreatePost;
using LinguaSpace.Application.Feed.Commands.DeleteComment;
using LinguaSpace.Application.Feed.Commands.DeletePost;
using LinguaSpace.Application.Feed.Commands.RemoveReaction;
using LinguaSpace.Application.Feed.Commands.UpdatePost;
using LinguaSpace.Application.Feed.DTOs;
using LinguaSpace.Application.Feed.Queries.GetFeed;
using LinguaSpace.Application.Feed.Queries.GetPost;
using LinguaSpace.Application.Feed.Queries.GetPostComments;
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
        group.MapGet(GetPost, "posts/{postId}").RequireAuthorization();
        group.MapGet(GetPostComments, "posts/{postId}/comments").RequireAuthorization();

        group.MapPost(CreatePost, "posts").RequireAuthorization();
        group.MapPut(UpdatePost, "posts/{postId}").RequireAuthorization();
        group.MapDelete(DeletePost, "posts/{postId}").RequireAuthorization();

        group.MapPost(CreateComment, "posts/{postId}/comments").RequireAuthorization();
        group.MapDelete(DeleteComment, "comments/{commentId}").RequireAuthorization();

        group.MapPost(AddReaction, "reactions").RequireAuthorization();
        group.MapDelete(RemoveReaction, "reactions/{targetType}/{targetId}").RequireAuthorization();
    }

    // ─── GET /api/Feed ────────────────────────────────────────────────────────

    [EndpointSummary("Get social feed")]
    [EndpointDescription("Returns paginated posts from followed users. Use beforeCursor for pagination.")]
    [ProducesResponseType(typeof(IList<PostSummaryDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<PostSummaryDto>>> GetFeed(
        ISender sender,
        [FromQuery] DateTimeOffset? beforeCursor = null,
        [FromQuery] int pageSize = 20)
    {
        IList<PostSummaryDto> feed = await sender.Send(new GetFeedQuery(beforeCursor, pageSize));
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
    [ProducesResponseType(typeof(IList<CommentDto>), StatusCodes.Status200OK)]
    public static async Task<Ok<IList<CommentDto>>> GetPostComments(
        [FromRoute] int postId,
        ISender sender,
        [FromQuery] int? parentCommentId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IList<CommentDto> comments = await sender.Send(
            new GetPostCommentsQuery(postId, parentCommentId, page, pageSize));
        return TypedResults.Ok(comments);
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

    // ─── POST /api/Feed/reactions ─────────────────────────────────────────────

    [EndpointSummary("Add/toggle a reaction")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public static async Task<NoContent> AddReaction(
        [FromBody] AddReactionCommand command,
        ISender sender)
    {
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    // ─── DELETE /api/Feed/reactions/{targetType}/{targetId} ──────────────────

    [EndpointSummary("Remove a reaction")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public static async Task<NoContent> RemoveReaction(
        [FromRoute] string targetType,
        [FromRoute] int targetId,
        ISender sender)
    {
        await sender.Send(new RemoveReactionCommand(targetId, targetType));
        return TypedResults.NoContent();
    }

    // ─── Request body records ─────────────────────────────────────────────────

    public record UpdatePostBody(string Content, string? LanguageCode);
    public record CreateCommentBody(string Content, int? ParentCommentId);
}
