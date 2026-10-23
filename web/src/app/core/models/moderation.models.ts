import type { ReportAction, ReportStatus, ReportTargetType } from './enums';

export interface ReportDto {
  id: number;
  reporterId: string;
  targetId: string;
  targetType: ReportTargetType;
  reason: string;
  status: ReportStatus;
  createdAt: string;
  resolvedAt: string | null;
  resolvedBy: string | null;
}

export interface CreateReportRequest {
  targetId: string;
  targetType: ReportTargetType;
  reason: string;
}

export interface ResolveReportRequest {
  action: ReportAction;
}

export interface BanRequest {
  /** Omit/null for a permanent ban. */
  until?: string | null;
}
