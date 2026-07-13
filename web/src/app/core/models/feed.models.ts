import type { PostType, ReactionType } from './enums';

export interface MediaItemDto {
  id: number;
  url: string;
  sortOrder: number;
}

export interface PostMetadataDto {
  audioUrl: string | null;
  durationSeconds: number | null;
  thumbnailUrl: string | null;
  linkUrl: string | null;
  linkTitle: string | null;
  linkDescription: string | null;
  backText: string | null;
  pronunciation: string | null;
  example: string | null;
}

export interface CommentDto {
  id: number;
  postId: number;
  authorId: string;
  content: string;
  parentCommentId: number | null; // one reply level only
  likeCount: number;
  createdAt: string;
}

export interface PostDto {
  id: number;
  authorId: string;
  content: string;
  postType: PostType;
  languageCode: string | null;
  metadata: PostMetadataDto | null;
  likeCount: number;
  commentCount: number;
  createdAt: string;
  tags: string[];
  mediaItems: MediaItemDto[];
  comments: CommentDto[];
}

export interface PostSummaryDto {
  id: number;
  authorId: string;
  content: string;
  postType: PostType;
  languageCode: string | null;
  metadata: PostMetadataDto | null;
  likeCount: number;
  commentCount: number;
  createdAt: string;
  tags: string[];
}

export interface ReactionDetailDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  reactionType: ReactionType;
  createdAt: string;
}

export interface CreatePostRequest {
  content: string;
  postType: PostType;
  languageCode?: string | null;
  metadata?: PostMetadataDto | null;
  tags?: string[];
  mediaUrls?: string[]; // max 4
}

export interface UpdatePostRequest {
  content: string;
  languageCode?: string | null;
}

export interface CreateCommentRequest {
  content: string;
  parentCommentId?: number | null;
}

export interface UpdateCommentRequest {
  content: string;
}

export interface ReactRequest {
  reactionType: ReactionType;
}

export interface FeedExploreQuery {
  languageCode?: string;
  postType?: PostType;
  beforeCursor?: string | null;
  pageSize?: number;
}
