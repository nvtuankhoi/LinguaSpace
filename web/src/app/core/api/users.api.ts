import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AcceptFriendRequestRequest,
  AddLanguageRequest,
  FriendRequestDto,
  PaginatedResult,
  UpdateLanguageRequest,
  UpdateProfileRequest,
  UserLanguageDto,
  UserProfileDto,
  UserSummaryDto,
} from '../models';

@Injectable({ providedIn: 'root' })
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getUser(userId: string) {
    return this.http.get<UserProfileDto>(`${this.base}/Users/${userId}`, { withCredentials: true });
  }

  updateProfile(req: UpdateProfileRequest) {
    return this.http.put(`${this.base}/Users/me/profile`, req, {
      withCredentials: true,
      observe: 'response',
    });
  }

  getMyLanguages() {
    return this.http.get<UserLanguageDto[]>(`${this.base}/Users/me/languages`, { withCredentials: true });
  }

  addLanguage(req: AddLanguageRequest) {
    // Backend AddLanguage returns Created<int> → a bare languageId, not { languageId }.
    return this.http.post<number>(`${this.base}/Users/me/languages`, req, {
      withCredentials: true,
    });
  }

  updateLanguage(languageId: number, req: UpdateLanguageRequest) {
    return this.http.put(`${this.base}/Users/me/languages/${languageId}`, req, { withCredentials: true });
  }

  removeLanguage(languageId: number) {
    return this.http.delete(`${this.base}/Users/me/languages/${languageId}`, { withCredentials: true });
  }

  searchUsers(query: string, opts: { page?: number; pageSize?: number } = {}) {
    let params = new HttpParams().set('term', query).set('page', String(opts.page ?? 1)).set('pageSize', String(opts.pageSize ?? 50));
    return this.http.get<PaginatedResult<UserSummaryDto>>(`${this.base}/Users`, { params, withCredentials: true });
  }

  getFriends(userId: string) {
    return this.http.get<PaginatedResult<UserSummaryDto>>(`${this.base}/Users/${userId}/friends`, { withCredentials: true })
      .pipe(map(r => r.items));
  }

  getFollowers(userId: string) {
    return this.http.get<PaginatedResult<UserSummaryDto>>(`${this.base}/Users/${userId}/followers`, { withCredentials: true })
      .pipe(map(r => r.items));
  }

  getFollowing(userId: string) {
    return this.http.get<PaginatedResult<UserSummaryDto>>(`${this.base}/Users/${userId}/following`, { withCredentials: true })
      .pipe(map(r => r.items));
  }

  getFriendRequests() {
    return this.http.get<PaginatedResult<FriendRequestDto>>(`${this.base}/Users/me/friend-requests`, { withCredentials: true })
      .pipe(map(r => r.items));
  }

  sendFriendRequest(userId: string) {
    return this.http.post(`${this.base}/Users/${userId}/friend-request`, {}, { withCredentials: true, observe: 'response' });
  }

  respondFriendRequest(id: number, req: AcceptFriendRequestRequest) {
    return this.http.put(`${this.base}/Users/friend-requests/${id}`, req, { withCredentials: true, observe: 'response' });
  }

  cancelFriendRequest(id: number) {
    return this.http.delete(`${this.base}/Users/friend-requests/${id}`, { withCredentials: true, observe: 'response' });
  }

  removeFriend(userId: string) {
    return this.http.delete(`${this.base}/Users/${userId}/friendship`, { withCredentials: true, observe: 'response' });
  }

  followUser(userId: string) {
    return this.http.post(`${this.base}/Users/${userId}/follow`, {}, { withCredentials: true, observe: 'response' });
  }

  unfollowUser(userId: string) {
    return this.http.delete(`${this.base}/Users/${userId}/follow`, { withCredentials: true, observe: 'response' });
  }

  blockUser(userId: string) {
    return this.http.post(`${this.base}/Users/${userId}/block`, {}, { withCredentials: true, observe: 'response' });
  }

  unblockUser(userId: string) {
    return this.http.delete(`${this.base}/Users/${userId}/block`, { withCredentials: true, observe: 'response' });
  }

  getBlockedUsers() {
    return this.http.get<PaginatedResult<UserSummaryDto>>(`${this.base}/Users/me/blocked`, { withCredentials: true })
      .pipe(map(r => r.items));
  }
}
