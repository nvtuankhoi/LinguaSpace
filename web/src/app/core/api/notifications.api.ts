import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  MarkNotificationsRequest,
  NotificationDto,
  PaginatedResult,
} from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationsApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getNotifications(opts: { unreadOnly?: boolean; page?: number; pageSize?: number } = {}) {
    let params = new HttpParams()
      .set('page', String(opts.page ?? 1))
      .set('pageSize', String(opts.pageSize ?? 50));
    if (opts.unreadOnly) params = params.set('unreadOnly', 'true');
    return this.http.get<PaginatedResult<NotificationDto>>(`${this.base}/Notifications`, {
      params,
      withCredentials: true,
    });
  }

  getUnreadCount() {
    return this.http.get<number>(`${this.base}/Notifications/unread-count`, {
      withCredentials: true,
    });
  }

  markRead(req: MarkNotificationsRequest) {
    return this.http.post(`${this.base}/Notifications/read`, req, { withCredentials: true });
  }

  deleteBatch(req: MarkNotificationsRequest) {
    return this.http.post(`${this.base}/Notifications/delete-batch`, req, { withCredentials: true });
  }
}
