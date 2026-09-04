import React from 'react';
import type { Session } from '../../domain/models/Session';

interface SessionItemProps {
  session: Session;
  onBook: (id: number) => void;
  disabled: boolean;
}

export const SessionItem: React.FC<SessionItemProps> = React.memo(({ session, onBook, disabled }) => {
  const isSoldOut = session.availableSeats <= 0;

  return (
    <li style={{
      border: '1px solid #e0e0e0',
      borderRadius: '8px',
      padding: '1.25rem',
      margin: '1rem 0',
      backgroundColor: isSoldOut ? '#f9f9f9' : '#ffffff',
      boxShadow: '0 2px 4px rgba(0,0,0,0.04)'
    }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h3 style={{ margin: '0 0 0.5rem 0', color: isSoldOut ? '#777' : '#111' }}>
          {session.title}
        </h3>
        <span style={{
          padding: '0.25rem 0.6rem',
          borderRadius: '12px',
          fontSize: '0.85rem',
          fontWeight: 'bold',
          backgroundColor: isSoldOut ? '#ffebee' : '#e8f5e9',
          color: isSoldOut ? '#c62828' : '#2e7d32'
        }}>
          {isSoldOut ? 'Agotado' : `${session.availableSeats} disponibles`}
        </span>
      </div>
      
      <p style={{ margin: '0.3rem 0', color: '#555' }}>
        <strong>Instructor:</strong> {session.instructor}
      </p>
      <p style={{ margin: '0.3rem 0', color: '#555' }}>
        <strong>Fecha:</strong> {new Date(session.startsAt).toLocaleString()} ({session.durationMinutes} min)
      </p>
      
      <button
        onClick={() => onBook(session.id)}
        disabled={disabled || isSoldOut}
        style={{
          marginTop: '0.8rem',
          padding: '0.6rem 1.2rem',
          cursor: (disabled || isSoldOut) ? 'not-allowed' : 'pointer',
          backgroundColor: isSoldOut ? '#ccc' : '#0066cc',
          color: '#fff',
          border: 'none',
          borderRadius: '4px',
          fontWeight: 600
        }}
      >
        {isSoldOut ? 'Sin cupo' : 'Reservar Plaza'}
      </button>
    </li>
  );
});

SessionItem.displayName = 'SessionItem';