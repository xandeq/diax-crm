'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

interface RoleGuardProps {
  allowedRoles: string[];
  children: React.ReactNode;
}

/**
 * Protege um trechos de rota por role.
 * Se o usuário não possui nenhum dos roles permitidos, redireciona para "/".
 */
export function RoleGuard({ allowedRoles, children }: RoleGuardProps) {
  const { roles, isLoading } = useAuth();
  const router = useRouter();

  const hasRole = allowedRoles.some((r) => roles.includes(r));

  useEffect(() => {
    if (!isLoading && !hasRole) {
      router.replace('/');
    }
  }, [isLoading, hasRole, router]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="text-center">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-current border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]" />
          <p className="mt-4 text-sm text-slate-600">Verificando permissões...</p>
        </div>
      </div>
    );
  }

  if (!hasRole) return null;

  return <>{children}</>;
}
