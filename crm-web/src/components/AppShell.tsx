'use client';

import { useAuth } from '@/contexts/AuthContext';
import {
  Activity, BarChart2, Bell, Bot, Briefcase, Bug, Calendar,
  ChevronRight, CreditCard, DollarSign, FileText, Globe,
  HelpCircle, LayoutDashboard, Link2, ListChecks, LogOut,
  Mail, Megaphone, Menu, MessageSquare, Package, Plus, Search,
  Settings, Shield, Star, Tag, Target, TrendingUp, Users,
  Wallet, Zap, Cpu, Newspaper, Home
} from 'lucide-react';
import { usePathname } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

type NavChild = { label: string; href: string; badge?: string };
type NavItem = { icon: React.ElementType; label: string; href?: string; children?: NavChild[]; badge?: string };
type NavGroup = { section: string; items: NavItem[] };

const navGroups: NavGroup[] = [
  {
    section: 'Principal',
    items: [
      { icon: LayoutDashboard, label: 'Dashboard', href: '/dashboard' },
      { icon: Newspaper, label: 'Daily Briefings', href: '/daily-briefings', badge: 'NEW' },
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

const PUBLIC_ROUTES = ['/login', '/register', '/forgot-password', '/landing'];

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
    cursor: pointer; transition: color var(--diax-transition-fast), background var(--diax-transition-fast);
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
  .sh-sub-menu {
    display: flex;
    flex-direction: column;
    gap: 2px;
    padding-left: 14px;
    margin-top: 2px;
    margin-bottom: 6px;
    border-left: 1px solid rgba(255,255,255,0.06);
    margin-left: 16px;
  }
  .sh-sub-item {
    display: flex;
    align-items: center;
    padding: 6px 10px;
    border-radius: 7px;
    color: #8E8E99;
    font-size: 12px;
    font-weight: 500;
    text-decoration: none;
    transition: color var(--diax-transition-fast), background var(--diax-transition-fast);
  }
  .sh-sub-item:hover {
    background: rgba(255,255,255,0.04);
    color: #F9FAFB;
  }
  .sh-sub-item.active {
    color: #10B981;
    background: rgba(16,185,129,0.08);
    font-weight: 600;
  }
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
    background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.08);
    border-radius: 10px; padding: 6px 12px;
    color: #9CA3AF; font-size: 12px;
    cursor: pointer;
    transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
  }
  .sh-search:hover {
    background: rgba(255,255,255,0.07);
    border-color: rgba(255, 255, 255, 0.16);
    color: #F9FAFB;
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
  .sh-menu-btn {
    display: none; background: none; border: none; cursor: pointer;
    color: #9CA3AF; padding: 8px; margin-left: -8px; border-radius: 9px;
    align-items: center; justify-content: center; flex-shrink: 0;
  }
  .sh-menu-btn:hover { background: rgba(255,255,255,0.06); color: #F9FAFB; }
  .sh-backdrop { display: none; }
  @media (max-width: 900px) {
    .sh-sidebar {
      position: fixed; left: 0; top: 0; bottom: 0; z-index: 300;
      transform: translateX(-100%); transition: transform .22s ease-out;
      box-shadow: 0 0 60px rgba(0,0,0,.6);
    }
    .sh-sidebar.open { transform: translateX(0); }
    .sh-backdrop {
      display: block; position: fixed; inset: 0; z-index: 250;
      background: rgba(0,0,0,0.55);
      opacity: 0; pointer-events: none; transition: opacity .22s;
    }
    .sh-backdrop.open { opacity: 1; pointer-events: auto; }
    .sh-menu-btn { display: inline-flex; }
    .sh-item { padding: 10px 12px; }
    .sh-topbar { padding: 0 14px; gap: 10px; }
    .sh-search { max-width: none; }
    .sh-search .sh-kbd { display: none; }
    .sh-inner { padding: 16px 14px 36px; }
  }
  @media (prefers-reduced-motion: reduce) {
    .sh-sidebar, .sh-backdrop { transition: none; }
  }
`;

function NavItemEl({ item, pathname }: { item: NavItem; pathname: string }) {
  const hasChildren = !!item.children?.length;
  
  // Check if any of the child hrefs is currently active
  const isChildActive = hasChildren && item.children!.some(c => {
    if (c.href === '/') return pathname === '/';
    return pathname.startsWith(c.href);
  });
  
  const isActive = pathname === item.href || (item.href && pathname.startsWith(item.href) && item.href !== '/');
  
  const [open, setOpen] = useState(isChildActive);

  // Auto-expand if the path segment changes and matches a child
  useEffect(() => {
    if (isChildActive) {
      setOpen(true);
    }
  }, [pathname, isChildActive]);

  return (
    <div className="sh-group">
      {hasChildren ? (
        <>
          <button
            type="button"
            onClick={() => setOpen(!open)}
            className={`sh-item ${isChildActive ? 'active' : ''}`}
            style={{ display: 'flex', width: '100%', border: 'none', background: 'none', textAlign: 'left', cursor: 'pointer', justifyContent: 'space-between', alignItems: 'center' }}
          >
            <div className="flex items-center gap-[9px] overflow-hidden">
              <item.icon size={14} style={{ flexShrink: 0 }} />
              <span className="sh-item-label">{item.label}</span>
            </div>
            <ChevronRight 
              size={12} 
              className="transition-transform duration-200" 
              style={{ flexShrink: 0, opacity: .5, transform: open ? 'rotate(90deg)' : 'none' }} 
            />
          </button>
          {open && (
            <div className="sh-sub-menu">
              {item.children!.map(child => {
                const isSubActive = pathname === child.href || (child.href !== '/' && pathname.startsWith(child.href));
                return (
                  <Link 
                    key={child.href} 
                    href={child.href} 
                    className={`sh-sub-item ${isSubActive ? 'active' : ''}`}
                  >
                    {child.label}
                    {child.badge && <span className="sh-badge" style={{ marginLeft: 6 }}>{child.badge}</span>}
                  </Link>
                );
              })}
            </div>
          )}
        </>
      ) : (
        <Link href={item.href || '#'} className={`sh-item ${isActive ? 'active' : ''}`}>
          <item.icon size={14} style={{ flexShrink: 0 }} />
          <span className="sh-item-label">{item.label}</span>
          {item.badge && <span className="sh-badge">{item.badge}</span>}
        </Link>
      )}
    </div>
  );
}

const ROUTE_LABELS: Record<string, string> = {
  dashboard: 'Dashboard',
  'daily-briefings': 'Daily Briefings',
  customers: 'Clientes',
  leads: 'Leads',
  import: 'Importar',
  helpdesk: 'Helpdesk',
  outreach: 'Outreach',
  'email-marketing': 'Email Marketing',
  pro: 'PRO',
  campanhas: 'Campanhas',
  campaigns: 'Campanhas',
  ads: 'Meta Ads',
  analytics: 'Analytics',
  'google-analytics': 'Google Analytics',
  finance: 'Finanças',
  'morning-briefing': 'Morning Briefing',
  'personal-control': 'Planilha Financeira',
  transactions: 'Transações',
  incomes: 'Receitas',
  expenses: 'Despesas',
  transfers: 'Transferências',
  imports: 'Importar',
  'credit-cards': 'Cartões de Crédito',
  accounts: 'Contas',
  planner: 'Planejador',
  goals: 'Metas',
  recurring: 'Recorrentes',
  'tax-documents': 'Imposto de Renda',
  'ai-chat': 'Claude Chat',
  tools: 'Ferramentas',
  'anthropic-proxy': 'Anthropic Proxy',
  utilities: 'Utilitários',
  'image-generation': 'Geração de Imagens',
  'prompt-generator': 'Gerador de Prompts',
  'humanize-text': 'Humanizar Texto',
  'email-subject-optimizer': 'Otimizador de Email',
  'lead-persona-generator': 'Gerador de Personas',
  'outreach-ab-test': 'Teste A/B',
  'social-batch-generator': 'Batch Social Media',
  'customer-insights': 'Insights',
  agenda: 'Agenda',
  tasks: 'Tarefas',
  household: 'Pessoal',
  checklists: 'Listas e Compras',
  snippets: 'Snippets',
  'html-extractor': 'HTML → Texto',
  'html-url-extractor': 'HTML → Links',
  'apps-inventory': 'Inventário de Apps',
  status: 'Status',
  users: 'Usuários',
  admin: 'Admin',
  groups: 'Grupos',
  ai: 'Provedores IA',
  blog: 'Blog',
  new: 'Novo',
  edit: 'Editar',
  logs: 'Logs do Sistema',
  monitoring: 'Erros',
};

export function AppShell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const [menuOpen, setMenuOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  // Handle Ctrl+K / Cmd+K keyboard shortcut
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setSearchOpen(prev => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  // fecha o drawer ao navegar
  useEffect(() => { setMenuOpen(false); }, [pathname]);

  const isPublic = PUBLIC_ROUTES.some(r => pathname.startsWith(r));
  if (isPublic) return <>{children}</>;

  const initials = user?.email
    ? user.email.substring(0, 2).toUpperCase()
    : 'AQ';

  // Dynamic breadcrumbs calculation
  const pathSegments = pathname.split('/').filter(Boolean);
  const breadcrumbs = pathSegments.map((segment, index) => {
    const href = '/' + pathSegments.slice(0, index + 1).join('/');
    const label = ROUTE_LABELS[segment] || segment.charAt(0).toUpperCase() + segment.slice(1);
    const isLast = index === pathSegments.length - 1;

    return (
      <div key={href} className="flex items-center gap-1">
        <ChevronRight size={10} className="text-slate-600 shrink-0" />
        {isLast ? (
          <span className="text-xs font-semibold text-emerald-400 select-none">{label}</span>
        ) : (
          <Link href={href} className="text-xs text-slate-400 hover:text-slate-200 transition-colors">
            {label}
          </Link>
        )}
      </div>
    );
  });

  const commandItems = [
    { label: 'Ir para Dashboard', href: '/dashboard', category: 'Navegação' },
    { label: 'Ir para Daily Briefings', href: '/daily-briefings', category: 'Navegação' },
    { label: 'Ir para Clientes', href: '/customers', category: 'Navegação' },
    { label: 'Ir para Leads', href: '/leads', category: 'Navegação' },
    { label: 'Ir para Helpdesk', href: '/helpdesk', category: 'Navegação' },
    { label: 'Ir para Outreach', href: '/outreach', category: 'Navegação' },
    { label: 'Ir para Email Marketing', href: '/email-marketing', category: 'Navegação' },
    { label: 'Ir para Planilha Financeira', href: '/finance/personal-control', category: 'Finanças' },
    { label: 'Ir para Transações', href: '/finance/transactions', category: 'Finanças' },
    { label: 'Ir para Claude Chat', href: '/ai-chat', category: 'IA' },
    { label: 'Criar Novo Lead', href: '/leads', category: 'Ações' }
  ];

  const filteredCommands = searchQuery
    ? commandItems.filter(item => item.label.toLowerCase().includes(searchQuery.toLowerCase()))
    : commandItems;

  return (
    <>
      <style>{SHELL_CSS}</style>
      <div className="sh-wrap">
        <div className={`sh-backdrop ${menuOpen ? 'open' : ''}`} onClick={() => setMenuOpen(false)} aria-hidden="true" />
        {/* Sidebar */}
        <nav className={`sh-sidebar ${menuOpen ? 'open' : ''}`}>
          <Link href="/dashboard" className="sh-logo hover:opacity-90 transition-opacity decoration-none">
            <div className="sh-logo-icon">D</div>
            <div>
              <div className="sh-logo-text">DIAX CRM</div>
              <div className="sh-logo-sub">Pro · v5</div>
            </div>
          </Link>

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
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button type="button" className="flex items-center gap-2.5 w-full text-left p-1 rounded-lg hover:bg-white/5 transition-colors focus-visible:ring-1 focus-visible:ring-ring outline-none cursor-pointer">
                  <div className="sh-avatar">{initials}</div>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontSize: 12, fontWeight: 600, color: '#F9FAFB', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {user?.email?.split('@')[0] ?? 'Admin'}
                    </div>
                    <div style={{ fontSize: 10, color: '#6B7280' }}>Admin</div>
                  </div>
                  <div className="sh-live" style={{ width: 7, height: 7, borderRadius: '50%', background: '#10B981', flexShrink: 0 }} />
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" side="right" className="w-56 bg-[#0D1F18] border-white/10 text-slate-200">
                <DropdownMenuLabel>Minha Conta</DropdownMenuLabel>
                <DropdownMenuSeparator className="bg-white/5" />
                <DropdownMenuItem asChild>
                  <Link href="/users" className="flex items-center gap-2 cursor-pointer focus:bg-white/10 w-full">
                    <Users size={14} /> Usuários
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/admin/groups" className="flex items-center gap-2 cursor-pointer focus:bg-white/10 w-full">
                    <Shield size={14} /> Grupos & Permissões
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuItem asChild>
                  <Link href="/tools/status" className="flex items-center gap-2 cursor-pointer focus:bg-white/10 w-full">
                    <Activity size={14} /> Status das Apps
                  </Link>
                </DropdownMenuItem>
                <DropdownMenuSeparator className="bg-white/5" />
                <DropdownMenuItem onClick={logout} className="flex items-center gap-2 cursor-pointer text-red-400 focus:bg-red-950 focus:text-red-300">
                  <LogOut size={14} /> Sair
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </nav>

        {/* Main */}
        <div className="sh-main">
          {/* Topbar */}
          <div className="sh-topbar">
            <button className="sh-menu-btn" onClick={() => setMenuOpen(o => !o)} aria-label="Abrir menu" aria-expanded={menuOpen}>
              <Menu size={19} />
            </button>

            {/* Breadcrumbs */}
            <div className="hidden sm:flex items-center gap-1 select-none">
              <Link href="/dashboard" className="text-slate-400 hover:text-slate-200 transition-colors">
                <Home size={13} />
              </Link>
              {breadcrumbs}
            </div>

            {/* Search Input trigger */}
            <button 
              onClick={() => setSearchOpen(true)}
              className="sh-search text-left focus:outline-none focus:ring-1 focus:ring-ring select-none"
            >
              <Search size={13} />
              <span>Buscar…</span>
              <span className="sh-kbd" style={{ marginLeft: 'auto', fontSize: 11, opacity: .4 }}>⌘K</span>
            </button>

            <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 12 }}>
              <Link href="/leads/" style={{ display: 'flex', alignItems: 'center', gap: 6, padding: '6px 13px', borderRadius: 9, background: '#10B981', color: '#fff', fontSize: 12, fontWeight: 600, textDecoration: 'none' }}>
                <Plus size={13} /> Novo Lead
              </Link>
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

      {/* Command Search Dialog */}
      <Dialog open={searchOpen} onOpenChange={setSearchOpen}>
        <DialogContent className="max-w-md bg-[#0D1F18] border-white/10 text-slate-200 p-0 overflow-hidden">
          <DialogHeader className="p-4 border-b border-white/5">
            <DialogTitle className="text-sm font-semibold text-slate-300">Buscar no CRM</DialogTitle>
          </DialogHeader>
          <div className="p-3">
            <div className="relative flex items-center mb-4">
              <Search className="absolute left-3 h-4 w-4 text-slate-400" />
              <input
                type="text"
                placeholder="Digite para buscar..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full bg-white/5 border border-white/10 rounded-lg pl-9 pr-4 py-2 text-sm focus:outline-none focus:border-emerald-500 text-white placeholder-slate-500"
                autoFocus
              />
            </div>
            <div className="max-h-60 overflow-y-auto space-y-3 pr-1">
              {filteredCommands.length > 0 ? (
                Array.from(new Set(filteredCommands.map(c => c.category))).map(cat => (
                  <div key={cat} className="space-y-1">
                    <div className="text-[10px] font-bold text-slate-500 uppercase tracking-wider px-2">{cat}</div>
                    {filteredCommands.filter(c => c.category === cat).map(cmd => (
                      <Link
                        key={cmd.label}
                        href={cmd.href}
                        onClick={() => setSearchOpen(false)}
                        className="flex items-center justify-between px-2 py-1.5 rounded-md hover:bg-white/5 text-sm transition-colors text-slate-300 hover:text-white"
                      >
                        <span>{cmd.label}</span>
                        <ChevronRight size={12} className="opacity-40" />
                      </Link>
                    ))}
                  </div>
                ))
              ) : (
                <div className="text-center py-6 text-sm text-slate-500">Nenhum resultado encontrado</div>
              )}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
