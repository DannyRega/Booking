export interface Session {
  id: number;
  title: string;
  instructor: string;
  startsAt: string;
  durationMinutes: number;
  capacity: number;
  availableSeats: number;
}

export interface CursorPagedResponse<T> {
  items: T[];
  nextCursor: number | null;
  hasNextPage: boolean;
}

export interface SessionFilters {
  from?: string;
  to?: string;
  instructor?: string;
  onlyAvailable?: boolean;
}