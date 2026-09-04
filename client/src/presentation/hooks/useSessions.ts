import { useState, useEffect, useCallback, useTransition } from 'react';
import type { Session, SessionFilters } from '../../domain/models/Session';
import type { ISessionRepository } from '../../infrastructure/repositories/SessionRepository';

export const useSessions = (
  sessionRepo: ISessionRepository,
  limit = 10,
  filters?: SessionFilters
) => {
  const [sessions, setSessions] = useState<Session[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nextCursor, setNextCursor] = useState<number | null>(null);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [, startTransition] = useTransition();

  const loadInitialSessions = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await sessionRepo.getSessions(null, limit, filters);
      startTransition(() => {
        setSessions(response.items);
        setNextCursor(response.nextCursor);
        setHasNextPage(response.hasNextPage);
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al cargar los talleres.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [sessionRepo, limit, filters?.from, filters?.to, filters?.instructor, filters?.onlyAvailable]);

  const loadMore = useCallback(async () => {
    if (!hasNextPage || loadingMore || nextCursor === null) return;

    setLoadingMore(true);
    try {
      const response = await sessionRepo.getSessions(nextCursor, limit, filters);
      startTransition(() => {
        setSessions(prev => [...prev, ...response.items]);
        setNextCursor(response.nextCursor);
        setHasNextPage(response.hasNextPage);
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Error al cargar más talleres.';
      setError(msg);
    } finally {
      setLoadingMore(false);
    }
  }, [sessionRepo, nextCursor, hasNextPage, loadingMore, limit, filters]);

  useEffect(() => {
    loadInitialSessions();
  }, [loadInitialSessions]);

  return {
    sessions,
    loading,
    loadingMore,
    error,
    hasNextPage,
    loadMore,
    refreshSessions: loadInitialSessions
  };
};