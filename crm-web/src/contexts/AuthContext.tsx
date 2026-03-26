'use client';

import { clearAccessToken, getAccessToken, setAccessToken as setApiToken } from '@/services/api';
import { User, UserRole } from '@/types/auth';
import { jwtDecode } from 'jwt-decode';
import { useRouter } from 'next/navigation';
import { createContext, ReactNode, useCallback, useContext, useEffect, useState } from 'react';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  hasRole: (role: UserRole) => boolean;
  login: (token: string) => void;
  logout: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  const decodeAndSetUser = useCallback((token: string) => {
    try {
      const decoded: any = jwtDecode(token);
      const email = decoded.email || decoded.sub || decoded[Object.keys(decoded).find(k => k.endsWith('/emailaddress')) || ''];
      const role = decoded.role || decoded[ROLE_CLAIM];

      if (email) {
        setUser({
          email,
          role: role as UserRole || 'User'
        });
        setIsAuthenticated(true);
      }
    } catch (error) {
      console.error('Failed to decode token:', error);
      setIsAuthenticated(false);
      setUser(null);
    }
  }, []);

  useEffect(() => {
    const token = getAccessToken();
    if (token) {
      decodeAndSetUser(token);
    }
    setIsLoading(false);
  }, [decodeAndSetUser]);

  const login = (token: string) => {
    if (!token || token === 'undefined' || token === 'null') {
      return;
    }
    setApiToken(token);
    decodeAndSetUser(token);
  };

  const logout = useCallback(() => {
    clearAccessToken();
    setIsAuthenticated(false);
    setUser(null);
    router.push('/login');
  }, [router]);

  useEffect(() => {
    const handleAuthExpired = () => {
      logout();
    };

    window.addEventListener('auth:expired', handleAuthExpired);
    return () => window.removeEventListener('auth:expired', handleAuthExpired);
  }, [logout]);

  const isAdmin = user?.role === 'Admin';
  const hasRole = (role: UserRole) => user?.role === role;

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated,
      isAdmin,
      hasRole,
      login,
      logout,
      isLoading
    }}>
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
