import React from 'react';
import { useAuth } from '../context/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, fallback }) => {
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return (
      fallback ? <>{fallback}</> : (
        <div style={{ padding: '1.5rem', background: '#fff3cd', color: '#856404', borderRadius: '4px' }}>
          Acceso restringido. Por favor inicia sesión para interactuar con los talleres.
        </div>
      )
    );
  }

  return <>{children}</>;
};