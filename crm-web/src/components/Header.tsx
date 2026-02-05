'use client';

import { useAuth } from '@/contexts/AuthContext';
import { LogOut } from 'lucide-react';
import Link from 'next/link';
import { Logo } from './Logo';

export function Header() {
  const { isAuthenticated, user, isAdmin, logout, isLoading } = useAuth();

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

        {isAuthenticated && (
          <>
            <Link href="/finance" className="hover:text-slate-900">Financeiro</Link>

            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Casa e Família
              </button>
              <div
                role="menu"
                className="absolute left-0 top-full min-w-[200px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
              >
                <Link
                  href="/household/checklists"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Listas e Compras
                </Link>
              </div>
            </div>

            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Utilitários
              </button>
              <div
                role="menu"
                className="absolute left-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition"
              >
                <Link
                  href="/tools/html-extractor"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Extrator de Texto (HTML → Texto)
                </Link>
                <Link
                  href="/tools/html-url-extractor"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Extrator de URLs (HTML → Links)
                </Link>
                <Link
                  href="/utilities/prompt-generator"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Gerador de Prompts
                </Link>
                <Link
                  href="/utilities/humanize-text"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Humanizar Texto
                </Link>
                <Link
                  href="/utilities/snippets"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Snippets
                </Link>
              </div>
            </div>
          </>
        )}

        {!isAuthenticated && (
          <Link href="/login/" className="hover:text-slate-900">Login</Link>
        )}

        {isAuthenticated && (
          <>
            <Link href="/dashboard/">Dashboard</Link>

            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Clientes
              </button>
              <div
                role="menu"
                className="absolute left-0 top-full min-w-[180px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
              >
                <Link
                  href="/customers/"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Clientes
                </Link>
                <Link
                  href="/leads/"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Leads
                </Link>
              </div>
            </div>

            {isAdmin && (
              <>
                <Link href="/users/" className="hover:text-slate-900">Usuários</Link>
                <Link href="/logs/" className="hover:text-slate-900">Logs</Link>
              </>
            )}

            <div className="flex items-center gap-3 pl-2 border-l border-slate-200">
              <span className="text-xs text-slate-400 font-normal hidden md:inline">
                {user?.email}
              </span>
              <button
                onClick={logout}
                className="flex items-center gap-1 text-red-600 hover:text-red-800 text-sm font-medium"
                title="Sair"
              >
                <LogOut className="h-4 w-4" /> Sair
              </button>
            </div>
          </>
        )}
      </nav>
    </header>
  );
}
