import type { NotificationType } from './enums';

export interface NotificationDto {
  id: number;
  type: NotificationType;
  /** Shape varies by `type` — see design/api-contract.md for the per-type payload map. */
  payload: Record<string, unknown> | null;
  isRead: boolean;
  createdAt: string;
}

export interface MarkNotificationsRequest {
  /** Omit/null to mark all as read (or delete all). */
  notificationIds?: number[] | null;
}
