import type { CursorPagedResponse, Session, SessionFilters } from '../../domain/models/Session';
import { HttpClient } from '../api/HttpClient';

export interface ISessionRepository {
  getSessions(
    cursor?: number | null,
    limit?: number,
    filters?: SessionFilters
  ): Promise<CursorPagedResponse<Session>>;
}

export class SessionRepository implements ISessionRepository {
  async getSessions(
    cursor?: number | null,
    limit = 10,
    filters?: SessionFilters
  ): Promise<CursorPagedResponse<Session>> {
    const params = new URLSearchParams();
    params.set('limit', limit.toString());

    if (cursor !== undefined && cursor !== null) {
      params.set('cursor', cursor.toString());
    }

    if (filters?.from) params.set('from', filters.from);
    if (filters?.to) params.set('to', filters.to);
    if (filters?.instructor?.trim()) params.set('instructor', filters.instructor.trim());
    if (filters?.onlyAvailable) params.set('only_available', 'true');

    return HttpClient.request<CursorPagedResponse<Session>>(`/sessions?${params.toString()}`);
  }
}