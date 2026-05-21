'use client';

import { useAuth } from '@/contexts/AuthContext';
import { LogOut, Menu, X } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { Logo } from './Logo';

const menuItemClass = "block px-4 py-2 text-sm text-slate-700 hover:bg-slate-50";
const dropdownClass = "absolute left-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50";
const separatorClass = "border-t border-slate-100 my-1";

interface MobileSectionProps {
  label: string;
  children: React.ReactNode;
}

function MobileSection({ label, children }: MobileSectionProps) {
  const [open, setOpen] = useState(false);
  return (
    <div>
      <button
        type="button"
        onClick={() => setOpen(o => !o)}
        className="flex w-full items-center justify-between px-4 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
      >
        {label}
        <span className="text-slate-400">{open ? '▲' : '▼'}</span>
      </button>
      {open && <div className="border-t border-slate-100 bg-slate-50">{children}</div>}
    </div>
  );
}

export function Header() {
  const { isAuthenticated, user, isAdmin, logout, isLoading } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);

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
    <>
      <header className="relative z-50 flex justify-between items-center mb-4 py-4 border-b border-slate-100 bg-white">
        <Link href="/" className="hover:opacity-80 transition-opacity">
          <Logo variant="full" />
        </Link>

        {/* Desktop nav */}
        <nav className="hidden md:flex gap-4 items-center text-sm font-medium text-slate-600 overflow-visible">
          <Link href="/dashboard" className="hover:text-slate-900">Início</Link>

          {isAuthenticated && (
            <>
              {/* ── Core Business: CRM ── */}
              <div className="relative group">
                <button type="button" className="hover:text-slate-900" aria-haspopup="menu" aria-expanded="false">
                  CRM
                </button>
                <div role="menu" className={dropdownClass}>
                  <Link href="/customers/" className={menuItemClass} role="menuitem">Clientes</Link>
                  <Link href="/leads/" className={menuItemClass} role="menuitem">Leads</Link>
                  <div className={separatorClass}></div>
                  <Link href="/leads/import" className={menuItemClass} role="menuitem">Importar Leads</Link>
                  <div className={separatorClass}></div>
                  <Link href="/helpdesk" className={menuItemClass} role="menuitem">Helpdesk</Link>
                </div>
              </div>

              {/* ── Core Business: Marketing ── */}
              <div className="relative group">
                <button type="button" className="hover:text-slate-900" aria-haspopup="menu" aria-expanded="false">
                  Marketing
                </button>
                <div role="menu" className={dropdownClass}>
                  <Link href="/outreach" className={menuItemClass} role="menuitem">Outreach</Link>
                  <Link href="/email-marketing" className={menuItemClass} role="menuitem">Email Marketing</Link>
                  <Link href="/email-marketing/pro" className={menuItemClass} role="menuitem">
                    Email Marketing PRO
                    <span className="ml-1.5 rounded bg-blue-600 px-1 py-0.5 text-[10px] font-bold text-white leading-none">NEW</span>
                  </Link>
                  <Link href="/campanhas" className={menuItemClass} role="menuitem">Campanhas</Link>
                  <div className={separatorClass}></div>
                  <Link href="/ads/" className={menuItemClass} role="menuitem">Anúncios (Meta Ads)</Link>
                  <div className={separatorClass}></div>
                  <Link href="/analytics" className={menuItemClass} role="menuitem">Analytics</Link>
                </div>
              </div>

              {/* ── Finanças ── */}
              <div className="relative group">
                <button type="button" className="hover:text-slate-900" aria-haspopup="menu" aria-expanded="false">
                  Finanças
                </button>
                <div role="menu" className={dropdownClass}>
                  <Link href="/finance/morning-briefing" className={menuItemClass} role="menuitem">Morning Briefing</Link>
                  <Link href="/finance" className={menuItemClass} role="menuitem">Dashboard Financeiro</Link>
                  <Link href="/finance/personal-control" className={menuItemClass} role="menuitem">Planilha Financeira</Link>
                  <div className={separatorClass}></div>
                  <Link href="/finance/transactions" className={menuItemClass} role="menuitem">Transações</Link>
                  <Link href="/finance/planner" className={menuItemClass} role="menuitem">Planejador Financeiro</Link>
                  <div className={separatorClass}></div>
                  <Link href="/finance/tax-documents" className={menuItemClass} role="menuitem">Imposto de Renda</Link>
                </div>
              </div>

              {/* ── AI Tools: IA ── */}
              <div className="relative group">
                <button type="button" className="hover:text-slate-900" aria-haspopup="menu" aria-expanded="false">
                  IA
                </button>
                <div role="menu" className={dropdownClass}>
                  <Link href="/ai-chat/" className={menuItemClass} role="menuitem">
                    Claude Chat
                    <span className="ml-1.5 rounded bg-emerald-600 px-1 py-0.5 text-[10px] font-bold text-white leading-none">NEW</span>
                  </Link>
                  <div className={separatorClass}></div>
                  <Link href="/utilities/image-generation" className={menuItemClass} role="menuitem">Geração de Imagens</Link>
                  <Link href="/utilities/prompt-generator" className={menuItemClass} role="menuitem">Gerador de Prompts</Link>
                  <Link href="/utilities/humanize-text" className={menuItemClass} role="menuitem">Humanizar Texto</Link>
                  <Link href="/utilities/email-subject-optimizer" className={menuItemClass} role="menuitem">Otimizador de Email</Link>
                  <Link href="/utilities/lead-persona-generator" className={menuItemClass} role="menuitem">Gerador de Personas</Link>
                  <Link href="/utilities/outreach-ab-test" className={menuItemClass} role="menuitem">Teste A/B Outreach</Link>
                  <Link href="/utilities/social-batch-generator" className={menuItemClass} role="menuitem">Batch Social Media</Link>
                  <Link href="/utilities/customer-insights" className={menuItemClass} role="menuitem">Insights de Clientes</Link>
                </div>
              </div>

              {/* ── Personal System: Pessoal ── */}
              <div className="relative group">
                <button type="button" className="hover:text-slate-900" aria-haspopup="menu" aria-expanded="false">
                  Pessoal
                </button>
                <div role="menu" className={dropdownClass}>
                  <Link href="/agenda" className={menuItemClass} role="menuitem">Agenda</Link>
                  <Link href="/tasks" className={menuItemClass} role="menuitem">Tarefas</Link>
                  <Link href="/household/checklists" className={menuItemClass} role="menuitem">Listas e Compras</Link>
                  <div className={separatorClass}></div>
                  <Link href="/utilities/snippets" className={menuItemClass} role="menuitem">Snippets</Link>
                  <Link href="/tools/html-extractor" className={menuItemClass} role="menuitem">Extrator HTML → Texto</Link>
                  <Link href="/tools/html-url-extractor" className={menuItemClass} role="menuitem">Extrator HTML → Links</Link>
                  <div className={separatorClass}></div>
                  <Link href="/tools/apps-inventory" className={menuItemClass} role="menuitem">Inventário de Apps</Link>
                </div>
              </div>

              {/* ── Admin (admin-only) ── */}
              {isAdmin && (
                <div className="relative group">
                  <button type="button" className="hover:text-slate-900 font-medium" aria-haspopup="menu" aria-expanded="false">
                    Admin
                  </button>
                  <div role="menu" className="absolute right-0 top-full min-w-[220px] rounded-md border border-slate-200 bg-white shadow-lg opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition z-50">
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
                <span className="text-xs text-slate-400 font-normal hidden lg:inline">{user?.email}</span>
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

        {/* Mobile hamburger */}
        {isAuthenticated && (
          <button
            type="button"
            className="md:hidden p-2 text-slate-600 hover:text-slate-900"
            onClick={() => setMobileOpen(o => !o)}
            aria-label={mobileOpen ? 'Fechar menu' : 'Abrir menu'}
          >
            {mobileOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
          </button>
        )}
      </header>

      {/* Mobile menu drawer */}
      {isAuthenticated && mobileOpen && (
        <div className="md:hidden fixed inset-0 z-40 flex flex-col">
          {/* Backdrop */}
          <div className="absolute inset-0 bg-black/30" onClick={() => setMobileOpen(false)} />

          {/* Drawer */}
          <nav className="relative z-10 mt-[73px] bg-white border-t border-slate-200 overflow-y-auto max-h-[calc(100vh-73px)] shadow-xl divide-y divide-slate-100">
            <Link href="/dashboard" className="block px-4 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50" onClick={() => setMobileOpen(false)}>
              Início
            </Link>

            <MobileSection label="CRM">
              <Link href="/customers/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Clientes</Link>
              <Link href="/leads/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Leads</Link>
              <Link href="/leads/import" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Importar Leads</Link>
              <Link href="/helpdesk" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Helpdesk</Link>
            </MobileSection>

            <MobileSection label="Marketing">
              <Link href="/outreach" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Outreach</Link>
              <Link href="/email-marketing" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Email Marketing</Link>
              <Link href="/email-marketing/pro" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Email Marketing PRO</Link>
              <Link href="/campanhas" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Campanhas</Link>
              <Link href="/ads/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Anúncios (Meta Ads)</Link>
              <Link href="/analytics" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Analytics</Link>
            </MobileSection>

            <MobileSection label="Finanças">
              <Link href="/finance/morning-briefing" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Morning Briefing</Link>
              <Link href="/finance" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Dashboard Financeiro</Link>
              <Link href="/finance/personal-control" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Planilha Financeira</Link>
              <Link href="/finance/transactions" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Transações</Link>
              <Link href="/finance/planner" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Planejador Financeiro</Link>
              <Link href="/finance/tax-documents" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Imposto de Renda</Link>
            </MobileSection>

            <MobileSection label="IA">
              <Link href="/ai-chat/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100 font-medium" onClick={() => setMobileOpen(false)}>Claude Chat</Link>
              <Link href="/utilities/image-generation" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Geração de Imagens</Link>
              <Link href="/utilities/prompt-generator" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Gerador de Prompts</Link>
              <Link href="/utilities/humanize-text" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Humanizar Texto</Link>
              <Link href="/utilities/email-subject-optimizer" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Otimizador de Email</Link>
              <Link href="/utilities/lead-persona-generator" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Gerador de Personas</Link>
              <Link href="/utilities/outreach-ab-test" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Teste A/B Outreach</Link>
              <Link href="/utilities/social-batch-generator" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Batch Social Media</Link>
              <Link href="/utilities/customer-insights" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Insights de Clientes</Link>
            </MobileSection>

            <MobileSection label="Pessoal">
              <Link href="/agenda" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Agenda</Link>
              <Link href="/tasks" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Tarefas</Link>
              <Link href="/household/checklists" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Listas e Compras</Link>
              <Link href="/utilities/snippets" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Snippets</Link>
              <Link href="/tools/html-extractor" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Extrator HTML → Texto</Link>
              <Link href="/tools/apps-inventory" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Inventário de Apps</Link>
            </MobileSection>

            {isAdmin && (
              <MobileSection label="Admin">
                <Link href="/users/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Usuários</Link>
                <Link href="/admin/groups" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Grupos & Permissões</Link>
                <Link href="/admin/ai" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Provedores IA</Link>
                <Link href="/admin/blog" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Blog</Link>
                <Link href="/logs/" className="block px-6 py-2.5 text-sm text-slate-600 hover:bg-slate-100" onClick={() => setMobileOpen(false)}>Logs do Sistema</Link>
              </MobileSection>
            )}

            {/* Logout mobile */}
            <div className="px-4 py-3">
              <p className="text-xs text-slate-400 mb-2">{user?.email}</p>
              <button
                onClick={() => { logout(); setMobileOpen(false); }}
                className="flex items-center gap-2 text-red-600 hover:text-red-800 text-sm font-medium"
              >
                <LogOut className="h-4 w-4" /> Sair
              </button>
            </div>
          </nav>
        </div>
      )}
    </>
  );
}
