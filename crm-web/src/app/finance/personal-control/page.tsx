'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { getEffectivePayDay } from '@/lib/date-utils';
import { buildSalaryBuckets, buildRemanejamentos, liquidCashBalance } from '@/lib/salary-planner';
import { cn, formatCurrency } from '@/lib/utils';
import { financeService, AccountType, type CreditCard, type FinancialAccount } from '@/services/finance';
import {
  usePersonalControlMonth,
  useInvestiqSummary,
  useCreditCards,
  useFinancialAccounts,
  personalControlKeys,
  financeKeys,
} from '@/hooks/finance';
import { useQueryClient } from '@tanstack/react-query';
import {
  CreatePersonalControlExpenseRequest,
  CreatePersonalControlIncomeRequest,
  CreatePersonalControlSubscriptionRequest,
  InvestIQPortfolioSummary,
  InvoiceTransactionItem,
  LinkedSubscriptionPreview,
  PersonalControlBillingFrequency,
  PersonalControlExpenseItem,
  PersonalControlIncomeItem,
  PersonalControlInvoiceDueThisMonth,
  PersonalControlMonthView,
  PersonalControlPaymentType,
  PersonalControlSubscriptionItem,
  TogglePersonalControlStatusRequest,
  personalControlService,
} from '@/services/personalControlService';
import {
  ArrowDown,
  ArrowDownCircle,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  ArrowUpCircle,
  Banknote,
  BarChart3,
  Building2,
  Calendar,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  CreditCard as CreditCardIcon,
  Download,
  FileText,
  PencilLine,
  Plus,
  Repeat,
  RotateCcw,
  Sparkles,
  TrendingDown,
  TrendingUp,
  Trash2,
  Upload,
  Wallet,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useRef, useState, useTransition } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { toast } from 'sonner';

type EditingState<T> = T & { editingId: string | null };

const months = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
];

const paymentTypeOptions: { value: PersonalControlPaymentType; label: string }[] = [
  { value: 'debit', label: 'Débito' },
  { value: 'credit', label: 'Crédito' },
];

const billingFrequencyOptions: { value: PersonalControlBillingFrequency; label: string }[] = [
  { value: 'weekly', label: 'Semanal' },
  { value: 'monthly', label: 'Mensal' },
  { value: 'quarterly', label: 'Trimestral' },
  { value: 'yearly', label: 'Anual' },
];

function currentPeriod() {
  const now = new Date();
  // Default to previous month during the first 5 days so users can finalize last month
  if (now.getDate() <= 5) {
    const prev = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    return { year: prev.getFullYear(), month: prev.getMonth() + 1 };
  }
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
}

function monthTitle(year: number, month: number) {
  return `${months[month - 1]} de ${year}`;
}

function incomeFormReset(): EditingState<CreatePersonalControlIncomeRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    dayOfMonth: new Date().getDate(),
    isRecurring: true,
    isPaid: true,
    paymentDate: undefined,
    details: '',
    editingId: null,
  };
}

function expenseFormReset(): EditingState<CreatePersonalControlExpenseRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    paymentType: 'debit',
    dueDay: new Date().getDate(),
    isPaid: false,
    paymentDate: undefined,
    creditCardId: '',
    details: '',
    hasVariableAmount: false,
    editingId: null,
  };
}

function subscriptionFormReset(): EditingState<CreatePersonalControlSubscriptionRequest> {
  const now = currentPeriod();
  return {
    year: now.year,
    month: now.month,
    name: '',
    amount: 0,
    billingFrequency: 'monthly',
    paymentType: 'credit',
    isPaid: false,
    paymentDate: undefined,
    creditCardId: '',
    details: '',
    hasVariableAmount: false,
    editingId: null,
  };
}

function StatusBadge({ paid, onClick, loading }: { paid: boolean; onClick?: () => void; loading?: boolean }) {
  const interactive = !!onClick;
  const base = 'select-none transition-opacity ' +
    (interactive ? 'cursor-pointer focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-emerald-500 ' : '') +
    (loading ? 'opacity-50 pointer-events-none' : '');

  const handleKeyDown = interactive
    ? (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick?.();
        }
      }
    : undefined;

  const a11y = interactive
    ? {
        role: 'button' as const,
        tabIndex: 0,
        'aria-pressed': paid,
        'aria-busy': loading || undefined,
        'aria-label': paid ? 'Marcar como pendente' : 'Marcar como pago',
        onKeyDown: handleKeyDown,
      }
    : {};

  return paid ? (
    <Badge onClick={onClick} className={cn('border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100', base)} {...a11y}>
      Pago ✓
    </Badge>
  ) : (
    <Badge onClick={onClick} className={cn('border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100', base)} {...a11y}>
      Pendente
    </Badge>
  );
}

function MetricCard({
  title,
  value,
  description,
  icon: Icon,
  tone,
}: {
  title: string;
  value: string;
  description: string;
  icon: LucideIcon;
  tone: 'green' | 'red' | 'blue' | 'slate';
}) {
  const toneStyles = {
    green: { bg: 'rgba(16,185,129,0.08)', border: 'rgba(16,185,129,0.2)', icon: 'rgba(16,185,129,0.15)', iconColor: '#10B981' },
    red:   { bg: 'rgba(239,68,68,0.08)',  border: 'rgba(239,68,68,0.2)',  icon: 'rgba(239,68,68,0.15)',  iconColor: '#F87171' },
    blue:  { bg: 'rgba(14,165,233,0.08)', border: 'rgba(14,165,233,0.2)', icon: 'rgba(14,165,233,0.15)', iconColor: '#38BDF8' },
    slate: { bg: 'rgba(255,255,255,0.04)', border: 'rgba(255,255,255,0.09)', icon: 'rgba(255,255,255,0.08)', iconColor: '#9CA3AF' },
  };
  const t = toneStyles[tone];

  return (
    <div className="rounded-xl p-5" style={{ background: t.bg, border: `1px solid ${t.border}` }}>
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium" style={{ color: '#9CA3AF' }}>{title}</p>
          <p className="mt-2 text-2xl font-bold tracking-tight" style={{ color: '#F9FAFB' }}>{value}</p>
          <p className="mt-1 text-xs" style={{ color: '#6B7280' }}>{description}</p>
        </div>
        <div className="rounded-xl p-3" style={{ background: t.icon }}>
          <Icon className="h-5 w-5" style={{ color: t.iconColor }} />
        </div>
      </div>
    </div>
  );
}

function SectionShell({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <Card className="shadow-sm">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

function ExpenseSummaryFooter({ expenses }: { expenses: PersonalControlExpenseItem[] }) {
  if (!expenses.length) return null;
  const paidTotal = expenses.filter(e => e.isPaid).reduce((s, e) => s + e.amount, 0);
  const pendingTotal = expenses.filter(e => !e.isPaid).reduce((s, e) => s + e.amount, 0);
  const paidCount = expenses.filter(e => e.isPaid).length;
  const pendingCount = expenses.filter(e => !e.isPaid).length;
  return (
    <div className="mt-3 flex flex-wrap gap-3 border-t pt-3">
      <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2.5">
        <CheckCircle2 className="h-5 w-5 text-emerald-600 shrink-0" />
        <div>
          <p className="text-[11px] font-medium uppercase tracking-wide text-emerald-700">Pago</p>
          <p className="text-lg font-bold text-emerald-700 tabular-nums leading-tight">{formatCurrency(paidTotal)}</p>
          <p className="text-[11px] text-emerald-600">{paidCount} {paidCount === 1 ? 'item' : 'itens'}</p>
        </div>
      </div>
      <div className="flex items-center gap-3 rounded-lg border border-rose-200 bg-rose-50 px-4 py-2.5">
        <Calendar className="h-5 w-5 text-rose-500 shrink-0" />
        <div>
          <p className="text-[11px] font-medium uppercase tracking-wide text-rose-600">Pendente</p>
          <p className="text-lg font-bold text-rose-600 tabular-nums leading-tight">{formatCurrency(pendingTotal)}</p>
          <p className="text-[11px] text-rose-500">{pendingCount} {pendingCount === 1 ? 'item' : 'itens'}</p>
        </div>
      </div>
    </div>
  );
}

const ASSET_CLASS_LABELS: Record<string, string> = {
  acao: 'Ações',
  fii: 'FIIs',
  renda_fixa: 'Renda Fixa',
  bdr: 'BDRs',
  etf: 'ETFs',
};

function InvestIQWidget({ data, loading }: { data: InvestIQPortfolioSummary | null; loading: boolean }) {
  if (loading) {
    return (
      <div className="flex items-center gap-3 rounded-2xl px-5 py-4 text-sm" style={{ background: 'rgba(139,92,246,0.08)', border: '1px solid rgba(139,92,246,0.2)', color: '#9CA3AF' }}>
        <BarChart3 className="h-4 w-4 text-violet-400 animate-pulse" />
        Carregando InvestIQ...
      </div>
    );
  }

  if (!data || data.configured === false) return null;

  const pnl = data.unrealized_pnl + data.realized_pnl;
  const pnlPositive = pnl >= 0;
  const retPct = data.total_return_pct;

  const PILL = { background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.09)', borderRadius: '0.5rem', padding: '0.375rem 0.75rem' };

  return (
    <div className="rounded-2xl px-5 py-4" style={{ background: 'rgba(139,92,246,0.08)', border: '1px solid rgba(139,92,246,0.2)' }}>
      <div className="flex flex-wrap items-center gap-4">
        <div className="flex items-center gap-2 text-sm font-semibold" style={{ color: '#c084fc' }}>
          <BarChart3 className="h-4 w-4" />
          InvestIQ
        </div>
        <div className="h-4 w-px" style={{ background: 'rgba(139,92,246,0.3)' }} />

        {/* Portfolio value */}
        <div className="flex items-center gap-1.5 text-sm" style={PILL}>
          <span style={{ color: '#9CA3AF' }}>Carteira</span>
          <span className="font-bold tabular-nums" style={{ color: '#c084fc' }}>{formatCurrency(data.portfolio_value)}</span>
        </div>

        {/* P&L */}
        <div className="flex items-center gap-1.5 text-sm" style={PILL}>
          {pnlPositive ? (
            <TrendingUp className="h-3.5 w-3.5 text-emerald-400" />
          ) : (
            <TrendingDown className="h-3.5 w-3.5 text-rose-400" />
          )}
          <span style={{ color: '#9CA3AF' }}>P&L</span>
          <span className="font-semibold tabular-nums" style={{ color: pnlPositive ? '#10B981' : '#F87171' }}>
            {pnlPositive ? '+' : ''}{formatCurrency(pnl)}
            {retPct !== null && (
              <span className="ml-1 text-xs opacity-75">({retPct >= 0 ? '+' : ''}{retPct?.toFixed(2)}%)</span>
            )}
          </span>
        </div>

        {/* Dividends last 30d */}
        {data.monthly_dividends > 0 && (
          <div className="flex items-center gap-1.5 text-sm" style={PILL}>
            <span style={{ color: '#9CA3AF' }}>Dividendos/30d</span>
            <span className="font-semibold tabular-nums" style={{ color: '#10B981' }}>+{formatCurrency(data.monthly_dividends)}</span>
          </div>
        )}

        {/* Allocation pills */}
        {data.asset_allocation.map((a) => (
          <div key={a.asset_class} className="flex items-center gap-1 rounded-full px-2.5 py-1 text-xs" style={{ background: 'rgba(99,102,241,0.12)', border: '1px solid rgba(99,102,241,0.25)', color: '#818CF8' }}>
            <span className="font-medium">{ASSET_CLASS_LABELS[a.asset_class] ?? a.asset_class}</span>
            <span className="opacity-70">{a.percentage.toFixed(1)}%</span>
          </div>
        ))}

        <div className="ml-auto flex items-center gap-1 text-[11px]" style={{ color: '#6B7280' }}>
          <span>{data.position_count} posições</span>
        </div>
      </div>
    </div>
  );
}

function PatrimonioTotalCard({
  accountsTotal,
  accountsCount,
  investiqValue,
  investiqConfigured,
  investiqLoading,
}: {
  accountsTotal: number;
  accountsCount: number;
  investiqValue: number | null;
  investiqConfigured: boolean;
  investiqLoading: boolean;
}) {
  if (accountsCount === 0 && !investiqConfigured && !investiqLoading) return null;

  const investiqAmount = investiqConfigured ? (investiqValue ?? 0) : 0;
  const total = accountsTotal + investiqAmount;
  const accountsPct = total > 0 ? Math.round((accountsTotal / total) * 100) : 0;
  const investiqPct = total > 0 ? Math.round((investiqAmount / total) * 100) : 0;

  const BREAKDOWN_PILL = { background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.09)', borderRadius: '0.5rem', padding: '0.5rem 0.75rem' };

  return (
    <div className="rounded-2xl p-5" style={{ background: 'linear-gradient(135deg, rgba(16,185,129,0.1) 0%, rgba(11,21,16,0.8) 50%, rgba(139,92,246,0.1) 100%)', border: '1px solid rgba(16,185,129,0.2)' }}>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-start gap-3">
          <div className="rounded-xl p-2.5" style={{ background: 'linear-gradient(135deg, #10B981, #8B5CF6)', color: '#fff' }}>
            <Wallet className="h-5 w-5" />
          </div>
          <div>
            <p className="text-xs font-medium uppercase tracking-wide" style={{ color: '#9CA3AF' }}>Patrimônio Total</p>
            <p className="text-3xl font-semibold tracking-tight" style={{ color: '#F9FAFB' }}>{formatCurrency(total)}</p>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <div className="text-xs" style={BREAKDOWN_PILL}>
            <div className="flex items-center gap-1.5" style={{ color: '#9CA3AF' }}>
              <Building2 className="h-3.5 w-3.5" />
              <span>Contas</span>
              {accountsPct > 0 && <span className="font-medium" style={{ color: '#D1D5DB' }}>{accountsPct}%</span>}
            </div>
            <p className="font-semibold tabular-nums" style={{ color: '#F9FAFB' }}>{formatCurrency(accountsTotal)}</p>
          </div>
          <div className="text-xs" style={BREAKDOWN_PILL}>
            <div className="flex items-center gap-1.5" style={{ color: '#9CA3AF' }}>
              <BarChart3 className="h-3.5 w-3.5" />
              <span>InvestIQ</span>
              {investiqConfigured && investiqPct > 0 && <span className="font-medium" style={{ color: '#D1D5DB' }}>{investiqPct}%</span>}
            </div>
            <p className="font-semibold tabular-nums" style={{ color: '#F9FAFB' }}>
              {investiqLoading ? '—' : investiqConfigured ? formatCurrency(investiqAmount) : 'não configurado'}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

function PatrimonioWidget({
  accounts,
  onUpdateBalance,
}: {
  accounts: FinancialAccount[];
  onUpdateBalance: (id: string, balance: number) => Promise<void>;
}) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [saving, setSaving] = useState(false);

  if (accounts.length === 0) return null;
  const total = accounts.reduce((sum, a) => sum + a.balance, 0);

  const startEdit = (a: FinancialAccount) => {
    setEditingId(a.id);
    setDraft(String(a.balance));
  };

  const cancelEdit = () => {
    setEditingId(null);
    setDraft('');
  };

  const saveEdit = async (id: string) => {
    const parsed = parseFloat(draft.replace(',', '.'));
    if (isNaN(parsed)) { toast.error('Valor inválido.'); return; }
    setSaving(true);
    try {
      await onUpdateBalance(id, parsed);
    } catch (err) {
      console.error('saveEdit error:', err);
      toast.error('Erro ao salvar saldo: ' + (err instanceof Error ? err.message : String(err)));
    } finally {
      setSaving(false);
      setEditingId(null);
    }
  };

  const ACCT_PILL = { background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.09)', borderRadius: '0.5rem', padding: '0.375rem 0.75rem' };

  return (
    <div className="flex flex-wrap items-center gap-3 rounded-2xl px-5 py-4" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
      <div className="flex items-center gap-2 text-sm font-semibold" style={{ color: '#D1D5DB' }}>
        <Building2 className="h-4 w-4" style={{ color: '#9CA3AF' }} />
        Patrimônio
      </div>
      <div className="h-4 w-px" style={{ background: 'rgba(255,255,255,0.12)' }} />
      {accounts.map((a) => (
        <div key={a.id} className="flex items-center gap-1.5 text-sm" style={ACCT_PILL}>
          <span style={{ color: '#9CA3AF' }}>{a.name}</span>
          {editingId === a.id ? (
            <div className="flex items-center gap-1">
              <Input
                className="h-6 w-28 text-xs px-1.5"
                value={draft}
                autoFocus
                onChange={(e) => setDraft(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') void saveEdit(a.id);
                  if (e.key === 'Escape') cancelEdit();
                }}
              />
              <button
                onClick={() => void saveEdit(a.id)}
                disabled={saving}
                className="text-emerald-600 hover:text-emerald-700 text-xs font-medium disabled:opacity-50"
              >
                ✓
              </button>
              <button onClick={cancelEdit} className="text-slate-400 hover:text-slate-600 text-xs">✕</button>
            </div>
          ) : (
            <button
              onClick={() => startEdit(a)}
              className={cn(
                'font-semibold tabular-nums hover:underline cursor-pointer',
                a.balance >= 0 ? 'text-emerald-700' : 'text-rose-600'
              )}
            >
              {formatCurrency(a.balance)}
            </button>
          )}
        </div>
      ))}
      <div className="ml-auto flex items-center gap-1.5 rounded-xl px-4 py-1.5 text-sm" style={{ background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)' }}>
        <span style={{ color: '#9CA3AF' }}>Total</span>
        <span className={cn('font-bold tabular-nums', total >= 0 ? 'text-emerald-400' : 'text-rose-400')}>
          {formatCurrency(total)}
        </span>
      </div>
    </div>
  );
}

function matchSub(description: string, subs: LinkedSubscriptionPreview[]): LinkedSubscriptionPreview | null {
  const lower = description.toLowerCase();
  return subs.find((s) => {
    const sl = s.description.toLowerCase();
    return lower.includes(sl) || sl.includes(lower);
  }) ?? null;
}

function InvoiceBucketRow({
  invoice,
}: {
  invoice: PersonalControlInvoiceDueThisMonth & { _pending?: boolean };
}) {
  const [expanded, setExpanded] = useState(false);
  const displayAmount = invoice.statementAmount ?? invoice.totalTransactionsAmount;
  const dueDay = new Date(invoice.dueDate).getUTCDate();
  const hasRealTxs = invoice.transactions.length > 0;
  const hasLinked = invoice.linkedSubscriptions.length > 0;
  const canExpand = hasRealTxs || hasLinked;

  // Conciliation: find which templates were not matched by any real transaction
  const matchedTemplateIds = hasRealTxs
    ? new Set(
        invoice.transactions
          .map((tx) => matchSub(tx.description, invoice.linkedSubscriptions)?.templateId)
          .filter(Boolean) as string[]
      )
    : new Set<string>();
  const unmatchedTemplates = invoice.linkedSubscriptions.filter((ls) => !matchedTemplateIds.has(ls.templateId));

  return (
    <div style={{
      borderRadius: '0.5rem',
      border: invoice._pending ? '1px solid rgba(251,191,36,0.5)' : invoice.isPaid ? '1px solid rgba(255,255,255,0.08)' : '1px solid rgba(96,165,250,0.3)',
      background: invoice._pending ? 'rgba(217,119,6,0.15)' : invoice.isPaid ? 'rgba(255,255,255,0.04)' : 'rgba(59,130,246,0.1)',
      fontSize: '0.75rem',
    }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', padding: '0.375rem 0.75rem' }}>
        {invoice._pending && <span style={{ color: '#FCD34D', fontWeight: 700 }}>⚠ PENDENTE —</span>}
        <CreditCardIcon style={{ width: '0.75rem', height: '0.75rem', color: '#60A5FA', flexShrink: 0 }} />
        <span style={{ fontWeight: 600, flex: 1, color: invoice.isPaid ? '#6B7280' : '#F1F5F9', textDecoration: invoice.isPaid ? 'line-through' : 'none' }}>
          Fatura {invoice.creditCardGroupName}
        </span>
        <span style={{ color: '#94A3B8', fontVariantNumeric: 'tabular-nums' }}>dia {dueDay}</span>
        <span style={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums', color: invoice.isPaid ? '#6B7280' : invoice._pending ? '#FCD34D' : '#93C5FD', textDecoration: invoice.isPaid ? 'line-through' : 'none' }}>
          {formatCurrency(displayAmount)}
        </span>
        {invoice.statementAmount == null && invoice.totalTransactionsAmount > 0 && (
          <span style={{ borderRadius: '0.25rem', background: 'rgba(217,119,6,0.3)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 600, color: '#FCD34D' }}>estimado</span>
        )}
        {invoice.isPaid && <span style={{ borderRadius: '0.25rem', background: 'rgba(16,185,129,0.25)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#6EE7B7' }}>pago</span>}
        {canExpand && (
          <button onClick={() => setExpanded((v) => !v)} style={{ marginLeft: '0.25rem', color: '#64748B' }} aria-label={expanded ? 'Recolher' : 'Expandir'}>
            {expanded ? <ChevronDown style={{ width: '0.75rem', height: '0.75rem' }} /> : <ChevronRight style={{ width: '0.75rem', height: '0.75rem' }} />}
          </button>
        )}
      </div>

      {expanded && canExpand && (
        <div style={{ borderTop: '1px solid rgba(255,255,255,0.08)', padding: '0.5rem 0.75rem', display: 'flex', flexDirection: 'column', gap: '0.375rem' }}>
          {hasRealTxs ? (
            <>
              <span style={{ fontSize: '0.625rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#94A3B8', display: 'block', marginBottom: '0.125rem' }}>
                Transações reais ({invoice.transactions.length})
              </span>
              {invoice.transactions.map((tx) => {
                const matched = matchSub(tx.description, invoice.linkedSubscriptions);
                return (
                  <div key={tx.transactionId} style={{ display: 'flex', alignItems: 'center', gap: '0.375rem', fontSize: '0.6875rem' }}>
                    <span style={{ flex: 1, color: '#CBD5E1', fontWeight: 500 }}>{tx.description}</span>
                    {matched && (
                      <span style={{ borderRadius: '0.25rem', background: 'rgba(16,185,129,0.25)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#6EE7B7' }}>✓ conciliado</span>
                    )}
                    <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600, color: '#F1F5F9' }}>{formatCurrency(tx.amount)}</span>
                  </div>
                );
              })}
              {unmatchedTemplates.length > 0 && (
                <>
                  <span style={{ fontSize: '0.625rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#FCD34D', marginTop: '0.25rem', display: 'block' }}>
                    Esperado mas não encontrado:
                  </span>
                  {unmatchedTemplates.map((ls) => (
                    <div key={ls.templateId} style={{ display: 'flex', alignItems: 'center', gap: '0.375rem', fontSize: '0.6875rem', color: '#FDE68A', fontWeight: 500 }}>
                      <span style={{ flex: 1 }}>{ls.description}</span>
                      {ls.hasVariableAmount && <span style={{ borderRadius: '0.25rem', background: 'rgba(217,119,6,0.3)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#FCD34D' }}>variável</span>}
                      <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>{formatCurrency(ls.amount)}</span>
                    </div>
                  ))}
                </>
              )}
            </>
          ) : (
            <>
              <span style={{ fontSize: '0.625rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#60A5FA', display: 'block', marginBottom: '0.125rem' }}>
                Recorrências previstas (sem PDF):
              </span>
              {invoice.linkedSubscriptions.map((ls) => (
                <div key={ls.templateId} style={{ display: 'flex', alignItems: 'center', gap: '0.375rem', fontSize: '0.6875rem', color: '#CBD5E1', fontWeight: 500 }}>
                  <span style={{ flex: 1 }}>{ls.description}</span>
                  {ls.hasVariableAmount && <span style={{ borderRadius: '0.25rem', background: 'rgba(217,119,6,0.3)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#FCD34D' }}>variável</span>}
                  <span style={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600, color: '#F1F5F9' }}>{formatCurrency(ls.amount)}</span>
                </div>
              ))}
            </>
          )}
        </div>
      )}
    </div>
  );
}

function SalaryPlannerSection({
  monthView,
  startingBalance,
}: {
  monthView: PersonalControlMonthView;
  startingBalance: number;
}) {
  const buckets = buildSalaryBuckets(monthView, startingBalance);
  const remanejamentos = buildRemanejamentos(buckets);

  return (
    <Card className="shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-3">
          <div>
            <CardTitle className="text-lg">Planner de Salário</CardTitle>
            <CardDescription>Despesas diretas afetam o caixa; faturas de cartão entram pelo vencimento. Expand da fatura mostra transações reais ou recorrências previstas. <strong>Caixa</strong> = saldo líquido projetado considerando apenas contas em aberto (as pagas já estão no saldo).</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          {buckets.map(({ day, nextDay, incomes, expensesAtVista, debitSubs, invoicesDue, totalIncome, totalCashOut, pendingTotal, periodBalance, runningBalance, investSuggestion }) => (
            <div key={day} style={{
              borderRadius: '1rem',
              border: runningBalance >= 0 ? '1px solid rgba(16,185,129,0.25)' : '1px solid rgba(239,68,68,0.25)',
              background: runningBalance >= 0 ? 'rgba(16,185,129,0.07)' : 'rgba(239,68,68,0.07)',
              padding: '1rem',
            }}>
              {/* Header */}
              <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
                <span style={{ fontSize: '0.875rem', fontWeight: 700, width: '6rem', flexShrink: 0, color: runningBalance >= 0 ? '#6EE7B7' : '#FCA5A5' }}>
                  Dia {day}{nextDay < 32 ? `–${nextDay - 1}` : '+'}
                </span>
                {incomes.length > 0 && (
                  <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '0.5rem' }}>
                    <span style={{ fontSize: '0.6875rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#6EE7B7' }}>Entradas:</span>
                    {incomes.map((item) => (
                      <span key={item.id} style={{ borderRadius: '0.375rem', background: 'rgba(16,185,129,0.25)', border: '1px solid rgba(16,185,129,0.4)', padding: '0.25rem 0.625rem', fontSize: '0.75rem', fontWeight: 600, color: '#6EE7B7' }}>
                        {item.name} {formatCurrency(item.amount)}
                      </span>
                    ))}
                  </div>
                )}
                <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.75rem', fontSize: '0.75rem' }}>
                  <span style={{ color: '#94A3B8' }}>Receitas <strong style={{ color: '#6EE7B7' }}>{formatCurrency(totalIncome)}</strong></span>
                  <span style={{ color: '#94A3B8' }}>Saídas <strong style={{ color: '#FCA5A5' }}>{formatCurrency(totalCashOut)}</strong></span>
                  <span style={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums', color: periodBalance >= 0 ? '#6EE7B7' : '#FCA5A5' }}>
                    Período: {periodBalance >= 0 ? '+' : ''}{formatCurrency(periodBalance)}
                  </span>
                  <span style={{ fontWeight: 800, fontVariantNumeric: 'tabular-nums', fontSize: '0.875rem', borderLeft: `2px solid ${runningBalance >= 0 ? 'rgba(16,185,129,0.4)' : 'rgba(239,68,68,0.4)'}`, paddingLeft: '0.75rem', color: runningBalance >= 0 ? '#34D399' : '#F87171' }}>
                    Caixa: {runningBalance >= 0 ? '+' : ''}{formatCurrency(runningBalance)}
                  </span>
                  {investSuggestion > 0 && (
                    <span style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', borderRadius: '0.5rem', background: 'rgba(56,189,248,0.12)', border: '1px solid rgba(56,189,248,0.25)', padding: '0.25rem 0.5rem', color: '#7DD3FC', fontWeight: 600 }}>
                      <TrendingUp style={{ width: '0.75rem', height: '0.75rem' }} />Investir 20%: <strong>{formatCurrency(investSuggestion)}</strong>
                    </span>
                  )}
                </div>
              </div>

              {/* Layer 1 — Debit subscriptions + direct expenses */}
              {(debitSubs.length > 0 || expensesAtVista.length > 0) && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem', marginTop: '0.25rem' }}>
                  <span style={{ fontSize: '0.6875rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#FCA5A5' }}>Saídas diretas:</span>
                  {debitSubs.map((item) => (
                    <span key={item.id} style={{
                      borderRadius: '0.375rem',
                      padding: '0.375rem 0.625rem',
                      fontSize: '0.75rem',
                      border: item.isPaid ? '1px solid rgba(255,255,255,0.08)' : item._pending ? '1px solid rgba(251,191,36,0.5)' : '1px solid rgba(255,255,255,0.1)',
                      background: item.isPaid ? 'rgba(255,255,255,0.03)' : item._pending ? 'rgba(217,119,6,0.2)' : 'rgba(255,255,255,0.06)',
                      color: item.isPaid ? '#6B7280' : item._pending ? '#FCD34D' : '#E2E8F0',
                      fontWeight: item._pending && !item.isPaid ? 700 : 500,
                      display: 'flex', alignItems: 'center', gap: '0.375rem',
                    }}>
                      <span style={{ textDecoration: item.isPaid ? 'line-through' : 'none' }}>{!item.isPaid && item._pending && '⚠ PENDENTE — '}{item.name}</span>
                      <strong style={{ marginLeft: 'auto', fontVariantNumeric: 'tabular-nums', textDecoration: item.isPaid ? 'line-through' : 'none' }}>{formatCurrency(item.amount)}</strong>
                      {item.isPaid
                        ? <span style={{ borderRadius: '0.25rem', background: 'rgba(16,185,129,0.25)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#6EE7B7' }}>pago</span>
                        : <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.125rem', borderRadius: '0.25rem', background: 'rgba(16,185,129,0.25)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#6EE7B7' }}><Banknote style={{ width: '0.625rem', height: '0.625rem' }} />PIX</span>}
                    </span>
                  ))}
                  {expensesAtVista.map((item) => (
                    <span key={item.id} style={{
                      borderRadius: '0.375rem',
                      padding: '0.375rem 0.625rem',
                      fontSize: '0.75rem',
                      border: item.isPaid ? '1px solid rgba(255,255,255,0.08)' : item._pending ? '1px solid rgba(251,191,36,0.5)' : '1px solid rgba(255,255,255,0.1)',
                      background: item.isPaid ? 'rgba(255,255,255,0.03)' : item._pending ? 'rgba(217,119,6,0.2)' : 'rgba(255,255,255,0.06)',
                      color: item.isPaid ? '#6B7280' : item._pending ? '#FCD34D' : '#E2E8F0',
                      fontWeight: item._pending && !item.isPaid ? 700 : 500,
                      display: 'flex', alignItems: 'center', gap: '0.375rem',
                    }}>
                      <span style={{ textDecoration: item.isPaid ? 'line-through' : 'none' }}>{!item.isPaid && item._pending && '⚠ PENDENTE — '}{item.name}</span>
                      <strong style={{ marginLeft: 'auto', fontVariantNumeric: 'tabular-nums', textDecoration: item.isPaid ? 'line-through' : 'none' }}>{formatCurrency(item.amount)}</strong>
                      {item.isPaid && <span style={{ borderRadius: '0.25rem', background: 'rgba(16,185,129,0.25)', padding: '0.125rem 0.375rem', fontSize: '0.625rem', fontWeight: 700, color: '#6EE7B7' }}>pago</span>}
                    </span>
                  ))}
                </div>
              )}

              {/* Layer 2 — Invoice payments due this month */}
              {invoicesDue.length > 0 && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.375rem', marginTop: '0.75rem', paddingTop: '0.625rem', borderTop: '1px solid rgba(255,255,255,0.08)' }}>
                  <span style={{ fontSize: '0.6875rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#93C5FD' }}>Faturas (vencimento):</span>
                  {invoicesDue.map((inv) => (
                    <InvoiceBucketRow key={inv.invoiceId} invoice={inv} />
                  ))}
                </div>
              )}

              {pendingTotal > 0 && (
                <div style={{ marginTop: '0.5rem', fontSize: '0.6875rem', fontWeight: 700, color: '#FCD34D', background: 'rgba(217,119,6,0.15)', border: '1px solid rgba(251,191,36,0.3)', borderRadius: '0.375rem', padding: '0.375rem 0.625rem' }}>
                  ⚠ Total não coberto: {formatCurrency(pendingTotal)} — será pago vencido ou com o próximo salário.
                </div>
              )}
            </div>
          ))}
        </div>

        {remanejamentos.length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', marginTop: '0.5rem' }}>
            <span style={{ fontSize: '0.6875rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.05em', color: '#FCD34D' }}>
              Sugestões de remanejamento
            </span>
            {remanejamentos.map((r) => (
              <div key={r.day} style={{ borderRadius: '0.5rem', border: '1px solid rgba(251,191,36,0.3)', background: 'rgba(217,119,6,0.12)', padding: '0.625rem 0.75rem' }}>
                <div style={{ fontSize: '0.75rem', fontWeight: 700, color: '#FCD34D', marginBottom: '0.125rem' }}>{r.title}</div>
                <div style={{ fontSize: '0.6875rem', color: '#FDE68A', fontWeight: 500, lineHeight: 1.4 }}>{r.message}</div>
              </div>
            ))}
          </div>
        )}

      </CardContent>
    </Card>
  );
}

// ─── Sort helpers ────────────────────────────────────────────────────────────
type SortConfig = { key: string; dir: 'asc' | 'desc' } | null;

function nextSort(current: SortConfig, key: string): SortConfig {
  if (current?.key !== key) return { key, dir: 'asc' };
  return current.dir === 'asc' ? { key, dir: 'desc' } : null;
}

function sortedBy<T>(
  items: T[],
  sort: SortConfig,
  getters: Partial<Record<string, (item: T) => string | number | boolean>>,
): T[] {
  if (!sort) return items;
  const get = getters[sort.key];
  if (!get) return items;
  return [...items].sort((a, b) => {
    const va = get(a);
    const vb = get(b);
    let cmp = 0;
    if (typeof va === 'number' && typeof vb === 'number') cmp = va - vb;
    else if (typeof va === 'boolean' && typeof vb === 'boolean') cmp = Number(va) - Number(vb);
    else cmp = String(va).localeCompare(String(vb), 'pt-BR', { sensitivity: 'base' });
    return sort.dir === 'asc' ? cmp : -cmp;
  });
}

function SortHead({ label, sortKey, sort, onSort }: {
  label: string;
  sortKey: string;
  sort: SortConfig;
  onSort: (key: string) => void;
}) {
  const active = sort?.key === sortKey;
  const Icon = active && sort?.dir === 'desc' ? ArrowDown : ArrowUp;
  return (
    <TableHead
      className="cursor-pointer select-none hover:bg-muted/50 whitespace-nowrap"
      onClick={() => onSort(sortKey)}
    >
      <div className="flex items-center gap-1">
        {label}
        <Icon className={cn('h-3 w-3 shrink-0', active ? 'text-primary' : 'opacity-20')} />
      </div>
    </TableHead>
  );
}
// ─────────────────────────────────────────────────────────────────────────────

function Page() {
  const [period, setPeriod] = useState(currentPeriod);
  const [isPending, startTransition] = useTransition();
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [incomeForm, setIncomeForm] = useState(incomeFormReset);
  const [expenseForm, setExpenseForm] = useState(expenseFormReset);
  const [subscriptionForm, setSubscriptionForm] = useState(subscriptionFormReset);
  const [incomeDialogOpen, setIncomeDialogOpen] = useState(false);
  const [expenseDialogOpen, setExpenseDialogOpen] = useState(false);
  const [subscriptionDialogOpen, setSubscriptionDialogOpen] = useState(false);
  const [deleteDialog, setDeleteDialog] = useState<{
    kind: 'income' | 'expense' | 'subscription';
    id: string;
    name: string;
  } | null>(null);
  const [makeRecurringDialog, setMakeRecurringDialog] = useState<{ item: PersonalControlExpenseItem } | null>(null);
  const [recurringIndefinite, setRecurringIndefinite] = useState(true);
  const [recurringMonths, setRecurringMonths] = useState(12);
  const [importingSheet, setImportingSheet] = useState(false);
  const [copyingRecurring, setCopyingRecurring] = useState(false);
  const [editingStatementId, setEditingStatementId] = useState<string | null>(null);
  const [statementDraft, setStatementDraft] = useState('');
  const [payingInvoiceId, setPayingInvoiceId] = useState<string | null>(null);

  // Sort states
  const [incomeSort, setIncomeSort] = useState<SortConfig>(null);
  const [expenseSort, setExpenseSort] = useState<SortConfig>(null);
  const [subscriptionSort, setSubscriptionSort] = useState<SortConfig>(null);
  const toggleIncomeSort = (key: string) => setIncomeSort((prev) => nextSort(prev, key));
  const toggleExpenseSort = (key: string) => setExpenseSort((prev) => nextSort(prev, key));
  const toggleSubscriptionSort = (key: string) => setSubscriptionSort((prev) => nextSort(prev, key));

  // Resizable pane (Receitas / Despesas)
  const [leftPct, setLeftPct] = useState(40);
  const paneContainerRef = useRef<HTMLDivElement>(null);
  const paneDrag = useRef({ active: false, containerLeft: 0, containerWidth: 0 });
  const onDividerMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    const rect = paneContainerRef.current?.getBoundingClientRect();
    if (!rect) return;
    paneDrag.current = { active: true, containerLeft: rect.left, containerWidth: rect.width };
    const onMove = (ev: MouseEvent) => {
      if (!paneDrag.current.active) return;
      const pct = ((ev.clientX - paneDrag.current.containerLeft) / paneDrag.current.containerWidth) * 100;
      setLeftPct(Math.min(Math.max(pct, 20), 75));
    };
    const onUp = () => {
      paneDrag.current.active = false;
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  };

  // Feature 2 — PDF statement import
  type ParsedStatementTx = { description: string; amount: number; date: string; selected: boolean };
  const [pdfImportCard, setPdfImportCard] = useState<{ creditCardId: string; creditCardName: string } | null>(null);
  const [parsedTxList, setParsedTxList] = useState<ParsedStatementTx[]>([]);
  const [pdfParsing, setPdfParsing] = useState(false);
  const [pdfImporting, setPdfImporting] = useState(false);

  const qc = useQueryClient();
  const { data: monthView = null, isLoading: loading } = usePersonalControlMonth(period.year, period.month);
  const { data: creditCards = [] } = useCreditCards();
  const { data: accounts = [] } = useFinancialAccounts();
  const { data: investiq = null, isLoading: investiqLoading } = useInvestiqSummary();

  const refresh = () => {
    void qc.invalidateQueries({ queryKey: personalControlKeys.monthView(period.year, period.month) });
  };

  const changeMonth = (delta: number) => {
    startTransition(() => {
      setPeriod((current) => {
        const next = new Date(current.year, current.month - 1 + delta, 1);
        return { year: next.getFullYear(), month: next.getMonth() + 1 };
      });
    });
  };

  const resetToCurrentMonth = () => {
    startTransition(() => setPeriod(currentPeriod()));
  };

  const saveStatus = async (
    kind: 'income' | 'expense' | 'subscription',
    id: string,
    isPaid: boolean,
  ) => {
      const payload: TogglePersonalControlStatusRequest = {
      year: period.year,
      month: period.month,
      isPaid,
      paymentDate: isPaid ? new Date().toISOString() : undefined,
    };

    setSavingKey(`${kind}-${id}`);
    try {
      if (kind === 'income') {
        await personalControlService.toggleIncomeStatus(id, payload);
      } else if (kind === 'expense') {
        await personalControlService.toggleExpenseStatus(id, payload);
      } else {
        await personalControlService.toggleSubscriptionStatus(id, payload);
      }
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao atualizar status.');
    } finally {
      setSavingKey(null);
    }
  };

  const confirmDelete = async () => {
    if (!deleteDialog) return;

    const { kind, id } = deleteDialog;
    setSavingKey(`delete-${kind}-${id}`);
    try {
      if (kind === 'income') {
        await personalControlService.deleteIncome(id);
      } else if (kind === 'expense') {
        await personalControlService.deleteExpense(id);
      } else {
        await personalControlService.deleteSubscription(id);
      }
      toast.success('Registro excluído.');
      setDeleteDialog(null);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao excluir registro.');
    } finally {
      setSavingKey(null);
    }
  };

  const importFromSheet = async () => {
    setImportingSheet(true);
    try {
      const result = await personalControlService.importFromSheet(period.year, period.month);
      toast.success(`Importado: ${result.matchedCards} cartões correspondidos, ${result.unmatchedCards} sem correspondência.`);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao importar da planilha.');
    } finally {
      setImportingSheet(false);
    }
  };

  const handleCopyRecurring = async () => {
    setCopyingRecurring(true);
    try {
      const result = await personalControlService.copyRecurring(period.year, period.month);
      const createdCount = result.created.length;
      const skippedCount = result.skipped.length;
      const cardSkipped = result.skipped.filter((s) => s.skipReason === 'CreditCardSkipped' || s.skipReason === 'NoInvoiceFound').length;
      const variableAmount = result.created.filter((c) => c.hasVariableAmount);

      if (createdCount === 0 && skippedCount === 0) {
        toast.info('Nenhuma recorrência aplicável a este mês.');
      } else if (createdCount === 0) {
        toast.info(`Nenhuma recorrência criada (${skippedCount} já existem ou não puderam ser geradas).`);
      } else {
        const cardSuffix = cardSkipped > 0 ? ` ${cardSkipped} de cartão precisam ser geradas manualmente.` : '';
        toast.success(`${createdCount} lançamento${createdCount !== 1 ? 's' : ''} gerado${createdCount !== 1 ? 's' : ''} a partir das recorrências.${cardSuffix}`);

        // Variable-amount items (condomínio com taxa extra, salário dolarizado, por dias úteis):
        // surface a separate warning so the user remembers to update the actual value.
        if (variableAmount.length > 0) {
          const sample = variableAmount.slice(0, 3).map((v) => v.description).join(', ');
          const more = variableAmount.length > 3 ? ` e mais ${variableAmount.length - 3}` : '';
          toast.warning(
            `${variableAmount.length} item${variableAmount.length !== 1 ? 's' : ''} com valor variável: ${sample}${more}. Confirme o valor real do mês.`,
            { duration: 8000 },
          );
        }
      }
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao copiar recorrências.');
    } finally {
      setCopyingRecurring(false);
    }
  };

  const handlePdfUpload = async (file: File, card: { creditCardId: string; creditCardName: string }) => {
    setPdfImportCard(card);
    setParsedTxList([]);
    setPdfParsing(true);
    try {
      const result = await personalControlService.parseStatement(file);
      const txs: ParsedStatementTx[] = result.transactions.map((t: { description: string; amount: number; date: string }) => ({
        ...t,
        amount: Math.abs(t.amount),
        selected: t.amount < 0,
      }));
      setParsedTxList(txs);
      if (txs.length === 0) toast.info('Nenhuma transação encontrada no PDF.');
    } catch {
      toast.error('Falha ao analisar o PDF. Verifique se o arquivo não está protegido por senha.');
      setPdfImportCard(null);
    } finally {
      setPdfParsing(false);
    }
  };

  const importSelectedPdfTxs = async () => {
    if (!pdfImportCard) return;
    const selected = parsedTxList.filter((t) => t.selected);
    if (selected.length === 0) { toast.info('Nenhuma transação selecionada.'); return; }
    setPdfImporting(true);
    let ok = 0; let fail = 0;
    for (const tx of selected) {
      try {
        const d = new Date(tx.date + 'T12:00:00Z');
        await personalControlService.createExpense({
          year: period.year,
          month: period.month,
          name: tx.description,
          amount: tx.amount,
          paymentType: 'credit',
          dueDay: d.getUTCDate(),
          isPaid: false,
          creditCardId: pdfImportCard.creditCardId,
        });
        ok++;
      } catch { fail++; }
    }
    refresh();
    setPdfImportCard(null);
    setParsedTxList([]);
    if (fail === 0) toast.success(`${ok} despesa${ok !== 1 ? 's' : ''} importada${ok !== 1 ? 's' : ''} com sucesso.`);
    else toast.warning(`${ok} importadas, ${fail} falhas.`);
    setPdfImporting(false);
  };

  const saveStatementAmount = async (invoiceId: string) => {
    const amount = parseFloat(statementDraft.replace(',', '.'));
    if (isNaN(amount)) {
      toast.error('Valor inválido.');
      return;
    }
    setSavingKey(`statement-${invoiceId}`);
    try {
      await personalControlService.setCardStatementAmount(invoiceId, amount);
      toast.success('Valor da fatura atualizado.');
      setEditingStatementId(null);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao atualizar valor da fatura.');
    } finally {
      setSavingKey(null);
    }
  };

  const payInvoice = async (invoiceId: string, paymentDate: string) => {
    setSavingKey(`pay-invoice-${invoiceId}`);
    try {
      await personalControlService.payCardInvoice(invoiceId, paymentDate);
      toast.success('Fatura marcada como paga.');
      setPayingInvoiceId(null);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao marcar fatura como paga.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitIncome = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('income');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: incomeForm.name,
        amount: Number(incomeForm.amount),
        dayOfMonth: Number(incomeForm.dayOfMonth),
        isRecurring: Boolean(incomeForm.isRecurring),
        isPaid: Boolean(incomeForm.isPaid),
        paymentDate: incomeForm.paymentDate || undefined,
        details: incomeForm.details || undefined,
      };

      if (incomeForm.editingId) {
        await personalControlService.updateIncome(incomeForm.editingId, payload);
        toast.success('Receita atualizada.');
      } else {
        await personalControlService.createIncome(payload);
        toast.success('Receita criada.');
      }
      setIncomeForm(incomeFormReset());
      setIncomeDialogOpen(false);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar receita.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitExpense = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('expense');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: expenseForm.name,
        amount: Number(expenseForm.amount),
        paymentType: expenseForm.paymentType,
        dueDay: Number(expenseForm.dueDay),
        isPaid: Boolean(expenseForm.isPaid),
        paymentDate: expenseForm.paymentDate || undefined,
        details: expenseForm.details || undefined,
        creditCardId: expenseForm.paymentType === 'credit' && expenseForm.creditCardId ? expenseForm.creditCardId : undefined,
      };

      if (expenseForm.editingId) {
        await personalControlService.updateExpense(expenseForm.editingId, payload);
        toast.success('Despesa atualizada.');
      } else {
        await personalControlService.createExpense(payload);
        toast.success('Despesa criada.');
      }
      setExpenseForm(expenseFormReset());
      setExpenseDialogOpen(false);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar despesa.');
    } finally {
      setSavingKey(null);
    }
  };

  const submitSubscription = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSavingKey('subscription');
    try {
      const payload = {
        year: period.year,
        month: period.month,
        name: subscriptionForm.name,
        amount: Number(subscriptionForm.amount),
        billingFrequency: subscriptionForm.billingFrequency,
        paymentType: subscriptionForm.paymentType,
        isPaid: Boolean(subscriptionForm.isPaid),
        paymentDate: subscriptionForm.paymentDate || undefined,
        details: subscriptionForm.details || undefined,
        creditCardId: subscriptionForm.paymentType === 'credit' && subscriptionForm.creditCardId ? subscriptionForm.creditCardId : undefined,
      };

      if (subscriptionForm.editingId) {
        await personalControlService.updateSubscription(subscriptionForm.editingId, payload);
        toast.success('Assinatura atualizada.');
      } else {
        await personalControlService.createSubscription(payload);
        toast.success('Assinatura criada.');
      }
      setSubscriptionForm(subscriptionFormReset());
      setSubscriptionDialogOpen(false);
      refresh();
    } catch (error) {
      console.error(error);
      toast.error('Falha ao salvar assinatura.');
    } finally {
      setSavingKey(null);
    }
  };

  const editIncome = (item: PersonalControlIncomeItem) => {
    setIncomeForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      dayOfMonth: item.dayOfMonth,
      isRecurring: item.isRecurring,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      details: item.details || '',
      editingId: item.id,
    });
    setIncomeDialogOpen(true);
  };

  const editExpense = (item: PersonalControlExpenseItem) => {
    setExpenseForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      paymentType: item.paymentType,
      dueDay: item.dueDay,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      hasVariableAmount: item.hasVariableAmount ?? false,
      editingId: item.id,
    });
    setExpenseDialogOpen(true);
  };

  const editSubscription = (item: PersonalControlSubscriptionItem) => {
    setSubscriptionForm({
      year: period.year,
      month: period.month,
      name: item.name,
      amount: item.amount,
      billingFrequency: item.billingFrequency,
      paymentType: item.paymentType,
      isPaid: item.isPaid,
      paymentDate: item.paymentDate,
      creditCardId: item.creditCardId || '',
      details: item.details || '',
      hasVariableAmount: item.hasVariableAmount ?? false,
      editingId: item.id,
    });
    setSubscriptionDialogOpen(true);
  };

  const openNewIncomeDialog = () => {
    setIncomeForm(incomeFormReset());
    setIncomeDialogOpen(true);
  };

  const openNewExpenseDialog = () => {
    setExpenseForm(expenseFormReset());
    setExpenseDialogOpen(true);
  };

  const openNewSubscriptionDialog = () => {
    setSubscriptionForm(subscriptionFormReset());
    setSubscriptionDialogOpen(true);
  };

  // FASE 2A — update account balance
  const updateAccountBalance = async (id: string, balance: number) => {
    await financeService.updateAccountBalance(id, balance);
    toast.success('Saldo atualizado.');
    refresh();
    void qc.invalidateQueries({ queryKey: financeKeys.accounts() });
  };

  // FASE 2C — quick expense
  type QuickPaymentType = 'pix' | 'debit' | 'credit' | 'auto';
  const [quickExpenseOpen, setQuickExpenseOpen] = useState(false);
  const [quickExpense, setQuickExpense] = useState({
    name: '',
    amount: '' as string | number,
    date: new Date().toISOString().slice(0, 10),
    paymentKind: 'pix' as QuickPaymentType,
    creditCardId: '',
  });

  const submitQuickExpense = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const amount = parseFloat(String(quickExpense.amount).replace(',', '.'));
    if (isNaN(amount) || amount <= 0) { toast.error('Valor inválido.'); return; }
    const date = new Date(quickExpense.date + 'T12:00:00');
    const paymentType = quickExpense.paymentKind === 'credit' ? 'credit' : 'debit';
    setSavingKey('quick-expense');
    try {
      await personalControlService.createExpense({
        year: date.getFullYear(),
        month: date.getMonth() + 1,
        name: quickExpense.name,
        amount,
        paymentType,
        dueDay: date.getDate(),
        isPaid: false,
        creditCardId: paymentType === 'credit' && quickExpense.creditCardId ? quickExpense.creditCardId : undefined,
      });
      toast.success('Despesa criada.');
      setQuickExpenseOpen(false);
      setQuickExpense({ name: '', amount: '', date: new Date().toISOString().slice(0, 10), paymentKind: 'pix', creditCardId: '' });
      refresh();
    } catch (err) {
      console.error(err);
      toast.error('Falha ao criar despesa.');
    } finally {
      setSavingKey(null);
    }
  };

  const summary = monthView?.summary;

  // Sorted data
  const sortedIncomes = sortedBy(monthView?.incomes ?? [], incomeSort, {
    name: (i) => i.name,
    amount: (i) => i.amount,
    dayOfMonth: (i) => i.dayOfMonth,
    isPaid: (i) => i.isPaid,
  });
  const sortedExpenses = sortedBy(monthView?.expenses ?? [], expenseSort, {
    name: (i) => i.name,
    amount: (i) => i.amount,
    paymentType: (i) => i.paymentType,
    dueDay: (i) => i.dueDay,
    card: (i) => i.creditCardName ?? '',
    isPaid: (i) => i.isPaid,
  });
  const sortedSubscriptions = sortedBy(monthView?.subscriptions ?? [], subscriptionSort, {
    name: (i) => i.name,
    amount: (i) => i.amount,
    billingFrequency: (i) => i.billingFrequency,
    paymentType: (i) => i.paymentType,
    isPaid: (i) => i.isPaid,
  });

  return (
    <div className="p-8 max-w-[1700px] mx-auto space-y-8">
      <div className="rounded-3xl border bg-gradient-to-br from-slate-950 via-slate-900 to-slate-800 p-8 text-white shadow-xl">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="space-y-3">
            <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-3 py-1 text-xs font-medium text-white/80">
              <Wallet className="h-3.5 w-3.5" />
              Planilha Financeira
            </div>
            <h1 className="text-3xl font-bold tracking-tight md:text-4xl">Planilha Financeira</h1>
            <p className="max-w-2xl text-sm text-white/70 md:text-base">
              Receitas, despesas, assinaturas e cartões em uma visão mensal unificada, com marcação de
              pago/pendente e resumo consolidado do período.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <Button variant="outline" onClick={() => changeMonth(-1)} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              <ArrowLeft className="h-4 w-4" />
              Mês anterior
            </Button>
            <Button variant="outline" onClick={resetToCurrentMonth} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              <RotateCcw className="h-4 w-4" />
              Mês atual
            </Button>
            <Button variant="outline" onClick={() => changeMonth(1)} className="gap-2 border-white/20 bg-white/10 text-white hover:bg-white/20 hover:text-white">
              Próximo mês
              <ArrowRight className="h-4 w-4" />
            </Button>
            <div className="rounded-xl border border-white/15 bg-white/10 px-4 py-2 text-sm font-medium text-white/90">
              {isPending ? 'Atualizando...' : monthTitle(period.year, period.month)}
            </div>
          </div>
        </div>
      </div>

      <PatrimonioTotalCard
        accountsTotal={accounts.reduce((sum, a) => sum + a.balance, 0)}
        accountsCount={accounts.length}
        investiqValue={investiq?.portfolio_value ?? null}
        investiqConfigured={investiq !== null && investiq.configured !== false}
        investiqLoading={investiqLoading}
      />

      <PatrimonioWidget accounts={accounts} onUpdateBalance={updateAccountBalance} />

      <InvestIQWidget data={investiq} loading={investiqLoading} />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Total de receitas" value={formatCurrency(summary?.totalIncome || 0)} description="Entradas fixas e recorrentes" icon={ArrowUpCircle} tone="green" />
        <MetricCard title="Total de despesas" value={formatCurrency(summary?.totalExpenses || 0)} description="Saídas do mês selecionado" icon={ArrowDownCircle} tone="red" />
        <MetricCard title="Despesas no cartão" value={formatCurrency(summary?.totalCreditExpenses || 0)} description="Lançamentos vinculados ao crédito" icon={CreditCardIcon} tone="blue" />
        <MetricCard title="Saldo restante" value={formatCurrency(summary?.remainingBalance || 0)} description="Resultado consolidado do período" icon={Wallet} tone="slate" />
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard title="Pago" value={formatCurrency(summary?.paidAmount || 0)} description={`${summary?.paidCount || 0} itens quitados`} icon={CheckCircle2} tone="green" />
        <MetricCard title="Pendente" value={formatCurrency(summary?.unpaidAmount || 0)} description={`${summary?.unpaidCount || 0} itens em aberto`} icon={Calendar} tone="red" />
        <MetricCard title="Com cartão" value={formatCurrency(summary?.withCardAmount || 0)} description="Itens com cartão de crédito" icon={CreditCardIcon} tone="blue" />
        <MetricCard title="Sem cartão" value={formatCurrency(summary?.withoutCardAmount || 0)} description="Itens pagos sem cartão" icon={Wallet} tone="slate" />
      </div>

      {/* KPIs de Cartões e Projeção Financeira */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Total faturas cartões"
          value={formatCurrency(summary?.totalCardStatements || 0)}
          description={`${summary?.cardsPendingCount || 0} pendentes · ${summary?.cardsPaidCount || 0} pagas`}
          icon={CreditCardIcon}
          tone="blue"
        />
        <MetricCard
          title="Faturas pendentes"
          value={formatCurrency(summary?.totalCardPending || 0)}
          description="Cartões ainda não pagos"
          icon={Calendar}
          tone="red"
        />
        <MetricCard
          title="Total a pagar"
          value={formatCurrency(summary?.totalToPay || 0)}
          description="Débitos + faturas pendentes"
          icon={ArrowDownCircle}
          tone="red"
        />
        <MetricCard
          title="Disponível p/ investir"
          value={formatCurrency(summary?.availableToInvest || 0)}
          description="Receita − despesas − cartões"
          icon={Wallet}
          tone={(summary?.availableToInvest ?? 0) > 0 ? 'green' : 'red'}
        />
      </div>

      {monthView && (
        <SalaryPlannerSection monthView={monthView} startingBalance={liquidCashBalance(accounts)} />
      )}

      {monthView
        && monthView.incomes.length === 0
        && monthView.expenses.length === 0
        && monthView.subscriptions.length === 0
        && (
          <div className="rounded-lg border border-violet-200 bg-violet-50 p-4 dark:border-violet-900 dark:bg-violet-950/30">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-start gap-3">
                <Sparkles className="mt-0.5 h-5 w-5 shrink-0 text-violet-600 dark:text-violet-400" />
                <div className="space-y-1">
                  <p className="font-medium text-violet-900 dark:text-violet-100">
                    Mês ainda vazio
                  </p>
                  <p className="text-sm text-violet-800/80 dark:text-violet-200/80">
                    Gere automaticamente as receitas, despesas e assinaturas recorrentes ativas para {monthView.period.label}. Despesas no cartão precisam ser lançadas manualmente.
                  </p>
                </div>
              </div>
              <Button
                onClick={handleCopyRecurring}
                disabled={copyingRecurring}
                className="gap-2 self-start bg-violet-600 hover:bg-violet-700 text-white sm:self-auto"
              >
                <Sparkles className="h-4 w-4" />
                {copyingRecurring ? 'Gerando...' : 'Copiar recorrências'}
              </Button>
            </div>
          </div>
        )}

      <SectionShell title="Ações" description="Abra os formulários só quando precisar criar ou editar um lançamento.">
        <div className="flex flex-wrap gap-3">
          <Button className="gap-2" onClick={openNewIncomeDialog}>
            <Plus className="h-4 w-4" />
            Adicionar receita
          </Button>
          <Button className="gap-2" onClick={() => setQuickExpenseOpen(true)} variant="outline">
            <Plus className="h-4 w-4" />
            Despesa rápida
          </Button>
          <Button className="gap-2" onClick={openNewExpenseDialog}>
            <Plus className="h-4 w-4" />
            Adicionar despesa
          </Button>
          <Button className="gap-2" onClick={openNewSubscriptionDialog}>
            <Plus className="h-4 w-4" />
            Adicionar assinatura
          </Button>
        </div>
      </SectionShell>

      {/* Receitas / Despesas — resizable split pane */}
      <div ref={paneContainerRef} className="flex items-stretch gap-0">
        <div style={{ width: `${leftPct}%`, flexShrink: 0, minWidth: 0 }}>
          <SectionShell title="Receitas do mês" description="Clique no badge de status para marcar como pago/pendente.">
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <SortHead label="Nome" sortKey="name" sort={incomeSort} onSort={toggleIncomeSort} />
                    <SortHead label="Valor" sortKey="amount" sort={incomeSort} onSort={toggleIncomeSort} />
                    <SortHead label="Dia pagamento" sortKey="dayOfMonth" sort={incomeSort} onSort={toggleIncomeSort} />
                    <TableHead>Recorrente</TableHead>
                    <SortHead label="Status" sortKey="isPaid" sort={incomeSort} onSort={toggleIncomeSort} />
                    <TableHead className="text-right">Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : sortedIncomes.length ? sortedIncomes.map((item) => {
                    const pay = getEffectivePayDay(item.dayOfMonth, period.year, period.month);
                    return (
                      <TableRow key={item.id}>
                        <TableCell><div className="space-y-1"><p className="font-medium">{item.name}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                        <TableCell>{formatCurrency(item.amount)}</TableCell>
                        <TableCell>
                          <div className="space-y-0.5">
                            <p className="text-sm font-medium">{pay.label}</p>
                            {pay.adjusted && <p className="text-xs text-amber-600">Ajustado (fim de semana)</p>}
                            {item.paymentDate && (() => {
                              const pd = new Date(item.paymentDate);
                              const actualDay = pd.getUTCDate();
                              if (actualDay !== pay.effectiveDay) {
                                const d = pd.getUTCDate().toString().padStart(2, '0');
                                const m = (pd.getUTCMonth() + 1).toString().padStart(2, '0');
                                return <p className="text-xs text-sky-600 font-medium">Pago em {d}/{m}</p>;
                              }
                              return null;
                            })()}
                          </div>
                        </TableCell>
                        <TableCell>
                          {item.isRecurring
                            ? <Badge variant="outline" className="border-emerald-200 text-emerald-700">Recorrente</Badge>
                            : <Badge variant="outline" className="border-orange-200 bg-orange-50 text-orange-700">Encerrado</Badge>
                          }
                        </TableCell>
                        <TableCell>
                          <StatusBadge
                            paid={item.isPaid}
                            loading={savingKey === `income-${item.id}`}
                            onClick={() => saveStatus('income', item.id, !item.isPaid)}
                          />
                        </TableCell>
                        <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editIncome(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'income', id: item.id, name: item.name })} disabled={savingKey === `delete-income-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                      </TableRow>
                    );
                  }) : <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Nenhuma receita encontrada.</TableCell></TableRow>}
                </TableBody>
              </Table>
            </div>
          </SectionShell>
        </div>

        {/* Drag handle */}
        <div
          className="w-3 flex-shrink-0 cursor-col-resize flex items-stretch justify-center group"
          onMouseDown={onDividerMouseDown}
          title="Arrastar para redimensionar"
        >
          <div className="w-0.5 my-6 rounded-full bg-border group-hover:bg-primary/60 transition-colors" />
        </div>

        <div style={{ flex: 1, minWidth: 0 }}>
          <SectionShell title="Despesas do mês" description="Débito, crédito, vencimentos e cartão vinculado.">
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <SortHead label="Nome" sortKey="name" sort={expenseSort} onSort={toggleExpenseSort} />
                    <SortHead label="Valor" sortKey="amount" sort={expenseSort} onSort={toggleExpenseSort} />
                    <SortHead label="Tipo" sortKey="paymentType" sort={expenseSort} onSort={toggleExpenseSort} />
                    <SortHead label="Venc." sortKey="dueDay" sort={expenseSort} onSort={toggleExpenseSort} />
                    <SortHead label="Cartão" sortKey="card" sort={expenseSort} onSort={toggleExpenseSort} />
                    <SortHead label="Status" sortKey="isPaid" sort={expenseSort} onSort={toggleExpenseSort} />
                    <TableHead className="text-right">Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : sortedExpenses.length ? sortedExpenses.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell><div className="space-y-1"><p className="font-medium flex items-center gap-1">{item.name}{item.hasVariableAmount && <span title="Valor variável — verificar mês a mês" className="inline-flex items-center rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">~valor</span>}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                      <TableCell>{formatCurrency(item.amount)}</TableCell>
                      <TableCell>{item.paymentType === 'credit' ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 gap-1 hover:bg-blue-50"><CreditCardIcon className="h-3 w-3" />Crédito</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 gap-1 hover:bg-emerald-50"><Banknote className="h-3 w-3" />PIX/Déb</Badge>}</TableCell>
                      <TableCell>Dia {item.dueDay}</TableCell>
                      <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 hover:bg-blue-50"><CreditCardIcon className="mr-1 h-3 w-3 inline" />{item.creditCardName}</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-50"><Banknote className="mr-1 h-3 w-3 inline" />PIX / Débito</Badge>}</TableCell>
                      <TableCell><StatusBadge paid={item.isPaid} loading={savingKey === `expense-${item.id}`} onClick={() => saveStatus('expense', item.id, !item.isPaid)} /></TableCell>
                      <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" title="Tornar recorrente" onClick={() => setMakeRecurringDialog({ item })}><Repeat className="h-4 w-4 text-blue-500" /></Button><Button variant="ghost" size="icon" onClick={() => editExpense(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'expense', id: item.id, name: item.name })} disabled={savingKey === `delete-expense-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                    </TableRow>
                  )) : <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Nenhuma despesa encontrada.</TableCell></TableRow>}
                </TableBody>
              </Table>
            </div>
            <ExpenseSummaryFooter expenses={monthView?.expenses ?? []} />
          </SectionShell>
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <SectionShell title="Assinaturas" description="Serviços recorrentes consolidados por mês.">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <SortHead label="Nome" sortKey="name" sort={subscriptionSort} onSort={toggleSubscriptionSort} />
                  <SortHead label="Valor" sortKey="amount" sort={subscriptionSort} onSort={toggleSubscriptionSort} />
                  <SortHead label="Frequência" sortKey="billingFrequency" sort={subscriptionSort} onSort={toggleSubscriptionSort} />
                  <SortHead label="Tipo" sortKey="paymentType" sort={subscriptionSort} onSort={toggleSubscriptionSort} />
                  <TableHead>Cartão</TableHead>
                  <SortHead label="Status" sortKey="isPaid" sort={subscriptionSort} onSort={toggleSubscriptionSort} />
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Carregando dados do mês...</TableCell></TableRow> : sortedSubscriptions.length ? sortedSubscriptions.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell><div className="space-y-1"><p className="font-medium flex items-center gap-1">{item.name}{item.hasVariableAmount && <span title="Valor variável — verificar mês a mês" className="inline-flex items-center rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-medium text-amber-700">~valor</span>}</p>{item.details && <p className="text-xs text-muted-foreground">{item.details}</p>}</div></TableCell>
                    <TableCell>{formatCurrency(item.amount)}</TableCell>
                    <TableCell><Badge variant="outline">{billingFrequencyOptions.find((option) => option.value === item.billingFrequency)?.label || item.billingFrequency}</Badge></TableCell>
                    <TableCell>{item.paymentType === 'credit' ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 gap-1 hover:bg-blue-50"><CreditCardIcon className="h-3 w-3" />Crédito</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 gap-1 hover:bg-emerald-50"><Banknote className="h-3 w-3" />PIX/Déb</Badge>}</TableCell>
                    <TableCell>{item.creditCardName ? <Badge className="bg-blue-50 text-blue-700 border border-blue-200 hover:bg-blue-50"><CreditCardIcon className="mr-1 h-3 w-3 inline" />{item.creditCardName}</Badge> : <Badge className="bg-emerald-50 text-emerald-700 border border-emerald-200 hover:bg-emerald-50"><Banknote className="mr-1 h-3 w-3 inline" />PIX / Débito</Badge>}</TableCell>
                    <TableCell><StatusBadge paid={item.isPaid} loading={savingKey === `subscription-${item.id}`} onClick={() => saveStatus('subscription', item.id, !item.isPaid)} /></TableCell>
                    <TableCell><div className="flex justify-end gap-2"><Button variant="ghost" size="icon" onClick={() => editSubscription(item)}><PencilLine className="h-4 w-4" /></Button><Button variant="ghost" size="icon" onClick={() => setDeleteDialog({ kind: 'subscription', id: item.id, name: item.name })} disabled={savingKey === `delete-subscription-${item.id}`}><Trash2 className="h-4 w-4" /></Button></div></TableCell>
                  </TableRow>
                )) : <TableRow><TableCell colSpan={7} className="py-12 text-center text-muted-foreground">Nenhuma assinatura encontrada.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        </SectionShell>

        <Card className="shadow-sm">
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between gap-2">
              <div>
                <CardTitle className="text-lg">Cartões do mês</CardTitle>
                <CardDescription>Agregação mensal das despesas por cartão.</CardDescription>
              </div>
              <Button
                variant="outline"
                size="sm"
                className="gap-2"
                onClick={() => void importFromSheet()}
                disabled={importingSheet}
              >
                <Download className="h-4 w-4" />
                {importingSheet ? 'Importando...' : 'Importar da planilha'}
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Cartão</TableHead>
                    <TableHead>Fatura</TableHead>
                    <TableHead>Limite</TableHead>
                    <TableHead>Disponível</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Itens</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {loading ? (
                    <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Carregando cartões...</TableCell></TableRow>
                  ) : monthView?.cardSummaries.length ? monthView.cardSummaries.map((item) => (
                    <TableRow key={item.creditCardId}>
                      <TableCell className="font-medium">{item.creditCardName}</TableCell>
                      <TableCell>
                        {editingStatementId === item.invoiceId && item.invoiceId ? (
                          <div className="flex items-center gap-2">
                            <Input
                              className="w-28 h-7 text-sm"
                              value={statementDraft}
                              onChange={(e) => setStatementDraft(e.target.value)}
                              placeholder="0,00"
                              onKeyDown={(e) => {
                                if (e.key === 'Enter') void saveStatementAmount(item.invoiceId!);
                                if (e.key === 'Escape') setEditingStatementId(null);
                              }}
                            />
                            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => void saveStatementAmount(item.invoiceId!)} disabled={savingKey === `statement-${item.invoiceId}`}>
                              Salvar
                            </Button>
                            <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => setEditingStatementId(null)}>
                              Cancelar
                            </Button>
                          </div>
                        ) : (
                          <div className="flex items-center gap-2">
                            <span className="tabular-nums">
                              {item.statementAmount != null
                                ? formatCurrency(item.statementAmount)
                                : formatCurrency(item.totalAmount)}
                            </span>
                            {item.invoiceId && (
                              <button
                                className="text-muted-foreground hover:text-foreground transition-colors"
                                onClick={() => {
                                  setEditingStatementId(item.invoiceId!);
                                  setStatementDraft(
                                    item.statementAmount != null
                                      ? String(item.statementAmount)
                                      : String(item.totalAmount)
                                  );
                                }}
                              >
                                <PencilLine className="h-3.5 w-3.5" />
                              </button>
                            )}
                          </div>
                        )}
                      </TableCell>
                      <TableCell className="tabular-nums text-sm">
                        {item.creditLimit ? formatCurrency(item.creditLimit) : <span className="text-muted-foreground text-xs">—</span>}
                      </TableCell>
                      <TableCell>
                        {item.creditLimit ? (
                          <div className="flex flex-col gap-1 min-w-[100px]">
                            <span className={cn('tabular-nums text-sm font-medium', (item.availableCredit ?? 0) >= 0 ? 'text-emerald-700' : 'text-rose-600')}>
                              {formatCurrency(item.availableCredit ?? 0)}
                            </span>
                            <div className="h-1.5 w-full rounded-full bg-slate-100 overflow-hidden">
                              <div
                                className={cn('h-full rounded-full', (item.availableCredit ?? 0) >= 0 ? 'bg-emerald-400' : 'bg-rose-400')}
                                style={{ width: `${Math.min(100, Math.max(0, ((item.creditLimit - (item.availableCredit ?? 0)) / item.creditLimit) * 100))}%` }}
                              />
                            </div>
                            <span className="text-[10px] text-muted-foreground">
                              {Math.round(((item.creditLimit - (item.availableCredit ?? 0)) / item.creditLimit) * 100)}% usado
                            </span>
                          </div>
                        ) : <span className="text-muted-foreground text-xs">—</span>}
                      </TableCell>
                      <TableCell>
                        {item.invoicePaid ? (
                          <Badge className="border-emerald-200 bg-emerald-50 text-emerald-700">Pago ✓</Badge>
                        ) : item.invoiceId ? (
                          payingInvoiceId === item.invoiceId ? (
                            <div className="flex items-center gap-2">
                              <Input
                                type="date"
                                className="w-36 h-7 text-sm"
                                defaultValue={new Date().toISOString().slice(0, 10)}
                                id={`pay-date-${item.invoiceId}`}
                              />
                              <Button size="sm" className="h-7 px-2" onClick={() => {
                                const input = document.getElementById(`pay-date-${item.invoiceId}`) as HTMLInputElement;
                                const date = input?.value ? new Date(input.value).toISOString() : new Date().toISOString();
                                void payInvoice(item.invoiceId!, date);
                              }} disabled={savingKey === `pay-invoice-${item.invoiceId}`}>
                                Confirmar
                              </Button>
                              <Button size="sm" variant="ghost" className="h-7 px-2" onClick={() => setPayingInvoiceId(null)}>
                                Cancelar
                              </Button>
                            </div>
                          ) : (
                            <Button
                              size="sm"
                              variant="outline"
                              className="h-7 text-xs"
                              onClick={() => setPayingInvoiceId(item.invoiceId!)}
                            >
                              Marcar Pago
                            </Button>
                          )
                        ) : (
                          <Badge variant="outline" className="text-muted-foreground">Sem fatura</Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">{item.itemCount} lançamentos</Badge>
                          <label className="cursor-pointer" title="Importar fatura PDF com IA">
                            <input
                              type="file"
                              accept=".pdf"
                              className="sr-only"
                              onChange={(e) => {
                                const file = e.target.files?.[0];
                                if (file) void handlePdfUpload(file, { creditCardId: item.creditCardId, creditCardName: item.creditCardName });
                                e.target.value = '';
                              }}
                            />
                            <Button size="sm" variant="ghost" className="h-7 px-2 pointer-events-none" asChild>
                              <span><Upload className="h-3.5 w-3.5" /></span>
                            </Button>
                          </label>
                        </div>
                      </TableCell>
                    </TableRow>
                  )) : (
                    <TableRow><TableCell colSpan={6} className="py-12 text-center text-muted-foreground">Nenhum cartão com lançamentos neste mês.</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Feature 2 — PDF Statement Import Modal */}
      <Dialog open={!!pdfImportCard && !pdfParsing} onOpenChange={(open) => { if (!open) { setPdfImportCard(null); setParsedTxList([]); } }}>
        <DialogContent className="sm:max-w-2xl max-h-[80vh] flex flex-col">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-blue-600" />
              Importar fatura — {pdfImportCard?.creditCardName}
            </DialogTitle>
            <DialogDescription>
              Selecione as transações que deseja importar como despesas de {months[period.month - 1]} {period.year}.
            </DialogDescription>
          </DialogHeader>
          <div className="flex-1 overflow-y-auto">
            {parsedTxList.length === 0 ? (
              <p className="text-sm text-muted-foreground py-8 text-center">Nenhuma transação encontrada.</p>
            ) : (
              <div className="space-y-1">
                <div className="flex items-center gap-2 pb-2 border-b">
                  <input
                    type="checkbox"
                    className="rounded border-gray-300"
                    checked={parsedTxList.every((t) => t.selected)}
                    onChange={(e) => setParsedTxList((prev) => prev.map((t) => ({ ...t, selected: e.target.checked })))}
                  />
                  <span className="text-xs text-muted-foreground font-medium">Selecionar todas ({parsedTxList.filter((t) => t.selected).length}/{parsedTxList.length})</span>
                </div>
                {parsedTxList.map((tx, i) => (
                  <label key={i} className="flex items-center gap-3 py-2 px-1 rounded hover:bg-muted/50 cursor-pointer">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 shrink-0"
                      checked={tx.selected}
                      onChange={(e) => setParsedTxList((prev) => prev.map((t, idx) => idx === i ? { ...t, selected: e.target.checked } : t))}
                    />
                    <span className="flex-1 text-sm truncate">{tx.description}</span>
                    <span className="text-sm tabular-nums text-rose-600 font-medium">{formatCurrency(tx.amount)}</span>
                    <span className="text-xs text-muted-foreground shrink-0">{tx.date}</span>
                  </label>
                ))}
              </div>
            )}
          </div>
          <DialogFooter className="gap-2">
            <Button variant="outline" onClick={() => { setPdfImportCard(null); setParsedTxList([]); }}>Cancelar</Button>
            <Button
              onClick={() => void importSelectedPdfTxs()}
              disabled={pdfImporting || parsedTxList.filter((t) => t.selected).length === 0}
            >
              {pdfImporting ? 'Importando...' : `Importar ${parsedTxList.filter((t) => t.selected).length} despesa${parsedTxList.filter((t) => t.selected).length !== 1 ? 's' : ''}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* PDF Parsing Loading Overlay */}
      <Dialog open={pdfParsing} onOpenChange={() => {}}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 text-blue-600 animate-pulse" />
              Analisando PDF com IA...
            </DialogTitle>
            <DialogDescription>Aguarde enquanto extraímos as transações da fatura.</DialogDescription>
          </DialogHeader>
        </DialogContent>
      </Dialog>

      {/* FASE 2C — Despesa Rápida */}
      <Dialog open={quickExpenseOpen} onOpenChange={setQuickExpenseOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Nova Despesa Rápida</DialogTitle>
            <DialogDescription>Lance uma despesa sem precisar selecionar o período.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitQuickExpense} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="qe-name">Descrição</Label>
              <Input
                id="qe-name"
                placeholder="Ex: Mercado, Uber, Farmácia..."
                value={quickExpense.name}
                onChange={(e) => setQuickExpense((c) => ({ ...c, name: e.target.value }))}
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="qe-amount">Valor (R$)</Label>
                <Input
                  id="qe-amount"
                  placeholder="0,00"
                  value={quickExpense.amount}
                  onChange={(e) => setQuickExpense((c) => ({ ...c, amount: e.target.value }))}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="qe-date">Data</Label>
                <Input
                  id="qe-date"
                  type="date"
                  value={quickExpense.date}
                  onChange={(e) => setQuickExpense((c) => ({ ...c, date: e.target.value }))}
                  required
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label>Tipo de pagamento</Label>
              <div className="grid grid-cols-2 gap-2">
                {([
                  { value: 'pix', label: 'PIX' },
                  { value: 'debit', label: 'Débito em Conta' },
                  { value: 'credit', label: 'Cartão de Crédito' },
                  { value: 'auto', label: 'Débito Automático' },
                ] as { value: QuickPaymentType; label: string }[]).map(({ value, label }) => (
                  <button
                    key={value}
                    type="button"
                    onClick={() => setQuickExpense((c) => ({ ...c, paymentKind: value }))}
                    className={cn(
                      'rounded-lg border px-3 py-2 text-sm font-medium text-left transition-colors',
                      quickExpense.paymentKind === value
                        ? 'border-slate-900 bg-slate-900 text-white'
                        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
            {quickExpense.paymentKind === 'credit' && (
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select
                  value={quickExpense.creditCardId || 'none'}
                  onValueChange={(v) => setQuickExpense((c) => ({ ...c, creditCardId: v === 'none' ? '' : v }))}
                >
                  <SelectTrigger><SelectValue placeholder="Selecionar cartão" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem cartão</SelectItem>
                    {creditCards.map((card) => (
                      <SelectItem key={card.id} value={card.id}>{card.name} •{card.lastFourDigits}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <DialogFooter>
              <Button type="button" variant="ghost" onClick={() => setQuickExpenseOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={savingKey === 'quick-expense'} className="gap-2">
                <Plus className="h-4 w-4" />
                {savingKey === 'quick-expense' ? 'Criando...' : 'Criar despesa'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={incomeDialogOpen} onOpenChange={setIncomeDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{incomeForm.editingId ? 'Editar receita' : 'Nova receita'}</DialogTitle>
            <DialogDescription>Receitas fixas ou recorrentes do mês.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitIncome} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="income-name">Nome</Label><Input id="income-name" value={incomeForm.name} onChange={(event) => setIncomeForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="income-amount">Valor</Label><Input id="income-amount" type="number" min="0" step="0.01" value={incomeForm.amount} onChange={(event) => setIncomeForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2"><Label htmlFor="income-day">Dia previsto</Label><Input id="income-day" type="number" min="1" max="31" value={incomeForm.dayOfMonth} onChange={(event) => setIncomeForm((current) => ({ ...current, dayOfMonth: Number(event.target.value) }))} /></div>
            </div>
            <div className="space-y-2"><Label htmlFor="income-details">Detalhes / Observações</Label><Input id="income-details" value={incomeForm.details || ''} onChange={(event) => setIncomeForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <div className="space-y-3">
              <label className="flex cursor-pointer items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm">
                <input type="checkbox" checked={Boolean(incomeForm.isPaid)} onChange={(event) => setIncomeForm((current) => ({ ...current, isPaid: event.target.checked, paymentDate: event.target.checked ? (current.paymentDate || new Date().toISOString().split('T')[0] + 'T00:00:00Z') : undefined }))} />
                Pago
              </label>
              {incomeForm.isPaid && (
                <div className="space-y-1 pl-1">
                  <Label htmlFor="income-payment-date">Data do pagamento</Label>
                  <Input
                    id="income-payment-date"
                    type="date"
                    value={incomeForm.paymentDate ? incomeForm.paymentDate.split('T')[0] : new Date().toISOString().split('T')[0]}
                    onChange={(event) => setIncomeForm((current) => ({ ...current, paymentDate: event.target.value ? event.target.value + 'T00:00:00Z' : undefined }))}
                  />
                  <p className="text-xs text-muted-foreground">Preencha se pago em data diferente do dia previsto (ex: pagamento antecipado).</p>
                </div>
              )}
            </div>
            <label className="flex cursor-pointer items-center gap-3 rounded-xl border bg-white px-3 py-2 text-sm">
              <input type="checkbox" checked={Boolean(incomeForm.isRecurring)} onChange={(event) => setIncomeForm((current) => ({ ...current, isRecurring: event.target.checked }))} />
              <span>Receita recorrente</span>
              {!incomeForm.isRecurring && <span className="ml-auto text-xs font-medium text-orange-600">Contrato encerrado</span>}
            </label>
            <DialogFooter>
              {incomeForm.editingId ? <Button type="button" variant="outline" onClick={() => setIncomeForm(incomeFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'income'}><Plus className="h-4 w-4" />{savingKey === 'income' ? 'Salvando...' : incomeForm.editingId ? 'Atualizar receita' : 'Criar receita'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={expenseDialogOpen} onOpenChange={setExpenseDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{expenseForm.editingId ? 'Editar despesa' : 'Nova despesa'}</DialogTitle>
            <DialogDescription>Controle de débito, crédito e vencimentos.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitExpense} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="expense-name">Nome</Label><Input id="expense-name" value={expenseForm.name} onChange={(event) => setExpenseForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="expense-amount">Valor</Label><Input id="expense-amount" type="number" min="0" step="0.01" value={expenseForm.amount} onChange={(event) => setExpenseForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2"><Label htmlFor="expense-due">Dia do vencimento</Label><Input id="expense-due" type="number" min="1" max="31" value={expenseForm.dueDay} onChange={(event) => setExpenseForm((current) => ({ ...current, dueDay: Number(event.target.value) }))} /></div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Pagamento</Label>
                <Select value={expenseForm.paymentType} onValueChange={(value) => setExpenseForm((current) => ({ ...current, paymentType: value as PersonalControlPaymentType }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{paymentTypeOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select value={expenseForm.paymentType === 'credit' ? expenseForm.creditCardId || 'none' : 'none'} onValueChange={(value) => setExpenseForm((current) => ({ ...current, creditCardId: value === 'none' ? '' : value }))}>
                  <SelectTrigger><SelectValue placeholder="Sem vínculo" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem vínculo</SelectItem>
                    {creditCards.map((card) => <SelectItem key={card.id} value={card.id}>{card.name} • {card.lastFourDigits}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label htmlFor="expense-details">Detalhes</Label><Input id="expense-details" value={expenseForm.details || ''} onChange={(event) => setExpenseForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-2 cursor-pointer select-none text-sm">
              <input type="checkbox" className="rounded border-gray-300" checked={!!expenseForm.hasVariableAmount} onChange={(e) => setExpenseForm((c) => ({ ...c, hasVariableAmount: e.target.checked }))} />
              <span className="text-muted-foreground">Valor variável mês a mês <span className="text-amber-600 font-medium">(ex: plano de saúde, condomínio)</span></span>
            </label>
            <DialogFooter>
              {expenseForm.editingId ? <Button type="button" variant="outline" onClick={() => setExpenseForm(expenseFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'expense'}><Plus className="h-4 w-4" />{savingKey === 'expense' ? 'Salvando...' : expenseForm.editingId ? 'Atualizar despesa' : 'Criar despesa'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={subscriptionDialogOpen} onOpenChange={setSubscriptionDialogOpen}>
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>{subscriptionForm.editingId ? 'Editar assinatura' : 'Nova assinatura'}</DialogTitle>
            <DialogDescription>Serviços recorrentes e mensalidades.</DialogDescription>
          </DialogHeader>
          <form onSubmit={submitSubscription} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="subscription-name">Nome</Label><Input id="subscription-name" value={subscriptionForm.name} onChange={(event) => setSubscriptionForm((current) => ({ ...current, name: event.target.value }))} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label htmlFor="subscription-amount">Valor</Label><Input id="subscription-amount" type="number" min="0" step="0.01" value={subscriptionForm.amount} onChange={(event) => setSubscriptionForm((current) => ({ ...current, amount: Number(event.target.value) }))} /></div>
              <div className="space-y-2">
                <Label>Frequência</Label>
                <Select value={subscriptionForm.billingFrequency} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, billingFrequency: value as PersonalControlBillingFrequency }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{billingFrequencyOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>Pagamento</Label>
                <Select value={subscriptionForm.paymentType} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, paymentType: value as PersonalControlPaymentType }))}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>{paymentTypeOptions.map((option) => <SelectItem key={option.value} value={option.value}>{option.label}</SelectItem>)}</SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label>Cartão</Label>
                <Select value={subscriptionForm.paymentType === 'credit' ? subscriptionForm.creditCardId || 'none' : 'none'} onValueChange={(value) => setSubscriptionForm((current) => ({ ...current, creditCardId: value === 'none' ? '' : value }))}>
                  <SelectTrigger><SelectValue placeholder="Sem vínculo" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="none">Sem vínculo</SelectItem>
                    {creditCards.map((card) => <SelectItem key={card.id} value={card.id}>{card.name} • {card.lastFourDigits}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label htmlFor="subscription-details">Detalhes</Label><Input id="subscription-details" value={subscriptionForm.details || ''} onChange={(event) => setSubscriptionForm((current) => ({ ...current, details: event.target.value }))} /></div>
            <label className="flex items-center gap-2 cursor-pointer select-none text-sm">
              <input type="checkbox" className="rounded border-gray-300" checked={!!subscriptionForm.hasVariableAmount} onChange={(e) => setSubscriptionForm((c) => ({ ...c, hasVariableAmount: e.target.checked }))} />
              <span className="text-muted-foreground">Valor variável mês a mês <span className="text-amber-600 font-medium">(ex: plano de saúde, condomínio)</span></span>
            </label>
            <DialogFooter>
              {subscriptionForm.editingId ? <Button type="button" variant="outline" onClick={() => setSubscriptionForm(subscriptionFormReset())}>Limpar</Button> : null}
              <Button type="submit" className="gap-2" disabled={savingKey === 'subscription'}><Plus className="h-4 w-4" />{savingKey === 'subscription' ? 'Salvando...' : subscriptionForm.editingId ? 'Atualizar assinatura' : 'Criar assinatura'}</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={Boolean(deleteDialog)} onOpenChange={(open) => !open && setDeleteDialog(null)}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Excluir registro</DialogTitle>
            <DialogDescription>
              {deleteDialog ? `Tem certeza que deseja excluir "${deleteDialog.name}"? Essa ação não pode ser desfeita.` : ''}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setDeleteDialog(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant="destructive"
              onClick={confirmDelete}
              disabled={deleteDialog ? savingKey === `delete-${deleteDialog.kind}-${deleteDialog.id}` : false}
            >
              {deleteDialog && savingKey === `delete-${deleteDialog.kind}-${deleteDialog.id}` ? 'Excluindo...' : 'Excluir'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={Boolean(makeRecurringDialog)} onOpenChange={(open) => { if (!open) { setMakeRecurringDialog(null); setRecurringIndefinite(true); setRecurringMonths(12); } }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Tornar Recorrente</DialogTitle>
            <DialogDescription>
              {makeRecurringDialog ? `${makeRecurringDialog.item.name} — R$ ${makeRecurringDialog.item.amount.toFixed(2).replace('.', ',')}` : ''}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-2">
            <div className="flex gap-2">
              <Button type="button" variant={recurringIndefinite ? 'default' : 'outline'} size="sm" onClick={() => setRecurringIndefinite(true)}>Indefinido</Button>
              <Button type="button" variant={!recurringIndefinite ? 'default' : 'outline'} size="sm" onClick={() => setRecurringIndefinite(false)}>Definir meses</Button>
            </div>
            {!recurringIndefinite && (
              <div className="space-y-2">
                <Label htmlFor="recurring-months">Quantidade de meses</Label>
                <Input id="recurring-months" type="number" min={1} max={120} value={recurringMonths} onChange={(e) => setRecurringMonths(Math.max(1, parseInt(e.target.value) || 1))} />
                {(() => {
                  const now = new Date();
                  const end = new Date(now.getFullYear(), now.getMonth() + recurringMonths, 1);
                  const monthNames = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro'];
                  return <p className="text-sm text-muted-foreground">Encerra em {monthNames[end.getMonth()]}/{end.getFullYear()}</p>;
                })()}
              </div>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => { setMakeRecurringDialog(null); setRecurringIndefinite(true); setRecurringMonths(12); }}>Cancelar</Button>
            <Button
              type="button"
              disabled={savingKey === `make-recurring-${makeRecurringDialog?.item.id}`}
              onClick={async () => {
                if (!makeRecurringDialog) return;
                setSavingKey(`make-recurring-${makeRecurringDialog.item.id}`);
                try {
                  await personalControlService.makeExpenseRecurring(makeRecurringDialog.item.id, recurringIndefinite ? null : recurringMonths);
                  void qc.invalidateQueries({ queryKey: personalControlKeys.monthView(period.year, period.month) });
                  toast.success('Despesa marcada como recorrente.');
                  setMakeRecurringDialog(null);
                  setRecurringIndefinite(true);
                  setRecurringMonths(12);
                } catch (err) {
                  toast.error('Falha ao tornar recorrente: ' + (err instanceof Error ? err.message : String(err)));
                } finally {
                  setSavingKey(null);
                }
              }}
            >
              {savingKey === `make-recurring-${makeRecurringDialog?.item.id}` ? 'Salvando...' : 'Confirmar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

export default function PersonalControlPage() {
  return <Page />;
}
