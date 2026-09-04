import React, { createContext, useContext, useState, useCallback, useMemo } from 'react';
import { ENV } from '../../config/env';

interface AuthContextType {
  token: string | null;
  userId: number | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => sessionStorage.getItem('auth_token'));
  const [userId, setUserId] = useState<number | null>(() => {
    const stored = sessionStorage.getItem('auth_user_id');
    return stored ? Number(stored) : null;
  });

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch(`${ENV.API_URL}/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
      throw new Error('Credenciales inválidas.');
    }

    const data = await res.json();
    setToken(data.token);
    setUserId(data.user_id);
    sessionStorage.setItem('auth_token', data.token);
    sessionStorage.setItem('auth_user_id', String(data.user_id));
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setUserId(null);
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('auth_user_id');
  }, []);

  const value = useMemo(
    () => ({
      token,
      userId,
      isAuthenticated: Boolean(token),
      login,
      logout,
    }),
    [token, userId, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe ser utilizado dentro de un AuthProvider');
  }
  return context;
};