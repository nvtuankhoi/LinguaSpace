import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, ids, session } from '../db';
import type { MockConversation, MockDm } from '../db';
import type {
  ConversationDto,
  ConversationsUnreadCountDto,
  DirectMessageDto,
  SendDmRequest,
} from '../../models';

const BASE = environment.apiBaseUrl;

const meId = (): string | null => session.userId;

/** The other participant in a conversation (relative to the current user). */
const otherOf = (c: MockConversation, uid: string): string => c.participantIds.find((p) => p !== uid) ?? c.participantIds[0];

const dmDto = (m: MockDm): DirectMessageDto => ({
  id: m.id,
  conversationId: m.conversationId,
  senderId: m.senderId,
  content: m.content,
  sentAt: m.sentAt,
  isRead: m.isRead,
  isDeleted: m.isDeleted,
  editedAt: m.editedAt,
});

const conversationDto = (c: MockConversation, uid: string): ConversationDto => {
  const other = otherOf(c, uid);
  const user = db.users.find((u) => u.userId === other);
  const visible = c.messages.filter((m) => !m.isDeleted);
  const last = visible[visible.length - 1] ?? null;
  const unread = c.messages.filter((m) => m.senderId !== uid && !m.isRead).length;
  return {
    id: c.id,
    otherUserId: other,
    otherUserDisplayName: user?.displayName ?? null,
    otherUserAvatarUrl: user?.avatarUrl ?? null,
    lastMessage: last?.content ?? null,
    lastMessageAt: last?.sentAt ?? null,
    unreadCount: unread,
  };
};

const conversationsFor = (uid: string): MockConversation[] =>
  db.conversations.filter((c) => c.participantIds.includes(uid));

export const socialHandlers = [
  http.get(`${BASE}/Social/conversations`, () => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const items = conversationsFor(uid)
      .map((c) => conversationDto(c, uid))
      .sort((a, b) => (b.lastMessageAt ?? '').localeCompare(a.lastMessageAt ?? ''));
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 50, hasMore: false });
  }),

  // Must precede /conversations/:id/* so 'unread-count' isn't captured as an id.
  http.get(`${BASE}/Social/conversations/unread-count`, () => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const convs = conversationsFor(uid);
    const unreadConversations = convs.filter((c) => c.messages.some((m) => m.senderId !== uid && !m.isRead)).length;
    const totalUnread = convs.reduce((sum, c) => sum + c.messages.filter((m) => m.senderId !== uid && !m.isRead).length, 0);
    const body: ConversationsUnreadCountDto = { unreadConversations, totalUnread };
    return HttpResponse.json(body);
  }),

  // Search messages within a conversation by content (case-insensitive in mock).
  http.get(`${BASE}/Social/conversations/:conversationId/messages/search`, ({ params, request }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const conv = db.conversations.find((c) => c.id === Number(params['conversationId']));
    if (!conv) return HttpResponse.json({ detail: 'Conversation not found.' }, { status: 404 });
    const term = (new URL(request.url).searchParams.get('term') ?? '').trim().toLowerCase();
    const items = term
      ? conv.messages.filter((m) => !m.isDeleted && m.content.toLowerCase().includes(term)).map(dmDto)
      : [];
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 30, hasMore: false });
  }),

  http.get(`${BASE}/Social/conversations/:conversationId/messages`, ({ params }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const conv = db.conversations.find((c) => c.id === Number(params['conversationId']));
    if (!conv) return HttpResponse.json({ detail: 'Conversation not found.' }, { status: 404 });
    const items = conv.messages.map(dmDto);
    return HttpResponse.json({ items, hasMore: false, nextCursor: null });
  }),

  http.post(`${BASE}/Social/conversations/:conversationId/read`, ({ params }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const conv = db.conversations.find((c) => c.id === Number(params['conversationId']));
    if (conv) {
      conv.messages.forEach((m) => {
        if (m.senderId !== uid) m.isRead = true;
      });
    }
    return new HttpResponse(null, { status: 204 });
  }),

  // Clear the current user's own messages in a conversation (soft-delete; others' stay).
  http.delete(`${BASE}/Social/conversations/:conversationId/messages`, ({ params }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const conv = db.conversations.find((c) => c.id === Number(params['conversationId']));
    if (!conv) return HttpResponse.json({ detail: 'Conversation not found.' }, { status: 404 });
    conv.messages.forEach((m) => {
      if (m.senderId === uid && !m.isDeleted) {
        m.isDeleted = true;
        m.content = '[deleted]';
      }
    });
    return new HttpResponse(null, { status: 204 });
  }),

  http.post(`${BASE}/Social/dm`, async ({ request }) => {
    const uid = meId();
    if (!uid) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as SendDmRequest;
    if (!body.recipientId || !body.content?.trim()) {
      return HttpResponse.json({ detail: 'Recipient and content are required.' }, { status: 400 });
    }
    let conv = db.conversations.find(
      (c) => c.participantIds.includes(uid) && c.participantIds.includes(body.recipientId),
    );
    if (!conv) {
      conv = { id: ids.conversation(), participantIds: [uid, body.recipientId], messages: [] };
      db.conversations.unshift(conv);
    }
    const dm: MockDm = {
      id: ids.dm(),
      conversationId: conv.id,
      senderId: uid,
      content: body.content.trim(),
      sentAt: new Date().toISOString(),
      isRead: true,
      isDeleted: false,
      editedAt: null,
    };
    conv.messages.push(dm);
    return HttpResponse.json(dmDto(dm), { status: 201 });
  }),
];
