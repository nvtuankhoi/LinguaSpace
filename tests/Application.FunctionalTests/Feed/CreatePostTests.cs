using LinguaSpace.Application.Common.Exceptions;
using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Feed.Commands.AddReaction;
using LinguaSpace.Application.Feed.Commands.CreateComment;
using LinguaSpace.Application.Feed.Commands.CreatePost;
using LinguaSpace.Application.Feed.Commands.DeletePost;
using LinguaSpace.Application.Feed.DTOs;
using LinguaSpace.Application.Feed.Queries.GetFeed;
using LinguaSpace.Application.Feed.Queries.GetPost;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Application.FunctionalTests.Feed;

/// <summary>
/// Functional tests for CreatePostCommand, GetFeedQuery, DeletePostCommand.
/// </summary>
public class CreatePostTests : TestBase
{
    [Test]
    public async Task ShouldCreatePostAndReturnId()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "My first post!",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: null,
            Tags: null,
            MediaUrls: null));

        postId.ShouldBeGreaterThan(0);

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.Content.ShouldBe("My first post!");
        post.PostType.ShouldBe(PostType.Text);
        post.IsDeleted.ShouldBeFalse();
    }

    [Test]
    public async Task ShouldCreatePostWithTags()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "Tagged post",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: null,
            Tags: ["language", "english"],
            MediaUrls: null));

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.Tags.Count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldThrowValidationExceptionForEmptyContent()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        ValidationException ex = await Should.ThrowAsync<ValidationException>(
            () => TestApp.SendAsync(new CreatePostCommand(
                Content: string.Empty,
                PostType: "Text",
                LanguageCode: null,
                Metadata: null,
                Tags: null,
                MediaUrls: null)));

        ex.Errors.ShouldContainKey("Content");
    }

    [Test]
    public async Task ShouldThrowForbiddenWhenNotAuthenticated()
    {
        await Should.ThrowAsync<ForbiddenAccessException>(
            () => TestApp.SendAsync(new CreatePostCommand(
                "Hello", "Text", null, null, null, null)));
    }

    [Test]
    public async Task GetFeedShouldReturnOwnPosts()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        await TestApp.SendAsync(new CreatePostCommand(
            "Feed test post", "Text", "en", null, null, null));

        CursorPagedResult<PostSummaryDto> feed = await TestApp.SendAsync(
            new GetFeedQuery(BeforeCursor: null, PageSize: 20));

        feed.Items.Count.ShouldBe(1);
        feed.Items[0].Content.ShouldBe("Feed test post");
    }

    [Test]
    public async Task GetPostShouldReturnPostById()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            "Get by id test", "Text", "en", null, null, null));

        PostDto? dto = await TestApp.SendAsync(new GetPostQuery(postId));

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(postId);
        dto.Content.ShouldBe("Get by id test");
    }

    [Test]
    public async Task DeletePostShouldSoftDeletePost()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            "To be deleted", "Text", null, null, null, null));

        await TestApp.SendAsync(new DeletePostCommand(postId));

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.IsDeleted.ShouldBeTrue();
    }

    [Test]
    public async Task AddReactionShouldIncrementLikeCount()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            "React to me", "Text", null, null, null, null));

        await TestApp.SendAsync(new AddReactionCommand(postId, "Like"));

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.LikeCount.ShouldBe(1);
    }

    [Test]
    public async Task CreateCommentShouldIncrementCommentCount()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            "Comment on me", "Text", null, null, null, null));

        await TestApp.SendAsync(new CreateCommentCommand(postId, "Great post!", null));

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.CommentCount.ShouldBe(1);
    }
}
