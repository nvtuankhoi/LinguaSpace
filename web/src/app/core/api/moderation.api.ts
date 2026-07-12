import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BanRequest, CreateReportRequest, PaginatedResult, ReportAction, ReportDto } from '../models';

/**
 * Moderation HTTP client. `report` is public (any authenticated user);
 * the review/ban endpoints are admin-only on the backend.
 */
@Injectable({ providedIn: 'root' })
export class ModerationApi {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  /** POST /api/Moderation/report → 201 (bare reportId). */
  report(req: CreateReportRequest): Observable<number> {
    return this.http.post<number>(`${this.base}/Moderation/report`, req, { withCredentials: true });
  }

  /** GET /api/Moderation/reports → paginated reports (admin). `status=null` falls back to Pending. */
  getReports(opts: { status?: string | null; page?: number; pageSize?: number } = {}): Observable<PaginatedResult<ReportDto>> {
    return this.http.get<PaginatedResult<ReportDto>>(`${this.base}/Moderation/reports`, {
      params: {
        ...(opts.status ? { status: opts.status } : {}),
        ...(opts.page ? { page: opts.page } : {}),
        ...(opts.pageSize ? { pageSize: opts.pageSize } : {}),
      },
      withCredentials: true,
    });
  }

  /** GET /api/Moderation/reports/{id} → single report (admin). */
  getReport(reportId: number): Observable<ReportDto> {
    return this.http.get<ReportDto>(`${this.base}/Moderation/reports/${reportId}`, { withCredentials: true });
  }

  /** POST /api/Moderation/reports/{id}/resolve → 204 (admin). action: 0=Resolve, 1=Dismiss. */
  resolveReport(reportId: number, action: ReportAction): Observable<void> {
    return this.http.post<void>(`${this.base}/Moderation/reports/${reportId}/resolve`, { action }, { withCredentials: true });
  }

  /** POST /api/Moderation/users/{userId}/ban → 204 (admin). Omit `until` for a permanent ban. */
  banUser(userId: string, req: BanRequest = {}): Observable<void> {
    return this.http.post<void>(`${this.base}/Moderation/users/${userId}/ban`, req, { withCredentials: true });
  }

  /** DELETE /api/Moderation/users/{userId}/ban → 204 (admin). */
  unbanUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/Moderation/users/${userId}/ban`, { withCredentials: true });
  }
}
