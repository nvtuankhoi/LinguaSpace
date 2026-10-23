import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, session } from '../db';
import type { MockNotification } from '../db';
import type { NotificationDto, MarkNotificationsRequest } from '../../models';

const BASE = environment.apiBaseUrl;

const meId = (): string | null => session.userId;

const toDto = (n: MockNotification): NotificationDto => ({
  id: n.id,
  type: n.type,
  payload: n.payload,
  isRead: n.isRead,
  createdAt: n.createdAt,
});

export const notificationsHandlers = [
  // GET /api/Notifications?unreadOnly=&page=&pageSize=
  http.get(`${BASE}/Notifications`, ({ request }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });

    const url = new URL(request.url);
    const unreadOnly = url.searchParams.get('unreadOnly') === 'true';
    const page = Math.max(1, Number(url.searchParams.get('page') ?? 1));
    const pageSize = Math.min(50, Math.max(1, Number(url.searchParams.get('pageSize') ?? 20)));

    let items = [...db.notifications].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    if (unreadOnly) {
      items = items.filter((n) => !n.isRead);
    }
    const totalCount = items.length;
    const start = (page - 1) * pageSize;
    const paged = items.slice(start, start + pageSize);

    return HttpResponse.json({
      items: paged.map(toDto),
      totalCount,
      page,
      pageSize,
      hasMore: start + pageSize < totalCount,
    });
  }),

  // GET /api/Notifications/unread-count
  http.get(`${BASE}/Notifications/unread-count`, () => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const count = db.notifications.filter((n) => !n.isRead).length;
    return HttpResponse.json(count);
  }),

  // POST /api/Notifications/read
  http.post(`${BASE}/Notifications/read`, async ({ request }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as MarkNotificationsRequest;
    const ids = body.notificationIds;
    if (ids && ids.length) {
      db.notifications.forEach((n) => {
        if (ids.includes(n.id)) n.isRead = true;
      });
    } else {
      db.notifications.forEach((n) => (n.isRead = true));
    }
    return new HttpResponse(null, { status: 204 });
  }),

  // POST /api/Notifications/delete-batch
  http.post(`${BASE}/Notifications/delete-batch`, async ({ request }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as MarkNotificationsRequest;
    const ids = body.notificationIds;
    if (ids && ids.length) {
      db.notifications = db.notifications.filter((n) => !ids.includes(n.id)) as typeof db.notifications;
    } else {
      db.notifications = [] as unknown as typeof db.notifications;
    }
    return new HttpResponse(null, { status: 204 });
  }),
];
