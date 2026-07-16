import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, ids, session } from '../db';
import type { MockComment, MockPost } from '../db';
import type {
  CommentDto,
  CreateCommentRequest,
  CreatePostRequest,
  CursorPagedResult,
  PostDto,
  PostSummaryDto,
  PostType,
  ReactionType,
} from '../../models';

const BASE = environment.apiBaseUrl;
const PAGE = 5;

const commentDto = (c: MockComment): CommentDto => ({
  id: c.id,
  postId: c.postId,
  authorId: c.authorId,
  content: c.content,
  parentCommentId: c.parentCommentId,
  likeCount: c.reactions.length,
  createdAt: c.createdAt,
});

const summary = (p: MockPost): PostSummaryDto => ({
  id: p.id,
  authorId: p.authorId,
  content: p.content,
  postType: p.postType,
  languageCode: p.languageCode,
  metadata: p.metadata,
  likeCount: p.reactions.length,
  commentCount: p.comments.length,
  createdAt: p.createdAt,
  tags: p.tags,
  mediaItems: p.mediaItems,
});

const detail = (p: MockPost): PostDto => ({
  ...summary(p),
  mediaItems: p.mediaItems,
  comments: p.comments.map(commentDto),
});

function page(sorted: MockPost[], beforeCursor: string | null, pageSize = PAGE): CursorPagedResult<PostSummaryDto> {
  const filtered = beforeCursor ? sorted.filter((p) => p.createdAt < beforeCursor) : sorted;
  const items = filtered.slice(0, pageSize).map(summary);
  const hasMore = filtered.length > pageSize;
  const nextCursor = hasMore && items.length > 0 ? items[items.length - 1].createdAt : null;
  return { items, hasMore, nextCursor };
}

const newestFirst = (): MockPost[] => [...db.posts].sort((a, b) => b.createdAt.localeCompare(a.createdAt));

export const feedHandlers = [
  http.get(`${BASE}/Feed`, ({ request }) => {
    const url = new URL(request.url);
    return HttpResponse.json(
      page(newestFirst(), url.searchParams.get('beforeCursor'), Number(url.searchParams.get('pageSize') ?? PAGE)),
    );
  }),

  http.get(`${BASE}/Feed/explore`, ({ request }) => {
    const url = new URL(request.url);
    const lang = url.searchParams.get('languageCode');
    const type = url.searchParams.get('postType');
    let sorted = newestFirst();
    if (lang) sorted = sorted.filter((p) => p.languageCode === lang);
    if (type) sorted = sorted.filter((p) => p.postType === type);
    return HttpResponse.json(
      page(sorted, url.searchParams.get('beforeCursor'), Number(url.searchParams.get('pageSize') ?? PAGE)),
    );
  }),

  http.get(`${BASE}/Feed/users/:userId`, ({ params, request }) => {
    const url = new URL(request.url);
    const userId = params['userId'] as string;
    const sorted = newestFirst().filter((p) => p.authorId === userId);
    return HttpResponse.json(
      page(sorted, url.searchParams.get('beforeCursor'), Number(url.searchParams.get('pageSize') ?? PAGE)),
    );
  }),

  http.get(`${BASE}/Feed/posts/:postId`, ({ params }) => {
    const post = db.posts.find((p) => p.id === Number(params['postId']));
    return post ? HttpResponse.json(detail(post)) : HttpResponse.json({ detail: 'Post not found.' }, { status: 404 });
  }),

  http.get(`${BASE}/Feed/posts/:postId/comments`, ({ params, request }) => {
    const post = db.posts.find((p) => p.id === Number(params['postId']));
    if (!post) return HttpResponse.json({ detail: 'Post not found.' }, { status: 404 });
    const url = new URL(request.url);
    const parent = url.searchParams.get('parentCommentId');
    const comments = parent
      ? post.comments.filter((c) => String(c.parentCommentId) === parent)
      : post.comments.filter((c) => c.parentCommentId == null);
    const items = comments.map(commentDto);
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.post(`${BASE}/Feed/posts`, async ({ request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as CreatePostRequest;
    const post: MockPost = {
      id: ids.post(),
      authorId: session.userId,
      content: body.content,
      postType: (body.postType ?? 'Text') as PostType,
      languageCode: body.languageCode ?? null,
      metadata: body.metadata ?? null,
      tags: body.tags ?? [],
      createdAt: new Date().toISOString(),
      comments: [],
      reactions: [],
      mediaItems: (body.mediaUrls ?? []).map((url, i) => ({ id: i + 1, url, sortOrder: i })),
    };
    db.posts.unshift(post);
    return HttpResponse.json({ postId: post.id }, { status: 201 });
  }),

  http.post(`${BASE}/Feed/posts/:postId/comments`, async ({ params, request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const post = db.posts.find((p) => p.id === Number(params['postId']));
    if (!post) return HttpResponse.json({ detail: 'Post not found.' }, { status: 404 });
    const body = (await request.json()) as CreateCommentRequest;
    const comment: MockComment = {
      id: ids.comment(),
      postId: post.id,
      authorId: session.userId,
      content: body.content,
      parentCommentId: body.parentCommentId ?? null,
      createdAt: new Date().toISOString(),
      reactions: [],
    };
    post.comments.push(comment);
    return HttpResponse.json({ commentId: comment.id }, { status: 201 });
  }),

  http.post(`${BASE}/Feed/posts/:postId/reactions`, async ({ params, request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const post = db.posts.find((p) => p.id === Number(params['postId']));
    if (!post) return HttpResponse.json({ detail: 'Post not found.' }, { status: 404 });
    const body = (await request.json()) as { reactionType: ReactionType };
    post.reactions = post.reactions.filter((r) => r.userId !== session.userId);
    post.reactions.push({ userId: session.userId, type: body.reactionType });
    return new HttpResponse(null, { status: 204 });
  }),

  http.delete(`${BASE}/Feed/posts/:postId/reactions/:reactionType`, ({ params }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const post = db.posts.find((p) => p.id === Number(params['postId']));
    if (!post) return HttpResponse.json({ detail: 'Post not found.' }, { status: 404 });
    post.reactions = post.reactions.filter(
      (r) => !(r.userId === session.userId && r.type === (params['reactionType'] as ReactionType)),
    );
    return new HttpResponse(null, { status: 204 });
  }),
];
