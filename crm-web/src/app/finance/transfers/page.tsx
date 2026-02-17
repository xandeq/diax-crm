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
    Trash2
} from 'lucide-react';
import { useEffect, useState } from 'react';

function DeleteModal({ isOpen, onClose, onConfirm, loading }: {
  isOpen: boolean; onClose: () => void; onConfirm: () => void; loading: boolean;
}) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white p-8 rounded-2xl shadow-2xl max-w-md w-full mx-4 border border-gray-100">
        <h3 className="text-xl font-bold text-gray-900 mb-2">Confirmar Exclusão</h3>
        <p className="text-gray-600 mb-8">
          Tem certeza que deseja excluir esta transferência? O saldo das contas será revertido.
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={loading} className="px-6 rounded-xl">Cancelar</Button>
          <Button onClick={onConfirm} disabled={loading} variant="destructive" className="px-6 rounded-xl">
            {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
            Excluir
          </Button>
        </div>
      </div>
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white p-8 rounded-2xl shadow-2xl max-w-lg w-full mx-4 border border-gray-100">
        <h3 className="text-xl font-bold text-gray-900 mb-6">Nova Transferência</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Conta Origem</label>
            <select
              className="w-full border rounded-lg px-3 py-2 text-sm"
              value={form.fromFinancialAccountId}
              onChange={e => setForm(prev => ({ ...prev, fromFinancialAccountId: e.target.value }))}
            >
              <option value="">Selecione...</option>
              {activeAccounts.map(a => (
                <option key={a.id} value={a.id}>{a.name} ({formatCurrency(a.balance)})</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Conta Destino</label>
            <select
              className="w-full border rounded-lg px-3 py-2 text-sm"
              value={form.toFinancialAccountId}
              onChange={e => setForm(prev => ({ ...prev, toFinancialAccountId: e.target.value }))}
            >
              <option value="">Selecione...</option>
              {activeAccounts.filter(a => a.id !== form.fromFinancialAccountId).map(a => (
                <option key={a.id} value={a.id}>{a.name} ({formatCurrency(a.balance)})</option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Valor</label>
              <input
                type="number"
                min="0.01"
                step="0.01"
                className="w-full border rounded-lg px-3 py-2 text-sm"
                value={form.amount || ''}
                onChange={e => setForm(prev => ({ ...prev, amount: parseFloat(e.target.value) || 0 }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Data</label>
              <input
                type="date"
                className="w-full border rounded-lg px-3 py-2 text-sm"
                value={form.date}
                onChange={e => setForm(prev => ({ ...prev, date: e.target.value }))}
              />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Descrição</label>
            <input
              type="text"
              className="w-full border rounded-lg px-3 py-2 text-sm"
              placeholder="Ex: Transferência para reserva"
              value={form.description}
              onChange={e => setForm(prev => ({ ...prev, description: e.target.value }))}
            />
          </div>
        </div>
        <div className="flex justify-end gap-3 mt-6">
          <Button variant="ghost" onClick={onClose} disabled={loading}>Cancelar</Button>
          <Button onClick={() => onConfirm(form)} disabled={loading || !canSubmit}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <ArrowRightLeft className="h-4 w-4 mr-2" />}
            Transferir
          </Button>
        </div>
      </div>
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
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchData(); }, []);

  const handleCreate = async (data: CreateAccountTransferRequest) => {
    setActionLoading(true);
    try {
      await financeService.createAccountTransfer(data);
      setShowNewModal(false);
      fetchData();
    } catch (err) {
      console.error('Erro ao criar transferência:', err);
    } finally {
      setActionLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setActionLoading(true);
    try {
      await financeService.deleteAccountTransfer(deleteTarget);
      setDeleteTarget(null);
      fetchData();
    } catch (err) {
      console.error('Erro ao excluir transferência:', err);
    } finally {
      setActionLoading(false);
    }
  };

  const totalTransferred = transfers.reduce((sum, t) => sum + t.amount, 0);

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      <FinanceNav />

      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <ArrowRightLeft className="h-6 w-6 text-purple-600" />
            Transferências entre Contas
          </h1>
          <p className="text-gray-500 text-sm mt-1">
            Movimentações entre contas próprias — não afetam receita nem despesa.
          </p>
        </div>
        <Button onClick={() => setShowNewModal(true)} className="gap-2">
          <Plus className="h-4 w-4" />
          Nova Transferência
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Total Transferido</CardDescription>
            <CardTitle className="text-2xl text-purple-700">{formatCurrency(totalTransferred)}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Transferências</CardDescription>
            <CardTitle className="text-2xl">{transfers.length}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Contas Ativas</CardDescription>
            <CardTitle className="text-2xl">{accounts.filter(a => a.isActive).length}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Transfers List */}
      {loading ? (
        <div className="flex items-center justify-center py-20">
          <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
        </div>
      ) : transfers.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16">
            <ArrowRightLeft className="h-12 w-12 text-gray-300 mb-4" />
            <h3 className="text-lg font-semibold text-gray-500 mb-2">Nenhuma transferência</h3>
            <p className="text-gray-400 text-sm mb-6">Mova dinheiro entre contas sem afetar seu resultado.</p>
            <Button onClick={() => setShowNewModal(true)} variant="outline" className="gap-2">
              <Plus className="h-4 w-4" />
              Criar Primeira Transferência
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {transfers.map(transfer => (
            <Card key={transfer.id} className="hover:shadow-md transition-shadow">
              <CardContent className="flex items-center justify-between p-4">
                <div className="flex items-center gap-4 flex-1">
                  <div className="h-10 w-10 rounded-full bg-purple-100 flex items-center justify-center flex-shrink-0">
                    <ArrowRightLeft className="h-5 w-5 text-purple-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-gray-900 truncate">{transfer.description}</p>
                    <div className="flex items-center gap-2 mt-1 text-sm text-gray-500">
                      <Landmark className="h-3.5 w-3.5" />
                      <span className="font-medium">{transfer.fromAccountName}</span>
                      <ArrowRight className="h-3.5 w-3.5 text-purple-500" />
                      <span className="font-medium">{transfer.toAccountName}</span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-6">
                  <div className="text-right">
                    <p className="text-lg font-bold text-purple-700">{formatCurrency(transfer.amount)}</p>
                    <div className="flex items-center gap-1 text-xs text-gray-400">
                      <CalendarDays className="h-3 w-3" />
                      {formatDisplayDate(transfer.date)}
                    </div>
                  </div>
                  <Badge variant="outline" className="bg-purple-50 text-purple-700 border-purple-200">
                    Transferência
                  </Badge>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="text-gray-400 hover:text-red-600"
                    onClick={() => setDeleteTarget(transfer.id)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
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

      <DeleteModal
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        loading={actionLoading}
      />
    </div>
  );
}
