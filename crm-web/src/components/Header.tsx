'use client';

import { useAuth } from '@/contexts/AuthContext';
import { LogOut } from 'lucide-react';
import Link from 'next/link';
import { Logo } from './Logo';

export function Header() {
  const { isAuthenticated, logout, isLoading } = useAuth();

  if (isLoading) {
    return (
      <header className="flex justify-between items-center mb-4 py-4 border-b border-slate-100">
        <div className="w-[150px]">
          <Logo variant="full" />
        </div>
        <nav className="flex gap-4 items-center">
          <div className="h-5 w-20 bg-slate-100 animate-pulse rounded"></div>
        </nav>
      </header>
    );
  }

  return (
    <header className="flex justify-between items-center mb-4 py-4 border-b border-slate-100">
      <Link href="/" className="hover:opacity-80 transition-opacity">
        <Logo variant="full" />
      </Link>
      <nav className="flex gap-4 items-center text-sm font-medium text-slate-600">
        <Link href="/" className="hover:text-slate-900">Início</Link>

        {!isAuthenticated && (
          <Link href="/login/" className="hover:text-slate-900">Login</Link>
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
