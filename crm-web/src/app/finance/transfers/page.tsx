'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { formatDisplayDate } from '@/lib/date-utils';
import { formatCurrency } from '@/lib/utils';
import {
    AccountTransfer,
    CreateAccountTransferRequest,
    FinancialAccount,
    financeService
} from '@/services/finance';
import {
    ArrowRight,
    ArrowRightLeft,
    CalendarDays,
    Landmark,
    Loader2,
    Plus,
    Trash2,
    CheckCircle2,
    AlertCircle,
    Wallet
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { EmptyState } from '@/components/dashboard/EmptyState';

function DeleteModal({ isOpen, onClose, onConfirm, loading }: {
  isOpen: boolean; onClose: () => void; onConfirm: () => void; loading: boolean;
}) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-md p-4">
      <motion.div 
        initial={{ opacity: 0, scale: 0.95 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.95 }}
        className="p-6 rounded-2xl max-w-md w-full border border-zinc-800 bg-[#0a130f]/95 shadow-2xl relative overflow-hidden"
      >
        <div className="absolute inset-0 bg-gradient-to-br from-rose-500/5 to-transparent pointer-events-none" />
        
        <h3 className="text-lg font-bold mb-2 text-zinc-100 flex items-center gap-2 relative z-10">
          <AlertCircle className="h-5 w-5 text-rose-400" />
          Confirmar Exclusão
        </h3>
        <p className="text-zinc-400 text-sm mb-6 relative z-10 leading-relaxed">
          Tem certeza que deseja excluir esta transferência? O saldo de ambas as contas será revertido automaticamente.
        </p>
        
        <div className="flex justify-end gap-3 relative z-10">
          <Button 
            variant="outline" 
            onClick={onClose} 
            disabled={loading} 
            className="border-zinc-800 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
          >
            Cancelar
          </Button>
          <Button 
            onClick={onConfirm} 
            disabled={loading} 
            variant="destructive" 
            className="bg-rose-600 hover:bg-rose-700 text-white rounded-xl gap-2 font-semibold"
          >
            {loading && <Loader2 className="h-4 w-4 animate-spin" />}
            Excluir Transferência
          </Button>
        </div>
      </motion.div>
    </div>
  );
}

function NewTransferModal({ isOpen, onClose, onConfirm, accounts, loading }: {
  isOpen: boolean; onClose: () => void; onConfirm: (data: CreateAccountTransferRequest) => void;
  accounts: FinancialAccount[]; loading: boolean;
}) {
  const [form, setForm] = useState<CreateAccountTransferRequest>({
    fromFinancialAccountId: '', toFinancialAccountId: '',
    amount: 0, date: new Date().toISOString().split('T')[0], description: ''
  });

  if (!isOpen) return null;

  const activeAccounts = accounts.filter(a => a.isActive);
  const canSubmit = form.fromFinancialAccountId && form.toFinancialAccountId
    && form.fromFinancialAccountId !== form.toFinancialAccountId
    && form.amount > 0 && form.description.trim();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-md p-4">
      <motion.div 
        initial={{ opacity: 0, y: 15 }}
        animate={{ opacity: 1, y: 0 }}
        className="p-6 rounded-2xl max-w-lg w-full border border-zinc-800 bg-[#0a130f]/95 shadow-2xl relative overflow-hidden"
      >
        <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />

        <h3 className="text-lg font-bold mb-6 text-zinc-100 flex items-center gap-2 relative z-10">
          <ArrowRightLeft className="h-5 w-5 text-emerald-400" />
          Nova Transferência
        </h3>
        
        <div className="space-y-4 relative z-10">
          <div>
            <Label className="text-zinc-300 font-medium text-xs mb-1 block">Conta Origem</Label>
            <select
              className="w-full bg-zinc-950 border border-zinc-800 text-zinc-100 rounded-xl px-3 py-2 text-sm focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] outline-none"
              value={form.fromFinancialAccountId}
              onChange={e => setForm(prev => ({ ...prev, fromFinancialAccountId: e.target.value }))}
            >
              <option value="" className="bg-zinc-900">Selecione a conta...</option>
              {activeAccounts.map(a => (
                <option key={a.id} value={a.id} className="bg-zinc-900">{a.name} ({formatCurrency(a.balance)})</option>
              ))}
            </select>
          </div>
          
          <div>
            <Label className="text-zinc-300 font-medium text-xs mb-1 block">Conta Destino</Label>
            <select
              className="w-full bg-zinc-950 border border-zinc-800 text-zinc-100 rounded-xl px-3 py-2 text-sm focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] outline-none"
              value={form.toFinancialAccountId}
              onChange={e => setForm(prev => ({ ...prev, toFinancialAccountId: e.target.value }))}
            >
              <option value="" className="bg-zinc-900">Selecione a conta...</option>
              {activeAccounts.filter(a => a.id !== form.fromFinancialAccountId).map(a => (
                <option key={a.id} value={a.id} className="bg-zinc-900">{a.name} ({formatCurrency(a.balance)})</option>
              ))}
            </select>
          </div>
          
          <div className="grid grid-cols-2 gap-4">
            <div>
              <Label className="text-zinc-300 font-medium text-xs mb-1 block">Valor</Label>
              <div className="relative">
                <span className="absolute left-3 top-2 text-xs text-zinc-500 font-bold">R$</span>
                <Input
                  type="number"
                  min="0.01"
                  step="0.01"
                  placeholder="0,00"
                  className="bg-zinc-950 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 pl-9 text-sm"
                  value={form.amount || ''}
                  onChange={e => setForm(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
                />
              </div>
            </div>
            <div>
              <Label className="text-zinc-300 font-medium text-xs mb-1 block">Data</Label>
              <Input
                type="date"
                className="bg-zinc-950 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
                value={form.date}
                onChange={e => setForm(prev => ({ ...prev, date: e.target.value }))}
              />
            </div>
          </div>
          
          <div>
            <Label className="text-zinc-300 font-medium text-xs mb-1 block">Descrição</Label>
            <Input
              type="text"
              className="bg-zinc-950 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
              placeholder="Ex: Transferência para reserva, Ajuste de caixa..."
              value={form.description}
              onChange={e => setForm(prev => ({ ...prev, description: e.target.value }))}
            />
          </div>
        </div>
        
        <div className="flex justify-end gap-3 mt-6 relative z-10">
          <Button 
            variant="outline" 
            onClick={onClose} 
            disabled={loading}
            className="border-zinc-800 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
          >
            Cancelar
          </Button>
          <Button 
            onClick={() => onConfirm(form)} 
            disabled={loading || !canSubmit}
            className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold rounded-xl gap-2"
          >
            {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowRightLeft className="h-4 w-4" />}
            Confirmar Transferência
          </Button>
        </div>
      </motion.div>
    </div>
  );
}

export default function TransfersPage() {
  const [transfers, setTransfers] = useState<AccountTransfer[]>([]);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [showNewModal, setShowNewModal] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [transfersData, accountsData] = await Promise.all([
        financeService.getAccountTransfers(),
        financeService.getFinancialAccounts()
      ]);
      setTransfers(transfersData.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()));
      setAccounts(accountsData);
    } catch (err) {
      console.error('Erro ao carregar transferências:', err);
      toast.error('Erro ao carregar dados de transferências.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchData(); }, []);

  const handleCreate = async (data: CreateAccountTransferRequest) => {
    setActionLoading(true);
    try {
      await financeService.createAccountTransfer(data);
      toast.success('Transferência efetuada com sucesso!');
      setShowNewModal(false);
      fetchData();
    } catch (err) {
      console.error('Erro ao criar transferência:', err);
      toast.error('Erro ao efetuar transferência bancária.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setActionLoading(true);
    try {
      await financeService.deleteAccountTransfer(deleteTarget);
      toast.success('Transferência estornada com sucesso!');
      setDeleteTarget(null);
      fetchData();
    } catch (err) {
      console.error('Erro ao excluir transferência:', err);
      toast.error('Erro ao estornar transferência.');
    } finally {
      setActionLoading(false);
    }
  };

  const totalTransferred = transfers.reduce((sum, t) => sum + t.amount, 0);
  const activeAccountsCount = accounts.filter(a => a.isActive).length;

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      <FinanceNav />

      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
            <ArrowRightLeft className="h-6 w-6 text-emerald-400" />
            Transferências entre Contas
          </h1>
          <p className="text-zinc-400 text-sm mt-1">
            Movimente recursos entre contas próprias — não afetam receitas ou despesas consolidadas.
          </p>
        </div>
        <Button onClick={() => setShowNewModal(true)} className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 gap-2 rounded-xl">
          <Plus className="h-4 w-4" />
          Nova Transferência
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, delay: 0 }}
          className="rounded-2xl p-5 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
          <p className="text-xs font-semibold uppercase tracking-wider text-zinc-400">Total Movimentado</p>
          <p className="mt-2 text-2xl font-extrabold tracking-tight text-[#00D4AA] tabular-nums">{formatCurrency(totalTransferred)}</p>
          <p className="text-[10px] text-zinc-500 mt-1">Soma de todas as transferências</p>
        </motion.div>
        
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, delay: 0.05 }}
          className="rounded-2xl p-5 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
          <p className="text-xs font-semibold uppercase tracking-wider text-zinc-400">Total de Operações</p>
          <p className="mt-2 text-2xl font-extrabold tracking-tight text-zinc-100 tabular-nums">{transfers.length}</p>
          <p className="text-[10px] text-zinc-500 mt-1">Transferências registradas no período</p>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3, delay: 0.1 }}
          className="rounded-2xl p-5 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
          <p className="text-xs font-semibold uppercase tracking-wider text-zinc-400">Contas Ativas</p>
          <p className="mt-2 text-2xl font-extrabold tracking-tight text-zinc-100 tabular-nums">{activeAccountsCount}</p>
          <p className="text-[10px] text-zinc-500 mt-1">Contas disponíveis para movimentação</p>
        </motion.div>
      </div>

      {/* Transfers List */}
      {loading ? (
        <div className="flex flex-col items-center justify-center py-20 gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
          <p className="text-zinc-400 text-sm animate-pulse">Carregando transferências...</p>
        </div>
      ) : transfers.length === 0 ? (
        <EmptyState
          title="Nenhuma transferência efetuada"
          description="Transfira valores entre suas contas financeiras cadastradas para organizar seus saldos internos de forma visual."
          icon={ArrowRightLeft}
          actionLabel="Criar Primeira Transferência"
          onAction={() => setShowNewModal(true)}
        />
      ) : (
        <div className="space-y-4">
          {transfers.map((transfer, index) => (
            <motion.div
              key={transfer.id}
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.3, delay: index * 0.03 }}
              className="rounded-2xl border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md hover:border-zinc-700/60 hover:shadow-lg transition-all duration-300"
            >
              <CardContent className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-5 gap-4">
                <div className="flex items-center gap-4 flex-1 min-w-0 w-full">
                  <div className="h-10 w-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center flex-shrink-0">
                    <ArrowRightLeft className="h-5 w-5 text-[#00D4AA]" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-semibold text-zinc-100 truncate text-sm leading-normal">{transfer.description}</p>
                    
                    <div className="flex flex-wrap items-center gap-2 mt-2">
                      <span className="px-2 py-0.5 rounded bg-zinc-950 border border-zinc-850 text-zinc-300 text-xs font-semibold leading-none truncate max-w-[150px]">
                        {transfer.fromAccountName}
                      </span>
                      <ArrowRight className="h-3 w-3 text-emerald-400 shrink-0" />
                      <span className="px-2 py-0.5 rounded bg-zinc-950 border border-zinc-850 text-zinc-300 text-xs font-semibold leading-none truncate max-w-[150px]">
                        {transfer.toAccountName}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between sm:justify-end gap-6 w-full sm:w-auto border-t sm:border-t-0 pt-3 sm:pt-0 border-zinc-800/50">
                  <div className="text-left sm:text-right">
                    <p className="text-base font-extrabold text-[#00D4AA] tabular-nums leading-snug">{formatCurrency(transfer.amount)}</p>
                    <div className="flex items-center gap-1 text-[10px] text-zinc-500 font-medium mt-0.5">
                      <CalendarDays className="h-3 w-3" />
                      {formatDisplayDate(transfer.date)}
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="bg-emerald-500/5 text-[#00D4AA] border-emerald-500/20 text-[10px] font-bold py-0.5 rounded-full select-none">
                      Transferência
                    </Badge>
                    
                    <Button
                      variant="ghost"
                      size="icon"
                      className="text-zinc-500 hover:text-rose-400 hover:bg-zinc-900/50 rounded-lg h-8 w-8"
                      onClick={() => setDeleteTarget(transfer.id)}
                    >
                      <Trash2 className="h-4 w-4" />
                      <span className="sr-only">Estornar</span>
                    </Button>
                  </div>
                </div>
              </CardContent>
            </motion.div>
          ))}
        </div>
      )}

      <NewTransferModal
        isOpen={showNewModal}
        onClose={() => setShowNewModal(false)}
        onConfirm={handleCreate}
        accounts={accounts}
        loading={actionLoading}
      />

      <AnimatePresence>
        {deleteTarget && (
          <DeleteModal
            isOpen={!!deleteTarget}
            onClose={() => setDeleteTarget(null)}
            onConfirm={handleDelete}
            loading={actionLoading}
          />
        )}
      </AnimatePresence>
    </div>
  );
}
