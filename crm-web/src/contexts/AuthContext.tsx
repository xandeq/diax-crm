'use client';

import { getAccessToken, setAccessToken as setApiToken } from '@/services/api';
import { useRouter } from 'next/navigation';
import { createContext, ReactNode, useCallback, useContext, useEffect, useState } from 'react';

interface AuthContextType {
  isAuthenticated: boolean;
  login: (token: string) => void;
  logout: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      setIsAuthenticated(true);
    }
    setIsLoading(false);
  }, []);

  const login = (token: string) => {
    if (!token || token === 'undefined' || token === 'null') {
      return;
    }
    setApiToken(token);
    setIsAuthenticated(true);
  };

  const logout = useCallback(() => {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem('accessToken');
    }
    setIsAuthenticated(false);
    router.push('/login');
  }, [router]);

  useEffect(() => {
    const handleAuthExpired = () => {
      logout();
    };

    window.addEventListener('auth:expired', handleAuthExpired);
    return () => window.removeEventListener('auth:expired', handleAuthExpired);
  }, [logout]);

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
