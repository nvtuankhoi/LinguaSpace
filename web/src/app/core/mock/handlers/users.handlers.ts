import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, session } from '../db';
import type { MockUser } from '../db';
import type { FriendRequestDto, UserProfileDto, UserSummaryDto } from '../../models';

const BASE = environment.apiBaseUrl;

const toProfile = (u: MockUser, myId: string | null): UserProfileDto => {
  const isMe = u.userId === myId;
  // In a real backend, we'd check relationships
  const isFriend = !isMe && db.notifications.some(n => n.type === 'FriendAccepted' && (n.payload as any)?.acceptorId === u.userId);
  return {
    id: 0,
    userId: u.userId,
    displayName: u.displayName,
    bio: u.bio,
    avatarUrl: u.avatarUrl,
    timezone: u.timezone,
    isOnline: u.isOnline,
    lastSeenAt: u.lastSeenAt,
    languages: u.languages,
    isFollowedByMe: false,
    isFriend: isFriend,
    hasOutgoingFriendRequest: false,
    hasIncomingFriendRequest: false,
    followerCount: u.followerCount,
    followingCount: u.followingCount,
    friendCount: u.friendCount,
    isBlockedByMe: false,
  };
};

const toSummary = (u: MockUser): UserSummaryDto => ({
  id: 0,
  userId: u.userId,
  displayName: u.displayName,
  avatarUrl: u.avatarUrl,
  isOnline: u.isOnline,
});

export const usersHandlers = [
  http.put(`${BASE}/Users/me/profile`, async ({ request }) => {
    const user = db.users.find((u) => u.userId === session.userId);
    if (!user) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const body = (await request.json()) as Partial<MockUser>;
    if (typeof body.displayName === 'string' && body.displayName.trim()) {
      user.displayName = body.displayName.trim();
    }
    if ('bio' in body) user.bio = body.bio ?? null;
    if ('timezone' in body) user.timezone = body.timezone ?? null;
    if ('avatarUrl' in body) user.avatarUrl = body.avatarUrl ?? null;
    return new HttpResponse(null, { status: 204 });
  }),

  http.get(`${BASE}/Users`, ({ request }) => {
    const url = new URL(request.url);
    const term = url.searchParams.get('term')?.toLowerCase() ?? '';
    const items = db.users
      .filter((u) => u.displayName.toLowerCase().includes(term) || u.userId.includes(term))
      .map(toSummary);
    return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.get(`${BASE}/Users/:userId/friends`, () => {
    const me = session.userId;
    if (!me) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    // Mock: everyone else is a friend
    const friends = db.users.filter(u => u.userId !== me).map(toSummary);
    return HttpResponse.json({ items: friends, totalCount: friends.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.get(`${BASE}/Users/:userId/followers`, () => {
    const me = session.userId;
    if (!me) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const followers = db.users.filter(u => u.userId !== me).slice(0, 2).map(toSummary);
    return HttpResponse.json({ items: followers, totalCount: followers.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.get(`${BASE}/Users/:userId/following`, () => {
    const me = session.userId;
    if (!me) return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    const following = db.users.filter(u => u.userId !== me).slice(1, 3).map(toSummary);
    return HttpResponse.json({ items: following, totalCount: following.length, page: 1, pageSize: 50, hasMore: false });
  }),

  http.get(`${BASE}/Users/me/friend-requests`, () => {
    const requests: FriendRequestDto[] = [
      { id: 10, requesterId: 'u-5', requesterDisplayName: 'Jin', requesterAvatarUrl: null, addresseeId: session.userId!, addresseeDisplayName: 'Me', addresseeAvatarUrl: null, createdAt: new Date().toISOString() }
    ];
    return HttpResponse.json({ items: requests, totalCount: requests.length, page: 1, pageSize: 20, hasMore: false });
  }),

  http.post(`${BASE}/Users/:userId/friend-request`, () => new HttpResponse(null, { status: 204 })),
  http.put(`${BASE}/Users/friend-requests/:id`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/Users/friend-requests/:id`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/Users/:userId/friendship`, () => new HttpResponse(null, { status: 204 })),
  http.post(`${BASE}/Users/:userId/follow`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/Users/:userId/follow`, () => new HttpResponse(null, { status: 204 })),
  http.post(`${BASE}/Users/:userId/block`, () => new HttpResponse(null, { status: 204 })),
  http.delete(`${BASE}/Users/:userId/block`, () => new HttpResponse(null, { status: 204 })),
  http.get(`${BASE}/Users/me/blocked`, () => HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 20, hasMore: false })),

  http.get(`${BASE}/Users/:userId`, ({ params }) => {
    const user = db.users.find((u) => u.userId === params['userId']);
    return user
      ? HttpResponse.json(toProfile(user, session.userId))
      : HttpResponse.json({ detail: 'User not found.' }, { status: 404 });
  }),
];
