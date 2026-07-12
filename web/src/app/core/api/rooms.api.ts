import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CreateRoomRequest,
  MediaSessionStatusDto,
  MediaTokenDto,
  MessageDto,
  MuteRequest,
  PaginatedResult,
  RoomDto,
  RoomMediaParticipantDto,
  RoomSummaryDto,
  SendMessageRequest,
  UpdateRoomRequest,
} from '../models';

@Injectable({ providedIn: 'root' })
export class RoomsApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getRooms(opts: { languageCode?: string; roomType?: string; q?: string; page?: number; pageSize?: number } = {}) {
    let params = new HttpParams().set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 50));
    if (opts.languageCode) params = params.set('languageCode', opts.languageCode);
    if (opts.roomType) params = params.set('roomType', opts.roomType);
    if (opts.q) params = params.set('q', opts.q);
    return this.http.get<PaginatedResult<RoomSummaryDto>>(`${this.base}/Rooms`, { params, withCredentials: true });
  }

  getRoom(id: number) {
    return this.http.get<RoomDto>(`${this.base}/Rooms/${id}`, { withCredentials: true });
  }

  createRoom(req: CreateRoomRequest) {
    // Backend returns the new room id as a bare integer (OpenAPI: type [integer,string]).
    return this.http.post<number>(`${this.base}/Rooms`, req, { withCredentials: true });
  }

  join(id: number) {
    return this.http.post(`${this.base}/Rooms/${id}/join`, {}, { withCredentials: true });
  }

  leave(id: number) {
    return this.http.post(`${this.base}/Rooms/${id}/leave`, {}, { withCredentials: true });
  }

  // ---- Host controls (host-only, per contract) ----

  /** Host-only: update title/description/maxParticipants. PUT /Rooms/{id} → 204. */
  updateRoom(id: number, req: UpdateRoomRequest) {
    return this.http.put(`${this.base}/Rooms/${id}`, req, { withCredentials: true });
  }

  /** Host-only: delete the room. DELETE /Rooms/{id} → 204. */
  deleteRoom(id: number) {
    return this.http.delete(`${this.base}/Rooms/${id}`, { withCredentials: true });
  }

  /** Transfer host to another participant. POST /Rooms/{id}/transfer-host/{userId} → 204. */
  transferHost(roomId: number, targetUserId: string) {
    return this.http.post(`${this.base}/Rooms/${roomId}/transfer-host/${targetUserId}`, {}, { withCredentials: true });
  }

  /** Invite a user to the room. POST /Rooms/{id}/invite/{userId} → 204. */
  invite(roomId: number, targetUserId: string) {
    return this.http.post(`${this.base}/Rooms/${roomId}/invite/${targetUserId}`, {}, { withCredentials: true });
  }

  /** Mute/unmute a participant. POST /Rooms/{id}/mute/{userId} {mute} → 204. */
  mute(roomId: number, targetUserId: string, req: MuteRequest) {
    return this.http.post(`${this.base}/Rooms/${roomId}/mute/${targetUserId}`, req, { withCredentials: true });
  }

  /** Remove a participant from the room. DELETE /Rooms/{id}/kick/{userId} → 204. */
  kick(roomId: number, targetUserId: string) {
    return this.http.delete(`${this.base}/Rooms/${roomId}/kick/${targetUserId}`, { withCredentials: true });
  }

  getMessages(id: number) {
    return this.http.get<MessageDto[]>(`${this.base}/Rooms/${id}/messages`, { withCredentials: true });
  }

  sendMessage(id: number, req: SendMessageRequest) {
    return this.http.post<{ messageId: number }>(`${this.base}/Rooms/${id}/messages`, req, { withCredentials: true });
  }

  deleteMessage(roomId: number, messageId: number) {
    return this.http.delete(`${this.base}/Rooms/${roomId}/messages/${messageId}`, { withCredentials: true });
  }

  /** LiveKit SFU token for voice/video. POST /api/Rooms/{roomId}/media-token -> MediaTokenDto. */
  mediaToken(roomId: number) {
    return this.http.post<MediaTokenDto>(`${this.base}/Rooms/${roomId}/media-token`, {}, { withCredentials: true });
  }

  /** Users currently in the room's voice/video session. GET /Rooms/{roomId}/media/participants. */
  getMediaParticipants(roomId: number) {
    return this.http.get<RoomMediaParticipantDto[]>(`${this.base}/Rooms/${roomId}/media/participants`, {
      withCredentials: true,
    });
  }

  /** Whether the room's voice/video session is active + how many are in it. GET /Rooms/{roomId}/media/status. */
  getMediaStatus(roomId: number) {
    return this.http.get<MediaSessionStatusDto>(`${this.base}/Rooms/${roomId}/media/status`, {
      withCredentials: true,
    });
  }

  /** Host-only: terminate the LiveKit room, disconnecting everyone. DELETE /Rooms/{roomId}/media → 204. */
  endMediaSession(roomId: number) {
    return this.http.delete(`${this.base}/Rooms/${roomId}/media`, { withCredentials: true });
  }
}
