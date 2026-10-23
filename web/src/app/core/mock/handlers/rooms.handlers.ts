import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, ids, session } from '../db';
import type { MockParticipant, MockRoom } from '../db';
import type {
  CreateRoomRequest,
  MessageDto,
  RoomDto,
  RoomParticipantDto,
  RoomSummaryDto,
  RoomType,
  SendMessageRequest,
} from '../../models';

const BASE = environment.apiBaseUrl;

const toParticipant = (p: MockParticipant): RoomParticipantDto => ({
  userId: p.userId,
  displayName: p.displayName,
  avatarUrl: p.avatarUrl,
  role: p.role,
  joinedAt: p.joinedAt,
  isMuted: false,
});

const summary = (r: MockRoom): RoomSummaryDto => ({
  id: r.id,
  title: r.title,
  languageCode: r.languageCode,
  maxParticipants: r.maxParticipants,
  participantCount: r.participants.length,
  roomType: r.roomType,
  hostId: r.hostId,
});

const detail = (r: MockRoom): RoomDto => ({
  ...summary(r),
  description: r.description,
  status: r.status,
  created: r.created,
  participants: r.participants.map(toParticipant),
});

const messageDto = (m: MockRoom['messages'][number]): MessageDto => ({
  id: m.id,
  senderId: m.senderId,
  senderDisplayName: m.senderDisplayName,
  content: m.content,
  type: m.type,
  sentAt: m.sentAt,
  isDeleted: m.isDeleted,
});

export const roomsHandlers = [
  http.get(`${BASE}/Rooms`, ({ request }) => {
    const url = new URL(request.url);
    const lang = url.searchParams.get('languageCode');
    let rooms = db.rooms.filter((r) => r.status === 'Active');
    if (lang) rooms = rooms.filter((r) => r.languageCode === lang);
    const items = rooms.map(summary);
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 50, hasMore: false });
  }),

  // Must precede /Rooms/:id so 'mine' isn't captured as an id.
  http.get(`${BASE}/Rooms/mine`, () => {
    const uid = session.userId;
    const rooms = uid ? db.rooms.filter((r) => r.participants.some((p) => p.userId === uid)) : [];
    const items = rooms.map(summary);
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.get(`${BASE}/Rooms/:id`, ({ params }) => {
    const room = db.rooms.find((r) => r.id === Number(params['id']));
    return room ? HttpResponse.json(detail(room)) : HttpResponse.json({ detail: 'Room not found.' }, { status: 404 });
  }),

  http.post(`${BASE}/Rooms`, async ({ request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as CreateRoomRequest;
    const host = db.users.find((u) => u.userId === session.userId)!;
    const room: MockRoom = {
      id: ids.room(),
      title: body.title,
      description: body.description ?? null,
      languageCode: body.languageCode,
      maxParticipants: body.maxParticipants,
      status: 'Active',
      roomType: (body.roomType ?? 'TextOnly') as RoomType,
      hostId: host.userId,
      created: new Date().toISOString(),
      participants: [
        { userId: host.userId, displayName: host.displayName, avatarUrl: host.avatarUrl, role: 'Host', joinedAt: new Date().toISOString() },
      ],
      messages: [],
    };
    db.rooms.unshift(room);
    return HttpResponse.json(room.id, { status: 201 });
  }),

  http.post(`${BASE}/Rooms/:id/join`, ({ params }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const room = db.rooms.find((r) => r.id === Number(params['id']));
    if (!room) return HttpResponse.json({ detail: 'Room not found.' }, { status: 404 });
    if (!room.participants.some((p) => p.userId === session.userId)) {
      const user = db.users.find((u) => u.userId === session.userId)!;
      room.participants.push({
        userId: user.userId, displayName: user.displayName, avatarUrl: user.avatarUrl, role: 'Listener', joinedAt: new Date().toISOString(),
      });
    }
    return new HttpResponse(null, { status: 204 });
  }),

  http.post(`${BASE}/Rooms/:id/leave`, ({ params }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const room = db.rooms.find((r) => r.id === Number(params['id']));
    if (room) room.participants = room.participants.filter((p) => p.userId !== session.userId);
    return new HttpResponse(null, { status: 204 });
  }),

  http.get(`${BASE}/Rooms/:id/messages`, ({ params }) => {
    const room = db.rooms.find((r) => r.id === Number(params['id']));
    if (!room) return HttpResponse.json({ detail: 'Room not found.' }, { status: 404 });
    return HttpResponse.json(room.messages.map(messageDto));
  }),

  http.post(`${BASE}/Rooms/:id/messages`, async ({ params, request }) => {
    if (!session.userId) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const room = db.rooms.find((r) => r.id === Number(params['id']));
    if (!room) return HttpResponse.json({ detail: 'Room not found.' }, { status: 404 });
    const user = db.users.find((u) => u.userId === session.userId)!;
    const body = (await request.json()) as SendMessageRequest;
    const message = {
      id: ids.message(),
      roomId: room.id,
      senderId: user.userId,
      senderDisplayName: user.displayName,
      content: body.content,
      type: 'Text' as const,
      sentAt: new Date().toISOString(),
      isDeleted: false,
    };
    room.messages.push(message);
    return HttpResponse.json({ messageId: message.id }, { status: 201 });
  }),
];
