'use client';

import { getAccessToken, setAccessToken as setApiToken } from '@/services/api';
import { decodeRoles } from '@/services/auth';
import { useRouter } from 'next/navigation';
import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react';

interface AuthContextType {
  isAuthenticated: boolean;
  login: (token: string) => void;
  logout: () => void;
  isLoading: boolean;
  roles: string[];
  isAdmin: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [roles, setRoles] = useState<string[]>([]);
  const router = useRouter();

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      setIsAuthenticated(true);
      setRoles(decodeRoles(token));
    }
    setIsLoading(false);
  }, []);

  const login = (token: string) => {
    if (!token || token === 'undefined' || token === 'null') {
      return;
    }
    setApiToken(token);
    setIsAuthenticated(true);
    setRoles(decodeRoles(token));
  };

  const logout = useCallback(() => {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem('accessToken');
    }
    setIsAuthenticated(false);
    setRoles([]);
    router.push('/login');
  }, [router]);

  useEffect(() => {
    const handleAuthExpired = () => {
      logout();
    };

    window.addEventListener('auth:expired', handleAuthExpired);
    return () => window.removeEventListener('auth:expired', handleAuthExpired);
  }, [logout]);

  const isAdmin = useMemo(() => roles.includes('Admin'), [roles]);

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout, isLoading, roles, isAdmin }}>
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
