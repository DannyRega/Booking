import React from 'react';
import type { SessionFilters } from '../../domain/models/Session';

interface FiltersBarProps {
  filters: SessionFilters;
  onChange: (newFilters: SessionFilters) => void;
  onReset: () => void;
}

export const SessionFiltersBar: React.FC<FiltersBarProps> = ({ filters, onChange, onReset }) => {
  return (
    <div style={{
      backgroundColor: '#f8f9fa',
      padding: '1rem',
      borderRadius: '8px',
      marginBottom: '1.5rem',
      display: 'flex',
      flexWrap: 'wrap',
      gap: '1rem',
      alignItems: 'flex-end'
    }}>
      <div style={{ display: 'flex', flexDirection: 'column', flex: '1 1 180px' }}>
        <label style={{ fontSize: '0.85rem', fontWeight: 600, marginBottom: '0.3rem' }}>
          Instructor:
        </label>
        <input
          type="text"
          placeholder="Ej. Instructor 1..."
          value={filters.instructor || ''}
          onChange={(e) => onChange({ ...filters, instructor: e.target.value })}
          style={{ padding: '0.45rem', borderRadius: '4px', border: '1px solid #ccc' }}
        />
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', flex: '1 1 140px' }}>
        <label style={{ fontSize: '0.85rem', fontWeight: 600, marginBottom: '0.3rem' }}>
          Desde:
        </label>
        <input
          type="date"
          value={filters.from || ''}
          onChange={(e) => onChange({ ...filters, from: e.target.value })}
          style={{ padding: '0.4rem', borderRadius: '4px', border: '1px solid #ccc' }}
        />
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', flex: '1 1 140px' }}>
        <label style={{ fontSize: '0.85rem', fontWeight: 600, marginBottom: '0.3rem' }}>
          Hasta:
        </label>
        <input
          type="date"
          value={filters.to || ''}
          onChange={(e) => onChange({ ...filters, to: e.target.value })}
          style={{ padding: '0.4rem', borderRadius: '4px', border: '1px solid #ccc' }}
        />
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', paddingBottom: '0.5rem' }}>
        <input
          type="checkbox"
          id="only_avail"
          checked={Boolean(filters.onlyAvailable)}
          onChange={(e) => onChange({ ...filters, onlyAvailable: e.target.checked })}
          style={{ width: '16px', height: '16px', cursor: 'pointer' }}
        />
        <label htmlFor="only_avail" style={{ fontSize: '0.9rem', cursor: 'pointer', fontWeight: 500 }}>
          Solo con cupo
        </label>
      </div>

      <button
        onClick={onReset}
        style={{
          padding: '0.5rem 1rem',
          backgroundColor: '#6c757d',
          color: '#fff',
          border: 'none',
          borderRadius: '4px',
          cursor: 'pointer',
          marginBottom: '0.2rem'
        }}
      >
        Limpiar
      </button>
    </div>
  );
};