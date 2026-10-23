// Shared wrappers — mirror Application PaginatedResult / CursorPagedResult.

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

/** Cursor is an ISO date string (the last item's createdAt). */
export interface CursorPagedResult<T> {
  items: T[];
  hasMore: boolean;
  nextCursor: string | null;
}

export interface LookupDto {
  id: number;
  title: string | null;
}
