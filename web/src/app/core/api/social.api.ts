import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  ConversationDto,
  ConversationsUnreadCountDto,
  CursorPagedResult,
  DirectMessageDto,
  PaginatedResult,
  SendDmRequest,
  UpdateMessageRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class SocialApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getConversations(opts: { page?: number; pageSize?: number } = {}) {
    let params = new HttpParams().set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 50));
    return this.http.get<PaginatedResult<ConversationDto>>(`${this.base}/Social/conversations`, {
      params,
      withCredentials: true,
    });
  }

  getUnreadCount() {
    return this.http.get<ConversationsUnreadCountDto>(`${this.base}/Social/conversations/unread-count`, {
      withCredentials: true,
    });
  }

  getMessages(conversationId: number, opts: { beforeCursor?: string; pageSize?: number } = {}) {
    let params = new HttpParams().set('pageSize', String(opts.pageSize ?? 50));
    if (opts.beforeCursor) params = params.set('beforeCursor', opts.beforeCursor);
    return this.http.get<CursorPagedResult<DirectMessageDto>>(
      `${this.base}/Social/conversations/${conversationId}/messages`,
      { params, withCredentials: true },
    );
  }

  sendDm(req: SendDmRequest) {
    return this.http.post<DirectMessageDto>(`${this.base}/Social/dm`, req, { withCredentials: true });
  }

  markRead(conversationId: number) {
    return this.http.post(`${this.base}/Social/conversations/${conversationId}/read`, {}, { withCredentials: true });
  }

  editDm(messageId: number, req: UpdateMessageRequest) {
    return this.http.put(`${this.base}/Social/messages/${messageId}`, req, { withCredentials: true });
  }

  deleteDm(messageId: number) {
    return this.http.delete(`${this.base}/Social/messages/${messageId}`, { withCredentials: true });
  }
}
