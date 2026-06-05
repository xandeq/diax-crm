'use client';

import { useAuth } from '@/contexts/AuthContext';
import {
  Activity, BarChart2, Bell, Bot, Briefcase, Bug, Calendar,
  ChevronRight, CreditCard, DollarSign, FileText, Globe,
  HelpCircle, LayoutDashboard, Link2, ListChecks, LogOut,
  Mail, Megaphone, MessageSquare, Package, Plus, Search,
  Settings, Shield, Star, Tag, Target, TrendingUp, Users,
  Wallet, Zap, Cpu
} from 'lucide-react';
import { usePathname } from 'next/navigation';
import { useRef, useState } from 'react';

type NavChild = { label: string; href: string; badge?: string };
type NavItem = { icon: React.ElementType; label: string; href?: string; children?: NavChild[]; badge?: string };
type NavGroup = { section: string; items: NavItem[] };

const navGroups: NavGroup[] = [
  {
    section: 'Principal',
    items: [
      { icon: LayoutDashboard, label: 'Dashboard', href: '/dashboard' },
    ],
  },
  {
    section: 'CRM',
    items: [
      { icon: Users, label: 'Clientes', href: '/customers/' },
      { icon: TrendingUp, label: 'Leads', children: [
        { label: 'Todos os Leads', href: '/leads/' },
        { label: 'Importar Leads', href: '/leads/import' },
      ]},
      { icon: HelpCircle, label: 'Helpdesk', href: '/helpdesk' },
    ],
  },
  {
    section: 'Marketing',
    items: [
      { icon: Mail, label: 'Outreach', href: '/outreach' },
      { icon: Megaphone, label: 'Email Marketing', children: [
        { label: 'Email Marketing', href: '/email-marketing' },
        { label: 'Email Marketing PRO', href: '/email-marketing/pro', badge: 'NEW' },
        { label: 'Campanhas', href: '/campanhas' },
      ]},
      { icon: Globe, label: 'Meta Ads', href: '/ads/' },
      { icon: BarChart2, label: 'Analytics', href: '/analytics' },
    ],
  },
  {
    section: 'Finanças',
    items: [
      { icon: Star, label: 'Morning Briefing', href: '/finance/morning-briefing' },
      { icon: LayoutDashboard, label: 'Dashboard Financeiro', href: '/finance' },
      { icon: Wallet, label: 'Planilha Financeira', href: '/finance/personal-control' },
      { icon: DollarSign, label: 'Transações', children: [
        { label: 'Todas as Transações', href: '/finance/transactions' },
        { label: 'Receitas', href: '/finance/incomes' },
        { label: 'Despesas', href: '/finance/expenses' },
        { label: 'Transferências', href: '/finance/transfers' },
        { label: 'Importar OFX/CSV', href: '/finance/imports' },
      ]},
      { icon: CreditCard, label: 'Cartões de Crédito', href: '/finance/credit-cards' },
      { icon: Briefcase, label: 'Contas', href: '/finance/accounts' },
      { icon: Target, label: 'Planejador', children: [
        { label: 'Planejador Financeiro', href: '/finance/planner' },
        { label: 'Metas Financeiras', href: '/finance/planner/goals' },
        { label: 'Recorrentes', href: '/finance/planner/recurring' },
      ]},
      { icon: FileText, label: 'Imposto de Renda', href: '/finance/tax-documents' },
    ],
  },
  {
    section: 'IA',
    items: [
      { icon: MessageSquare, label: 'Claude Chat', href: '/ai-chat/', badge: 'NEW' },
      { icon: Cpu, label: 'Anthropic Proxy', href: '/tools/anthropic-proxy', badge: 'API' },
      { icon: Zap, label: 'Ferramentas IA', children: [
        { label: 'Geração de Imagens', href: '/utilities/image-generation' },
        { label: 'Gerador de Prompts', href: '/utilities/prompt-generator' },
        { label: 'Humanizar Texto', href: '/utilities/humanize-text' },
        { label: 'Otimizador de Email', href: '/utilities/email-subject-optimizer' },
        { label: 'Gerador de Personas', href: '/utilities/lead-persona-generator' },
        { label: 'Teste A/B Outreach', href: '/utilities/outreach-ab-test' },
        { label: 'Batch Social Media', href: '/utilities/social-batch-generator' },
        { label: 'Insights de Clientes', href: '/utilities/customer-insights' },
      ]},
    ],
  },
  {
    section: 'Pessoal',
    items: [
      { icon: Calendar, label: 'Agenda', href: '/agenda' },
      { icon: ListChecks, label: 'Tarefas', href: '/tasks' },
      { icon: Package, label: 'Listas e Compras', href: '/household/checklists' },
      { icon: Tag, label: 'Snippets', href: '/utilities/snippets' },
      { icon: Link2, label: 'Extratores', children: [
        { label: 'HTML → Texto', href: '/tools/html-extractor' },
        { label: 'HTML → Links', href: '/tools/html-url-extractor' },
      ]},
      { icon: Briefcase, label: 'Inventário de Apps', href: '/tools/apps-inventory' },
      { icon: Activity, label: 'Status das Apps', href: '/tools/status' },
    ],
  },
  {
    section: 'Admin',
    items: [
      { icon: Users, label: 'Usuários', href: '/users/' },
      { icon: Shield, label: 'Grupos & Permissões', href: '/admin/groups' },
      { icon: Bot, label: 'Provedores IA', href: '/admin/ai' },
      { icon: FileText, label: 'Blog', children: [
        { label: 'Gerenciar Blog', href: '/admin/blog' },
        { label: 'Novo Post', href: '/admin/blog/new' },
      ]},
      { icon: Activity, label: 'Logs do Sistema', href: '/logs/' },
      { icon: Bug, label: 'Central de Erros', href: '/monitoring', badge: 'NEW' },
    ],
  },
];

const PUBLIC_ROUTES = ['/login', '/register', '/forgot-password'];

const SHELL_CSS = `
  .sh-wrap {
    position: fixed; inset: 0; z-index: 50;
    display: flex;
    background: #0F1A14;
    font-family: var(--font-jakarta, 'Plus Jakarta Sans', sans-serif);
  }
  .sh-sidebar {
    width: 220px; flex-shrink: 0;
    background: #0B1510;
    border-right: 1px solid rgba(255,255,255,0.06);
    display: flex; flex-direction: column;
    overflow-y: auto; overflow-x: hidden;
  }
  .sh-sidebar::-webkit-scrollbar { width: 4px; }
  .sh-sidebar::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.1); border-radius: 999px; }
  .sh-logo {
    padding: 20px 18px 14px;
    display: flex; align-items: center; gap: 10px;
    border-bottom: 1px solid rgba(255,255,255,0.06);
    flex-shrink: 0;
  }
  .sh-logo-icon {
    width: 32px; height: 32px; border-radius: 10px;
    background: linear-gradient(135deg, #10B981, #059669);
    display: flex; align-items: center; justify-content: center;
    font-weight: 800; font-size: 14px; color: #fff; flex-shrink: 0;
  }
  .sh-logo-text { font-size: 15px; font-weight: 700; color: #F9FAFB; }
  .sh-logo-sub { font-size: 10px; color: #9CA3AF; }
  .sh-nav { flex: 1; padding: 10px 8px; display: flex; flex-direction: column; gap: 1px; }
  .sh-section { font-size: 10px; font-weight: 700; color: #6B7280; text-transform: uppercase; letter-spacing: .1em; padding: 12px 10px 3px; }
  .sh-item {
    display: flex; align-items: center; gap: 9px;
    padding: 8px 10px; border-radius: 9px;
    cursor: pointer; transition: background .12s;
    color: #9CA3AF; font-size: 13px; font-weight: 500;
    text-decoration: none; width: 100%;
  }
  .sh-item:hover { background: rgba(255,255,255,0.05); color: #F9FAFB; }
  .sh-item.active { background: rgba(16,185,129,0.15); color: #10B981; }
  .sh-item-label { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .sh-badge {
    font-size: 9px; font-weight: 700; padding: 1px 5px; border-radius: 4px;
    background: #10B981; color: #fff; flex-shrink: 0;
  }
  .sh-group { position: relative; }
  .sh-flyout {
    position: fixed; left: 220px; width: 210px;
    background: #0D1F18;
    border: 1px solid rgba(255,255,255,0.12);
    border-radius: 10px; padding: 6px;
    z-index: 200; box-shadow: 0 8px 32px rgba(0,0,0,.6);
  }
  .sh-flyout-item {
    display: block; padding: 7px 10px; border-radius: 7px;
    font-size: 12px; color: #9CA3AF; cursor: pointer;
    text-decoration: none; transition: background .1s, color .1s;
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  }
  .sh-flyout-item:hover { background: rgba(255,255,255,0.07); color: #F9FAFB; }
  .sh-user {
    padding: 11px 14px;
    border-top: 1px solid rgba(255,255,255,0.06);
    display: flex; align-items: center; gap: 9px; flex-shrink: 0;
  }
  .sh-avatar {
    width: 32px; height: 32px; border-radius: 50%;
    background: linear-gradient(135deg, #10B981, #6366F1);
    display: flex; align-items: center; justify-content: center;
    font-size: 11px; font-weight: 700; color: #fff; flex-shrink: 0;
  }
  .sh-main { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
  .sh-topbar {
    height: 56px; flex-shrink: 0;
    background: rgba(11,21,16,0.96); backdrop-filter: blur(12px);
    border-bottom: 1px solid rgba(255,255,255,0.06);
    display: flex; align-items: center; padding: 0 22px; gap: 14px;
  }
  .sh-search {
    flex: 1; max-width: 340px;
    display: flex; align-items: center; gap: 8px;
    background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.08);
    border-radius: 10px; padding: 7px 13px;
    color: #9CA3AF; font-size: 13px;
  }
  .sh-content {
    flex: 1; overflow-y: auto;
    background: #0F1A14;
  }
  .sh-content::-webkit-scrollbar { width: 6px; }
  .sh-content::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.08); border-radius: 999px; }
  .sh-inner {
    min-height: 100%;
    padding: 24px 26px 40px;
  }
  @keyframes sh-pulse { 0%,100%{opacity:1}50%{opacity:.5} }
  .sh-live { animation: sh-pulse 2s infinite; }
`;

function NavItemEl({ item, pathname }: { item: NavItem; pathname: string }) {
  const [flyoutOpen, setFlyoutOpen] = useState(false);
  const [flyoutTop, setFlyoutTop] = useState(0);
  const ref = useRef<HTMLDivElement>(null);
  const isActive = pathname === item.href || (item.href && pathname.startsWith(item.href) && item.href !== '/');
  const hasChildren = !!item.children?.length;

  const handleEnter = () => {
    if (hasChildren && ref.current) {
      setFlyoutTop(ref.current.getBoundingClientRect().top);
      setFlyoutOpen(true);
    }
  };

  return (
    <div ref={ref} className="sh-group" onMouseEnter={handleEnter} onMouseLeave={() => setFlyoutOpen(false)}>
      {hasChildren ? (
        <div className={`sh-item ${isActive ? 'active' : ''}`} style={{ justifyContent: 'space-between' }}>
          <item.icon size={14} style={{ flexShrink: 0 }} />
          <span className="sh-item-label">{item.label}</span>
          <ChevronRight size={11} style={{ flexShrink: 0, opacity: .4 }} />
        </div>
      ) : (
        <a href={item.href} className={`sh-item ${isActive ? 'active' : ''}`}>
          <item.icon size={14} style={{ flexShrink: 0 }} />
          <span className="sh-item-label">{item.label}</span>
          {item.badge && <span className="sh-badge">{item.badge}</span>}
        </a>
      )}
      {flyoutOpen && hasChildren && (
        <div className="sh-flyout" style={{ top: flyoutTop }}>
          {item.children!.map(child => (
            <a key={child.href} href={child.href} className="sh-flyout-item">
              {child.label}
              {child.badge && <span className="sh-badge" style={{ marginLeft: 6 }}>{child.badge}</span>}
            </a>
          ))}
        </div>
      )}
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  const isPublic = PUBLIC_ROUTES.some(r => pathname.startsWith(r));
  if (isPublic) return <>{children}</>;

  const initials = user?.email
    ? user.email.substring(0, 2).toUpperCase()
    : 'AQ';

  return (
    <>
      <style>{SHELL_CSS}</style>
      <div className="sh-wrap">
        {/* Sidebar */}
        <nav className="sh-sidebar">
          <div className="sh-logo">
            <div className="sh-logo-icon">D</div>
            <div>
              <div className="sh-logo-text">DIAX CRM</div>
              <div className="sh-logo-sub">Pro · v5</div>
            </div>
          </div>

          <div className="sh-nav">
            {navGroups.map(group => (
              <div key={group.section}>
                <div className="sh-section">{group.section}</div>
                {group.items.map(item => (
                  <NavItemEl key={item.label} item={item} pathname={pathname} />
                ))}
              </div>
            ))}
          </div>

          <div className="sh-user">
            <div className="sh-avatar">{initials}</div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: '#F9FAFB', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user?.email?.split('@')[0] ?? 'Admin'}
              </div>
              <div style={{ fontSize: 10, color: '#6B7280' }}>Admin</div>
            </div>
            <div className="sh-live" style={{ width: 7, height: 7, borderRadius: '50%', background: '#10B981', flexShrink: 0 }} />
          </div>
        </nav>

        {/* Main */}
        <div className="sh-main">
          {/* Topbar */}
          <div className="sh-topbar">
            <div className="sh-search">
              <Search size={13} />
              <span>Buscar…</span>
              <span style={{ marginLeft: 'auto', fontSize: 11, opacity: .4 }}>⌘K</span>
            </div>
            <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 12 }}>
              <a href="/leads/" style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '6px 13px', borderRadius: 9, background: '#10B981', color: '#fff', fontSize: 12, fontWeight: 600, textDecoration: 'none' }}>
                <Plus size={13} /> Novo Lead
              </a>
              <Bell size={16} color="#6B7280" style={{ cursor: 'pointer' }} />
              <button
                onClick={logout}
                title="Sair"
                style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'flex', alignItems: 'center', color: '#6B7280' }}
              >
                <LogOut size={16} />
              </button>
            </div>
          </div>

          {/* Page content */}
          <div className="sh-content">
            <div className="sh-inner">
              {children}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
