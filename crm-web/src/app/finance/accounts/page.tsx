'use client';

import { Button } from '@/components/ui/button';
import { useAuth } from '@/contexts/AuthContext';
import { AccountType, FinancialAccount, financeService } from '@/services/finance';
import { Loader2, Pencil, Plus, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

const accountTypeLabels: Record<AccountType, string> = {
  [AccountType.Checking]: 'Conta Corrente',
  [AccountType.Business]: 'Conta Empresarial',
  [AccountType.Savings]: 'Poupança',
  [AccountType.Cash]: 'Dinheiro',
  [AccountType.Investment]: 'Investimento',
  [AccountType.DigitalWallet]: 'Carteira Digital',
};

function getAccountTypeLabel(type: AccountType): string {
  return accountTypeLabels[type] ?? 'Desconhecido';
}

export default function FinancialAccountsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    
    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    loadAccounts();
  }, [isAuthenticated, authLoading, router]);

  async function loadAccounts() {
    try {
      const data = await financeService.getFinancialAccounts();
      setAccounts(data);
    } catch (err) {
      setError('Erro ao carregar contas');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Tem certeza que deseja excluir esta conta? Esta ação não pode ser desfeita se houver transações vinculadas.')) return;

    try {
      await financeService.deleteFinancialAccount(id);
      await loadAccounts();
    } catch (err) {
      alert('Erro ao excluir conta. Verifique se não há transações vinculadas.');
      console.error(err);
    }
  }

  if (authLoading || loading) {
    return (
      <div className="p-8 flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-gray-500" />
      </div>
    );
  }

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold">Contas Financeiras</h1>
          <p className="text-gray-600">Gerencie suas contas bancárias e carteiras</p>
        </div>
        <Link href="/finance/accounts/new">
          <Button className="bg-blue-600 hover:bg-blue-700">
            <Plus className="mr-2 h-4 w-4" />
            Nova Conta
          </Button>
        </Link>
      </div>

      {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-6">{error}</div>}

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Nome
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tipo
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Saldo
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Ações
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {accounts.map((account) => (
              <tr key={account.id} className={!account.isActive ? 'bg-gray-50 opacity-60' : ''}>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">{account.name}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-500">{getAccountTypeLabel(account.accountType)}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className={`text-sm font-medium ${account.balance >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(account.balance)}
                  </div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                    account.isActive
                      ? 'bg-green-100 text-green-800'
                      : 'bg-gray-100 text-gray-800'
                  }`}>
                    {account.isActive ? 'Ativa' : 'Inativa'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <Link
                    href={`/finance/accounts/edit?id=${account.id}`}
                    className="text-indigo-600 hover:text-indigo-900 mr-4"
                  >
                    <Pencil className="h-4 w-4 inline" />
                  </Link>
                  <button
                    onClick={() => handleDelete(account.id)}
                    className="text-red-600 hover:text-red-900"
                  >
                    <Trash2 className="h-4 w-4 inline" />
                  </button>
                </td>
              </tr>
            ))}
            {accounts.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-4 text-center text-gray-500">
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
