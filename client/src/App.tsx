// client/src/App.tsx
import { useMemo, useState } from 'react';
import { AuthProvider, useAuth } from './presentation/context/AuthContext';
import { SessionRepository } from './infrastructure/repositories/SessionRepository';
import { BookingRepository } from './infrastructure/repositories/BookingRepository';
import { useSessions } from './presentation/hooks/useSessions';
import { useBookingOperations } from './presentation/hooks/useBookingOperations';
import { SessionItem } from './presentation/components/SessionItem';
import { ProtectedRoute } from './presentation/components/ProtectedRoute';
import { Toast } from './presentation/components/Toast';
import { SessionFiltersBar } from './presentation/components/SessionFiltersBar';
import type { SessionFilters } from './domain/models/Session';

function BookingDashboard() {
  const { isAuthenticated, login, logout, userId } = useAuth();
  const [authError, setAuthError] = useState<string | null>(null);
  const [lastBookingId, setLastBookingId] = useState<number | null>(null);

  const [filters, setFilters] = useState<SessionFilters>({
    instructor: '',
    from: '',
    to: '',
    onlyAvailable: false
  });

  const sessionRepo = useMemo(() => new SessionRepository(), []);
  const bookingRepo = useMemo(() => new BookingRepository(), []);

  const {
    sessions,
    loading,
    loadingMore,
    error: listError,
    hasNextPage,
    loadMore,
    refreshSessions
  } = useSessions(sessionRepo, 10, filters);

  const { reserve, cancel, isProcessing, toast, clearToast } = useBookingOperations(
    bookingRepo,
    refreshSessions
  );

  const handleTestLogin = async () => {
    setAuthError(null);
    try {
      await login('user1@test.com', 'password123');
    } catch {
      setAuthError('Fallo al autenticar usuario de prueba.');
    }
  };

  const handleReserve = async (sessionId: number) => {
    const bookingId = await reserve(sessionId);
    if (bookingId) {
      setLastBookingId(bookingId);
    }
  };
  
  const handleCancelLast = async () => {
    if (!lastBookingId) return;
    await cancel(lastBookingId);
    setLastBookingId(null);
  };

  const handleResetFilters = () => {
    setFilters({
      instructor: '',
      from: '',
      to: '',
      onlyAvailable: false
    });
  };

  return (
    <div style={{ maxWidth: 840, margin: '2rem auto', padding: '0 1rem', fontFamily: 'system-ui, sans-serif' }}>
      <header style={{ borderBottom: '2px solid #eee', paddingBottom: '1rem', marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '1.8rem' }}>Plataforma de Talleres</h1>
          <p style={{ margin: '0.2rem 0 0 0', color: '#666' }}>Sistema de reservas</p>
        </div>

        <div>
          {isAuthenticated ? (
            <div style={{ textAlign: 'right' }}>
              <span style={{ marginRight: '1rem', fontSize: '0.9rem' }}>Usuario ID: <strong>{userId}</strong></span>
              <button 
                onClick={logout}
                style={{ padding: '0.4rem 0.8rem', background: '#6c757d', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
              >
                Cerrar Sesión
              </button>
            </div>
          ) : (
            <button 
              onClick={handleTestLogin}
              style={{ padding: '0.5rem 1rem', background: '#28a745', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}
            >
              Iniciar sesión (user1@test.com)
            </button>
          )}
        </div>
      </header>

      {authError && (
        <div style={{ background: '#f8d7da', color: '#721c24', padding: '0.75rem', borderRadius: '4px', marginBottom: '1rem' }}>
          {authError}
        </div>
      )}

      <section>
        <ProtectedRoute>
          <h2>Talleres Disponibles</h2>

          <SessionFiltersBar
            filters={filters}
            onChange={setFilters}
            onReset={handleResetFilters}
          />

          {lastBookingId && (
            <div style={{
              background: '#e8f5e9',
              border: '1px solid #c8e6c9',
              padding: '1rem',
              borderRadius: '6px',
              marginBottom: '1.5rem',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center'
            }}>
              <span>Tienes una reserva activa: <strong>Reserva #{lastBookingId}</strong></span>
              <button
                onClick={handleCancelLast}
                disabled={isProcessing}
                style={{
                  padding: '0.4rem 0.9rem',
                  backgroundColor: '#d32f2f',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: isProcessing ? 'not-allowed' : 'pointer',
                  fontWeight: 600
                }}
              >
                {isProcessing ? 'Cancelando...' : 'Cancelar Reserva (Liberar cupo)'}
              </button>
            </div>
          )}

          {loading && <p>Cargando sesiones desde la API...</p>}
          {listError && <p style={{ color: '#d32f2f' }}>{listError}</p>}

          {!loading && sessions.length === 0 && !listError && (
            <p>No se encontraron talleres con los filtros seleccionados.</p>
          )}

          {/* LISTA DE SESIONES */}
          <ul style={{ listStyle: 'none', padding: 0 }}>
            {sessions.map((session) => (
              <SessionItem
                key={session.id}
                session={session}
                onBook={handleReserve}
                disabled={isProcessing}
              />
            ))}
          </ul>

          {/* Controles de Paginación por Cursor */}
          <div style={{ textAlign: 'center', margin: '2rem 0' }}>
            {hasNextPage ? (
              <button
                onClick={loadMore}
                disabled={loadingMore}
                style={{
                  padding: '0.75rem 1.75rem',
                  fontSize: '1rem',
                  fontWeight: 600,
                  backgroundColor: loadingMore ? '#9e9e9e' : '#1976d2',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: loadingMore ? 'not-allowed' : 'pointer',
                  boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
                }}
              >
                {loadingMore ? 'Cargando más talleres...' : 'Cargar más talleres'}
              </button>
            ) : (
              !loading && sessions.length > 0 && (
                <p style={{ color: '#757575', fontSize: '0.9rem' }}>
                  Has llegado al final del catálogo de talleres.
                </p>
              )
            )}
          </div>
        </ProtectedRoute>
      </section>

      <Toast toast={toast} onClose={clearToast} />
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <BookingDashboard />
    </AuthProvider>
  );
}