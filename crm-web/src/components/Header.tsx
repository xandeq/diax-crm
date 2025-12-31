'use client';

import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { LogOut } from 'lucide-react';

export function Header() {
  const { isAuthenticated, logout, isLoading } = useAuth();

  if (isLoading) {
    return (
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <strong>CRM</strong>
        <nav style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <div className="h-5 w-20 bg-slate-100 animate-pulse rounded"></div>
        </nav>
      </header>
    );
  }

  return (
    <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
      <strong>CRM</strong>
      <nav style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <Link href="/">Início</Link>
        
        {!isAuthenticated && (
          <Link href="/login/">Login</Link>
        )}

        {isAuthenticated && (
          <>
            <Link href="/dashboard/">Dashboard</Link>
            <Link href="/leads/">Leads</Link>
            <Link href="/customers/">Clientes</Link>
            <button 
              onClick={logout}
              className="flex items-center gap-1 text-red-600 hover:text-red-800 text-sm font-medium"
              title="Sair"
            >
              <LogOut className="h-4 w-4" /> Sair
            </button>
          </>
        )}
      </nav>
    </header>
  );
}
