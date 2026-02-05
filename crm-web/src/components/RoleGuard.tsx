'use client';

import { useAuth } from '@/contexts/AuthContext';
import { UserRole } from '@/types/auth';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

interface RoleGuardProps {
  children: React.ReactNode;
  allowedRoles: UserRole[];
}

export function RoleGuard({ children, allowedRoles }: RoleGuardProps) {
  const { user, isAuthenticated, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      if (!user || !allowedRoles.includes(user.role)) {
        // Redireciona para o dashboard ou uma página de erro se não tiver permissão
        router.push('/dashboard');
      }
    }
  }, [user, isAuthenticated, isLoading, allowedRoles, router]);

  if (isLoading) return null;

  if (isAuthenticated && user && allowedRoles.includes(user.role)) {
    return <>{children}</>;
  }

  return null;
}
