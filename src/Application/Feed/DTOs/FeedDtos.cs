namespace LinguaSpace.Application.Feed.DTOs;

public record PostSummaryDto(
    int Id,
    string AuthorId,
    string Content,
    string PostType,
    string? LanguageCode,
    PostMetadataDto? Metadata,
    int LikeCount,
    int CommentCount,
    DateTimeOffset CreatedAt,
    IList<string> Tags);

public record PostDto(
    int Id,
    string AuthorId,
    string Content,
    string PostType,
    string? LanguageCode,
    PostMetadataDto? Metadata,
    int LikeCount,
    int CommentCount,
    DateTimeOffset CreatedAt,
    IList<string> Tags,
    IList<MediaItemDto> MediaItems,
    IList<CommentDto> Comments);

public record CommentDto(
    int Id,
    int PostId,
    string AuthorId,
    string Content,
    int? ParentCommentId,
    int LikeCount,
    DateTimeOffset CreatedAt);

public record MediaItemDto(
    int Id,
    string Url,
    int SortOrder);

public record ReactionDetailDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string ReactionType,
    DateTimeOffset CreatedAt);

public record ReactionSummaryDto(
    int TargetId,
    string TargetType,
    string ReactionType,
    int Count);
