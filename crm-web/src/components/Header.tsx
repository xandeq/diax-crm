'use client';

import { useAuth } from '@/contexts/AuthContext';
import { LogOut } from 'lucide-react';
import Link from 'next/link';
import { Logo } from './Logo';

const menuItemClass = "block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50";
const dropdownClass = "absolute left-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50";
const separatorClass = "border-t border-slate-100 my-1";

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
        <Link href="/dashboard" className="hover:text-slate-900">Início</Link>

        {isAuthenticated && (
          <>
            {/* ── Core Business: CRM ── */}
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                CRM
              </button>
              <div role="menu" className={dropdownClass}>
                <Link href="/customers/" className={menuItemClass} role="menuitem">
                  Clientes
                </Link>
                <Link href="/leads/" className={menuItemClass} role="menuitem">
                  Leads
                </Link>
                <div className={separatorClass}></div>
                <Link href="/leads/import" className={menuItemClass} role="menuitem">
                  Importar Leads
                </Link>
              </div>
            </div>

            {/* ── Core Business: Marketing ── */}
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Marketing
              </button>
              <div role="menu" className={dropdownClass}>
                <Link href="/outreach" className={menuItemClass} role="menuitem">
                  Outreach
                </Link>
                <Link href="/email-marketing" className={menuItemClass} role="menuitem">
                  Email Marketing
                </Link>
                <div className={separatorClass}></div>
                <Link href="/ads/" className={menuItemClass} role="menuitem">
                  Anúncios (Meta Ads)
                </Link>
                <div className={separatorClass}></div>
                <Link href="/analytics" className={menuItemClass} role="menuitem">
                  Analytics
                </Link>
              </div>
            </div>

            {/* ── Operations: Operações ── */}
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Operações
              </button>
              <div role="menu" className={dropdownClass}>
                <Link href="/finance" className={menuItemClass} role="menuitem">
                  Dashboard Financeiro
                </Link>
                <Link href="/finance/planner" className={menuItemClass} role="menuitem">
                  Planejador Financeiro
                </Link>
                <div className={separatorClass}></div>
                <Link href="/finance/tax-documents" className={menuItemClass} role="menuitem">
                  Imposto de Renda
                </Link>
              </div>
            </div>

            {/* ── AI Tools: IA ── */}
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                IA
              </button>
              <div role="menu" className={dropdownClass}>
                <Link href="/utilities/image-generation" className={menuItemClass} role="menuitem">
                  Geração de Imagens
                </Link>
                <Link href="/utilities/prompt-generator" className={menuItemClass} role="menuitem">
                  Gerador de Prompts
                </Link>
                <Link href="/utilities/humanize-text" className={menuItemClass} role="menuitem">
                  Humanizar Texto
                </Link>
              </div>
            </div>

            {/* ── Personal System: Pessoal ── */}
            <div className="relative group">
              <button
                type="button"
                className="hover:text-slate-900"
                aria-haspopup="menu"
                aria-expanded="false"
              >
                Pessoal
              </button>
              <div role="menu" className={dropdownClass}>
                <Link href="/agenda" className={menuItemClass} role="menuitem">
                  Agenda
                </Link>
                <Link href="/household/checklists" className={menuItemClass} role="menuitem">
                  Listas e Compras
                </Link>
                <div className={separatorClass}></div>
                <Link href="/utilities/snippets" className={menuItemClass} role="menuitem">
                  Snippets
                </Link>
                <Link href="/tools/html-extractor" className={menuItemClass} role="menuitem">
                  Extrator HTML → Texto
                </Link>
                <Link href="/tools/html-url-extractor" className={menuItemClass} role="menuitem">
                  Extrator HTML → Links
                </Link>
              </div>
            </div>

            {/* ── Admin (admin-only) ── */}
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
                  className="absolute right-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50"
                >
                  <Link href="/users/" className={menuItemClass} role="menuitem">Usuários</Link>
                  <Link href="/admin/groups" className={menuItemClass} role="menuitem">Grupos & Permissões</Link>
                  <div className={separatorClass}></div>
                  <Link href="/admin/ai" className={menuItemClass} role="menuitem">Provedores IA</Link>
                  <Link href="/admin/blog" className={menuItemClass} role="menuitem">Blog</Link>
                  <div className={separatorClass}></div>
                  <Link href="/logs/" className={menuItemClass} role="menuitem">Logs do Sistema</Link>
                </div>
              </div>
            )}

            {/* ── User / Logout ── */}
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

        {!isAuthenticated && (
          <Link href="/login/" className="hover:text-slate-900">Login</Link>
        )}
      </nav>
    </header>
  );
}
