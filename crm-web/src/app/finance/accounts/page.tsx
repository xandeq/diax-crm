'use client';

import { Button } from '@/components/ui/button';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { useDeleteFinancialAccount, useFinancialAccounts } from '@/hooks/finance';
import { AccountType } from '@/services/finance';
import { Loader2, Pencil, Plus, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';

const accountTypeLabels: Record<AccountType, string> = {
  [AccountType.Checking]: 'Conta Corrente',
  [AccountType.Business]: 'Conta Empresarial',
  [AccountType.Savings]: 'Poupança',
  [AccountType.Cash]: 'Dinheiro',
  [AccountType.Investment]: 'Investimento',
  [AccountType.DigitalWallet]: 'Carteira Digital',
};

export default function FinancialAccountsPage() {
  const router = useRouter();
  const { data: accounts = [], isLoading, isError } = useFinancialAccounts();
  const deleteMutation = useDeleteFinancialAccount();
  const { showConfirm, confirmDialogNode } = useConfirmDialog();

  function handleDelete(id: string) {
    showConfirm('Tem certeza que deseja excluir esta conta? Esta ação não pode ser desfeita se houver transações vinculadas.', async () => {
      try {
        await deleteMutation.mutateAsync(id);
      } catch {
        toast.error('Erro ao excluir conta. Verifique se não há transações vinculadas.');
      }
    });
  }

  if (isLoading) {
    return (
      <div className="p-8 flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-gray-500" />
      </div>
    );
  }

  return (
    <div className="p-8">
      {confirmDialogNode}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold">Contas Financeiras</h1>
          <p style={{ color: '#9CA3AF' }}>Gerencie suas contas bancárias e carteiras</p>
        </div>
        <Link href="/finance/accounts/new">
          <Button className="bg-blue-600 hover:bg-blue-700">
            <Plus className="mr-2 h-4 w-4" />
            Nova Conta
          </Button>
        </Link>
      </div>

      {isError && <div className="bg-red-100 text-red-700 p-4 rounded mb-6">Erro ao carregar contas</div>}

      <div className="rounded-lg overflow-hidden" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
        <table className="min-w-full">
          <thead style={{ background: 'rgba(255,255,255,0.04)', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Nome</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Tipo</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Saldo</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Status</th>
              <th className="px-6 py-3 text-right text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Ações</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((account) => (
              <tr
                key={account.id}
                style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', opacity: !account.isActive ? 0.6 : 1 }}
              >
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium" style={{ color: '#F9FAFB' }}>{account.name}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm" style={{ color: '#9CA3AF' }}>{accountTypeLabels[account.accountType] ?? 'Desconhecido'}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className={`text-sm font-medium ${account.balance >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(account.balance)}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                    account.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                  }`}>
                    {account.isActive ? 'Ativa' : 'Inativa'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <Link href={`/finance/accounts/edit?id=${account.id}`} className="text-indigo-400 hover:text-indigo-300 mr-4">
                    <Pencil className="h-4 w-4 inline" />
                  </Link>
                  <button
                    onClick={() => handleDelete(account.id)}
                    disabled={deleteMutation.isPending}
                    className="text-red-400 hover:text-red-300 disabled:opacity-50"
                  >
                    <Trash2 className="h-4 w-4 inline" />
                  </button>
                </td>
              </tr>
            ))}
            {accounts.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-4 text-center" style={{ color: '#9CA3AF' }}>
                  Nenhuma conta encontrada
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="mt-4">
        <Button variant="outline" onClick={() => router.push('/finance')}>
          Voltar
        </Button>
      </div>
    </div>
  );
}
