import type { MessageType, ParticipantRole, RoomStatus, RoomType } from './enums';

export interface RoomParticipantDto {
  userId: string;
  displayName: string;
  avatarUrl: string | null;
  role: ParticipantRole;
  joinedAt: string;
  /** True when the room host has muted this participant's text chat. */
  isMuted: boolean;
}

export interface RoomDto {
  id: number;
  title: string;
  description: string | null;
  languageCode: string;
  maxParticipants: number;
  participantCount: number;
  status: RoomStatus;
  roomType: RoomType;
  hostId: string;
  created: string;
  participants: RoomParticipantDto[];
}

export interface RoomSummaryDto {
  id: number;
  title: string;
  languageCode: string;
  maxParticipants: number;
  participantCount: number;
  roomType: RoomType;
  hostId: string;
}

export interface MessageDto {
  id: number;
  senderId: string;
  senderDisplayName: string;
  content: string;
  type: MessageType;
  sentAt: string;
  isDeleted: boolean;
}

export interface CreateRoomRequest {
  title: string;
  description?: string | null;
  languageCode: string;
  maxParticipants: number;
  roomType: RoomType;
}

export interface UpdateRoomRequest {
  title: string;
  description?: string | null;
  maxParticipants: number;
}

export interface MuteRequest {
  mute: boolean;
}

export interface SendMessageRequest {
  content: string;
}

// ---- Media (room-scoped: /api/Rooms/{roomId}/media-*) ----

export interface MediaTokenDto {
  token: string;
  liveKitUrl: string;
}

export interface MediaSessionStatusDto {
  isActive: boolean;
  activeParticipantCount: number;
}

export interface RoomMediaParticipantDto {
  userId: string;
  joinedAt: string;
  leftAt: string | null;
  durationSeconds: number | null;
  wasScreenSharing: boolean;
  isActive: boolean;
}
