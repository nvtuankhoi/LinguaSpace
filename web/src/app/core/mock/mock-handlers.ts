import type { HttpHandler } from 'msw';
import { authHandlers } from './handlers/auth.handlers';
import { feedHandlers } from './handlers/feed.handlers';
import { mediaHandlers } from './handlers/media.handlers';
import { notificationsHandlers } from './handlers/notifications.handlers';
import { roomsHandlers } from './handlers/rooms.handlers';
import { socialHandlers } from './handlers/social.handlers';
import { usersHandlers } from './handlers/users.handlers';
import { gamificationHandlers } from './handlers/gamification.handlers';

/**
 * MSW handler registry. Handlers are added per feature as each surface is
 * crafted, mirroring design/api-contract.md incrementally. Enabled in dev
 * via enable-mocks.ts.
 */
export const handlers: HttpHandler[] = [
  ...authHandlers,
  ...usersHandlers,
  ...feedHandlers,
  ...mediaHandlers,
  ...roomsHandlers,
  ...socialHandlers,
  ...notificationsHandlers,
  ...gamificationHandlers,
];
