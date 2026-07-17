import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CommentDto,
  CreateCommentRequest,
  CreatePostRequest,
  CursorPagedResult,
  PaginatedResult,
  PostDto,
  PostSummaryDto,
  ReactionDetailDto,
  ReactionType,
  UpdateCommentRequest,
  UpdatePostRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class FeedApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getFeed(beforeCursor: string | null = null, pageSize = 8) {
    let params = new HttpParams().set('pageSize', pageSize);
    if (beforeCursor) params = params.set('beforeCursor', beforeCursor);
    return this.http.get<CursorPagedResult<PostSummaryDto>>(`${this.base}/Feed`, { params, withCredentials: true });
  }

  getExplore(opts: { languageCode?: string; postType?: string; beforeCursor?: string | null; pageSize?: number } = {}) {
    let params = new HttpParams().set('pageSize', opts.pageSize ?? 8);
    if (opts.languageCode) params = params.set('languageCode', opts.languageCode);
    if (opts.postType) params = params.set('postType', opts.postType);
    if (opts.beforeCursor) params = params.set('beforeCursor', opts.beforeCursor);
    return this.http.get<CursorPagedResult<PostSummaryDto>>(`${this.base}/Feed/explore`, { params, withCredentials: true });
  }

  getPost(id: number) {
    return this.http.get<PostDto>(`${this.base}/Feed/posts/${id}`, { withCredentials: true });
  }

  getUserPosts(userId: string, opts: { beforeCursor?: string | null; pageSize?: number } = {}) {
    let params = new HttpParams().set('pageSize', opts.pageSize ?? 8);
    if (opts.beforeCursor) params = params.set('beforeCursor', opts.beforeCursor);
    return this.http.get<CursorPagedResult<PostSummaryDto>>(`${this.base}/Feed/users/${userId}`, { params, withCredentials: true });
  }

  searchPosts(query: string, opts: { page?: number; pageSize?: number } = {}) {
    const params = new HttpParams()
      .set('q', query)
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 20));
    return this.http.get<PaginatedResult<PostSummaryDto>>(`${this.base}/Feed/search`, { params, withCredentials: true });
  }

  getComments(postId: number) {
    return this.http.get<PaginatedResult<CommentDto>>(`${this.base}/Feed/posts/${postId}/comments`, {
      withCredentials: true,
    });
  }

  createPost(req: CreatePostRequest) {
    // Backend CreatePost returns Created<int> → a bare postId in the body, not { postId }.
    return this.http.post<number>(`${this.base}/Feed/posts`, req, { withCredentials: true });
  }

  addComment(postId: number, req: CreateCommentRequest) {
    // Backend CreateComment returns Created<int> → a bare commentId in the body, not { commentId }.
    return this.http.post<number>(`${this.base}/Feed/posts/${postId}/comments`, req, {
      withCredentials: true,
    });
  }

  react(postId: number, reactionType: ReactionType) {
    return this.http.post(`${this.base}/Feed/posts/${postId}/reactions`, { reactionType }, { withCredentials: true });
  }

  removeReaction(postId: number, reactionType: ReactionType) {
    return this.http.delete(`${this.base}/Feed/posts/${postId}/reactions/${reactionType}`, {
      withCredentials: true,
    });
  }

  /** Who reacted to a post. GET /Feed/posts/{postId}/reactions → PaginatedResult<ReactionDetailDto>. */
  getReactions(postId: number, opts: { page?: number; pageSize?: number } = {}) {
    const params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 50));
    return this.http.get<PaginatedResult<ReactionDetailDto>>(`${this.base}/Feed/posts/${postId}/reactions`, {
      params,
      withCredentials: true,
    });
  }

  deletePost(postId: number) {
    return this.http.delete(`${this.base}/Feed/posts/${postId}`, { withCredentials: true });
  }

  deleteComment(commentId: number) {
    return this.http.delete(`${this.base}/Feed/comments/${commentId}`, { withCredentials: true });
  }

  updatePost(postId: number, req: UpdatePostRequest) {
    return this.http.put(`${this.base}/Feed/posts/${postId}`, req, { withCredentials: true });
  }

  updateComment(commentId: number, req: UpdateCommentRequest) {
    return this.http.put(`${this.base}/Feed/comments/${commentId}`, req, { withCredentials: true });
  }
}
