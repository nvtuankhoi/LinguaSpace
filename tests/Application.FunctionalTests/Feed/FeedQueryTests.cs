using LinguaSpace.Application.Common.Models;
using LinguaSpace.Application.Feed.Commands.AddReaction;
using LinguaSpace.Application.Feed.Commands.CreatePost;
using LinguaSpace.Application.Feed.Commands.DeletePost;
using LinguaSpace.Application.Feed.DTOs;
using LinguaSpace.Application.Feed.Queries.GetPost;
using LinguaSpace.Application.Feed.Queries.GetPostReactions;
using LinguaSpace.Application.Feed.Queries.SearchPosts;
using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Application.FunctionalTests.Feed;

public class FeedQueryTests : TestBase
{
    [Test]
    public async Task CreatePostShouldRoundTripTypedMetadata()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        PostMetadataDto metadata = new(
            AudioUrl: "https://cdn.example.com/audio.mp3",
            DurationSeconds: 42,
            ThumbnailUrl: "https://cdn.example.com/thumb.jpg",
            LinkUrl: "https://example.com/article",
            LinkTitle: "Article",
            LinkDescription: "Description",
            BackText: null,
            Pronunciation: null,
            Example: null);

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "Metadata post",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: metadata,
            Tags: null,
            MediaUrls: null));

        Post? post = await TestApp.FindAsync<Post>(postId);
        post.ShouldNotBeNull();
        post.Metadata.ShouldNotBeNull();
        post.Metadata.ShouldContain("audioUrl");
        post.Metadata.ShouldContain("https://cdn.example.com/audio.mp3");

        PostDto? dto = await TestApp.SendAsync(new GetPostQuery(postId));

        dto.ShouldNotBeNull();
        dto.Metadata.ShouldNotBeNull();
        dto.Metadata.AudioUrl.ShouldBe(metadata.AudioUrl);
        dto.Metadata.DurationSeconds.ShouldBe(metadata.DurationSeconds);
        dto.Metadata.ThumbnailUrl.ShouldBe(metadata.ThumbnailUrl);
        dto.Metadata.LinkUrl.ShouldBe(metadata.LinkUrl);
        dto.Metadata.LinkTitle.ShouldBe(metadata.LinkTitle);
        dto.Metadata.LinkDescription.ShouldBe(metadata.LinkDescription);
    }

    [Test]
    public async Task SearchPostsShouldReturnCaseInsensitiveMatchesAndExcludeDeletedPosts()
    {
        await TestApp.RegisterAndSetCurrentUserAsync();

        int activePostId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "Learning ENGLISH with friends",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: null,
            Tags: null,
            MediaUrls: null));

        int deletedPostId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "Archived english practice",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: null,
            Tags: null,
            MediaUrls: null));

        await TestApp.SendAsync(new DeletePostCommand(deletedPostId));

        await TestApp.SendAsync(new CreatePostCommand(
            Content: "Bonjour tout le monde",
            PostType: "Text",
            LanguageCode: "fr",
            Metadata: null,
            Tags: null,
            MediaUrls: null));

        PaginatedResult<PostSummaryDto> results = await TestApp.SendAsync(new SearchPostsQuery("english", 1, 20));

        results.TotalCount.ShouldBe(1);
        results.Items.Count.ShouldBe(1);
        results.Items[0].Id.ShouldBe(activePostId);
        results.Items[0].Content.ShouldBe("Learning ENGLISH with friends");
    }

    [Test]
    public async Task GetPostReactionsShouldReturnReactionUsersWithProfileData()
    {
        await TestApp.RegisterAndSetCurrentUserAsync("author@local");

        int postId = await TestApp.SendAsync(new CreatePostCommand(
            Content: "React to this",
            PostType: "Text",
            LanguageCode: "en",
            Metadata: null,
            Tags: null,
            MediaUrls: null));

        string reactorId = await TestApp.RegisterAndSetCurrentUserAsync("reactor@local");

        await TestApp.SendAsync(new AddReactionCommand(postId, "Like"));

        PaginatedResult<ReactionDetailDto> reactions = await TestApp.SendAsync(new GetPostReactionsQuery(postId));

        reactions.Items.Count.ShouldBe(1);
        reactions.Items[0].UserId.ShouldBe(reactorId);
        reactions.Items[0].DisplayName.ShouldBe("reactor");
        reactions.Items[0].AvatarUrl.ShouldBeNull();
        reactions.Items[0].ReactionType.ShouldBe("Like");
    }
}
