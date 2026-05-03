using LinguaSpace.Domain.Entities;
using LinguaSpace.Domain.Enums;

namespace LinguaSpace.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for Post, Comment, and Reaction entities (Phase 2 Feed domain).
/// </summary>
public class FeedEntityTests
{
    // ─── Post ─────────────────────────────────────────────────────────────────

    [Test]
    public void NewPost_HasExpectedDefaults()
    {
        Post post = new();

        post.Content.ShouldBe(string.Empty);
        post.PostType.ShouldBe(PostType.Text);
        post.LikeCount.ShouldBe(0);
        post.CommentCount.ShouldBe(0);
        post.IsDeleted.ShouldBeFalse();
        post.Metadata.ShouldBeNull();
        post.LanguageCode.ShouldBeNull();
        post.MediaItems.ShouldBeEmpty();
        post.Tags.ShouldBeEmpty();
        post.Comments.ShouldBeEmpty();
    }

    [Test]
    public void Post_IncrementLikeCount_Works()
    {
        Post post = new() { LikeCount = 5 };

        post.LikeCount++;

        post.LikeCount.ShouldBe(6);
    }

    [Test]
    public void Post_IncrementCommentCount_Works()
    {
        Post post = new() { CommentCount = 2 };

        post.CommentCount++;

        post.CommentCount.ShouldBe(3);
    }

    [Test]
    public void Post_SoftDelete_SetsIsDeletedTrue()
    {
        Post post = new() { Content = "Hello" };

        post.IsDeleted = true;

        post.IsDeleted.ShouldBeTrue();
        post.Content.ShouldBe("Hello"); // content preserved
    }

    [Test]
    public void Post_VocabCard_StoresMetadata()
    {
        const string json = """{"word":"ephemeral","definition":"lasting a short time"}""";

        Post post = new()
        {
            PostType = PostType.VocabCard,
            Metadata = json,
        };

        post.PostType.ShouldBe(PostType.VocabCard);
        post.Metadata.ShouldBe(json);
    }

    // ─── Comment ──────────────────────────────────────────────────────────────

    [Test]
    public void NewComment_HasExpectedDefaults()
    {
        Comment comment = new();

        comment.Content.ShouldBe(string.Empty);
        comment.LikeCount.ShouldBe(0);
        comment.IsDeleted.ShouldBeFalse();
        comment.ParentCommentId.ShouldBeNull();
    }

    [Test]
    public void Comment_WithParentId_IsReply()
    {
        Comment reply = new()
        {
            PostId = 1,
            AuthorId = "user-1",
            Content = "I agree!",
            ParentCommentId = 42,
        };

        reply.ParentCommentId.ShouldBe(42);
    }

    [Test]
    public void Comment_SoftDelete_SetsIsDeletedTrue()
    {
        Comment comment = new() { Content = "Text" };

        comment.IsDeleted = true;

        comment.IsDeleted.ShouldBeTrue();
    }

    // ─── Reaction ─────────────────────────────────────────────────────────────

    [Test]
    public void NewReaction_StoresTargetAndType()
    {
        Reaction reaction = new()
        {
            TargetId = 10,
            TargetType = ReactionTargetType.Post,
            UserId = "user-abc",
            Type = ReactionType.Like,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        reaction.TargetId.ShouldBe(10);
        reaction.TargetType.ShouldBe(ReactionTargetType.Post);
        reaction.UserId.ShouldBe("user-abc");
        reaction.Type.ShouldBe(ReactionType.Like);
    }

    // ─── PostMediaItem ────────────────────────────────────────────────────────

    [Test]
    public void PostMediaItem_StoresUrlAndOrder()
    {
        PostMediaItem media = new()
        {
            PostId = 5,
            Url = "https://cdn.example.com/img.jpg",
            SortOrder = 0,
        };

        media.Url.ShouldBe("https://cdn.example.com/img.jpg");
        media.SortOrder.ShouldBe(0);
    }
}
