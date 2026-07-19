import { http, HttpResponse } from 'msw';

import { environment } from '../../../../environments/environment';
import { db, session } from '../db';
import type { MockUser } from '../db';

const BASE = environment.apiBaseUrl;
const MOCK_TOKEN = 'mock-access-token';

export const authHandlers = [
  http.post(`${BASE}/Auth/login`, async ({ request }) => {
    const body = (await request.json()) as { email?: string; password?: string };
    const user = db.users.find((u) => u.email.toLowerCase() === (body.email ?? '').toLowerCase());
    if (!user || user.password !== body.password) {
      return HttpResponse.json({ detail: 'Invalid email or password.' }, { status: 401 });
    }
    session.userId = user.userId;
    return HttpResponse.json({ accessToken: MOCK_TOKEN, expiresIn: 3600, userId: user.userId, email: user.email });
  }),

  http.post(`${BASE}/Auth/register`, async ({ request }) => {
    const body = (await request.json()) as { email?: string; password?: string };
    const email = (body.email ?? '').toLowerCase();
    if (!email || !body.password) {
      return HttpResponse.json({ detail: 'Email and password are required.' }, { status: 400 });
    }
    if (db.users.some((u) => u.email.toLowerCase() === email)) {
      return HttpResponse.json({ detail: 'That email is already registered.' }, { status: 409 });
    }
    const user: MockUser = {
      userId: `u-${db.users.length + 1}`,
      email,
      password: body.password,
      displayName: email.split('@')[0],
      avatarUrl: null,
      bio: null,
      timezone: null,
      isOnline: true,
      lastSeenAt: null,
      languages: [],
      followerCount: 0,
      followingCount: 0,
      friendCount: 0,
      totalXp: 0,
      currentStreak: 0,
      longestStreak: 0,
      lastActivityAt: null,
      badges: [],
    };
    db.users.push(user);
    return HttpResponse.json({ userId: user.userId, email: user.email }, { status: 201 });
  }),

  http.post(`${BASE}/Auth/refresh`, () =>
    session.userId
      ? HttpResponse.json({ accessToken: MOCK_TOKEN, expiresIn: 3600 })
      : HttpResponse.json({ detail: 'Session expired.' }, { status: 401 }),
  ),

  http.get(`${BASE}/Auth/me`, () => {
    const user = db.users.find((u) => u.userId === session.userId);
    if (!user) {
      return HttpResponse.json({ detail: 'Not authenticated.' }, { status: 401 });
    }
    return HttpResponse.json({
      userId: user.userId,
      email: user.email,
      displayName: user.displayName,
      roles: [],
      avatarUrl: user.avatarUrl,
      isEmailConfirmed: true,
    });
  }),

  http.post(`${BASE}/Auth/logout`, () => {
    session.userId = null;
    return new HttpResponse(null, { status: 204 });
  }),
];
