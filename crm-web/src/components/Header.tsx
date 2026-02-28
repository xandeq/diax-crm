'use client';

import { useAuth } from '@/contexts/AuthContext';
import { LogOut } from 'lucide-react';
import Link from 'next/link';
import { Logo } from './Logo';

export function Header() {
  const { isAuthenticated, user, isAdmin, logout, isLoading } = useAuth();

  if (isLoading) {
    return (
      <header className="relative z-50 flex justify-between items-center mb-4 py-4 border-b border-slate-100 bg-white">
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
    <header className="relative z-50 flex justify-between items-center mb-4 py-4 border-b border-slate-100 bg-white">
      <Link href="/" className="hover:opacity-80 transition-opacity">
        <Logo variant="full" />
      </Link>
      <nav className="flex gap-4 items-center text-sm font-medium text-slate-600 overflow-visible">
        <Link href="/" className="hover:text-slate-900">Início</Link>

        {isAuthenticated && (
          <>
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Financeiro
              </button>
              <div
                role="menu"
                className="absolute left-0 top-full min-w-[200px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
              >
                <Link
                  href="/finance"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Dashboard Financeiro
                </Link>
                <Link
                  href="/finance/planner"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Planejador Financeiro
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
                className="absolute left-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
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
                <div className="border-t border-slate-100 my-1"></div>
                <Link
                  href="/utilities/image-generation"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Geração de Imagens IA
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

            <Link href="/ads/" className="hover:text-slate-900 flex items-center gap-1">
              <svg viewBox="0 0 24 24" className="w-3.5 h-3.5 fill-[#1877F2]" aria-hidden="true">
                <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" />
              </svg>
              Anúncios
            </Link>

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
                <div className="border-t border-slate-100 my-1"></div>
                <Link
                  href="/outreach"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  Outreach
                </Link>
                <div className="border-t border-slate-100 my-1"></div>
                <Link
                  href="/email-marketing"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  ✉️ Email Marketing
                </Link>
                <Link
                  href="/analytics"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  📊 Analytics
                </Link>
                <Link
                  href="/leads/import"
                  className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                  role="menuitem"
                >
                  📥 Importar Leads
                </Link>
              </div>
            </div>

            {isAdmin && (
              <div className="relative group">
                <button
                  type="button"
                  className="hover:text-slate-900 font-medium"
                  aria-haspopup="menu"
                  aria-expanded="false"
                >
                  Admin
                </button>
                <div
                  role="menu"
                  className="absolute right-0 top-full min-w-[200px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
                >
                  <Link href="/users/" className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50" role="menuitem">Usuários</Link>
                  <Link href="/logs/" className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50" role="menuitem">Logs do Sistema</Link>
                  <div className="border-t border-slate-100 my-1"></div>
                  <Link href="/admin/groups" className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50" role="menuitem">Grupos & Permissões</Link>
                  <Link href="/admin/ai" className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50" role="menuitem">Provedores IA</Link>
                  <Link href="/admin/blog" className="block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50" role="menuitem">Blog</Link>
                </div>
              </div>
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
