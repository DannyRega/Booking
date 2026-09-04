import React, { useEffect } from 'react';

export interface ToastMessage {
  id: string;
  type: 'error' | 'success' | 'info';
  message: string;
}

interface ToastProps {
  toast: ToastMessage | null;
  onClose: () => void;
  duration?: number;
}

export const Toast: React.FC<ToastProps> = ({ toast, onClose, duration = 4000 }) => {
  useEffect(() => {
    if (!toast) return;

    const timer = setTimeout(() => {
      onClose();
    }, duration);

    return () => clearTimeout(timer);
  }, [toast, duration, onClose]);

  if (!toast) return null;

  const bgColors = {
    error: '#d32f2f',
    success: '#2e7d32',
    info: '#0288d1'
  };

  return (
    <div
      style={{
        position: 'fixed',
        bottom: '24px',
        right: '24px',
        zIndex: 9999,
        minWidth: '280px',
        maxWidth: '420px',
        backgroundColor: bgColors[toast.type],
        color: '#ffffff',
        padding: '12px 16px',
        borderRadius: '8px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.25)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        fontSize: '0.95rem',
        animation: 'slideUp 0.3s ease-out'
      }}
    >
      <span>{toast.message}</span>
      <button
        onClick={onClose}
        style={{
          marginLeft: '16px',
          background: 'transparent',
          border: 'none',
          color: '#ffffff',
          cursor: 'pointer',
          fontSize: '1.2rem',
          lineHeight: '1'
        }}
      >
        ×
      </button>
    </div>
  );
};