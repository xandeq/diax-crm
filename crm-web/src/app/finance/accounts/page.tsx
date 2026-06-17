'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { Button } from '@/components/ui/button';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { useDeleteFinancialAccount, useFinancialAccounts } from '@/hooks/finance';
import { AccountType } from '@/services/finance';
import { 
  Landmark, 
  Briefcase, 
  PiggyBank, 
  Coins, 
  TrendingUp, 
  Wallet, 
  Plus, 
  Pencil, 
  Trash2, 
  Loader2,
  XCircle
} from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import { EmptyState } from '@/components/dashboard/EmptyState';

const accountTypeLabels: Record<AccountType, string> = {
  [AccountType.Checking]: 'Conta Corrente',
  [AccountType.Business]: 'Conta Empresarial',
  [AccountType.Savings]: 'Poupança',
  [AccountType.Cash]: 'Dinheiro',
  [AccountType.Investment]: 'Investimento',
  [AccountType.DigitalWallet]: 'Carteira Digital',
};

const accountTypeIcons: Record<AccountType, any> = {
  [AccountType.Checking]: Landmark,
  [AccountType.Business]: Briefcase,
  [AccountType.Savings]: PiggyBank,
  [AccountType.Cash]: Coins,
  [AccountType.Investment]: TrendingUp,
  [AccountType.DigitalWallet]: Wallet,
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
        toast.success('Conta excluída com sucesso');
      } catch (err) {
        toast.error('Erro ao excluir conta. Verifique se não há transações vinculadas.');
      }
    });
  }

  if (isLoading) {
    return (
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <FinanceNav />
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
          <p className="text-zinc-400 text-sm animate-pulse">Carregando contas financeiras...</p>
        </div>
      </div>
    );
  }

  const activeAccounts = accounts.filter(a => a.isActive);
  const inactiveAccounts = accounts.filter(a => !a.isActive);

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      {confirmDialogNode}
      <FinanceNav />

      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-8">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
            <Landmark className="h-6 w-6 text-emerald-400" />
            Contas Financeiras
          </h1>
          <p className="text-zinc-400 text-sm mt-1">Gerencie suas contas bancárias, investimentos e carteiras</p>
        </div>
        <Link href="/finance/accounts/new">
          <Button className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 gap-2 rounded-xl">
            <Plus className="h-4 w-4" />
            Nova Conta
          </Button>
        </Link>
      </div>

      {isError && (
        <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl mb-6 flex items-center gap-2 text-sm">
          <XCircle className="h-4 w-4 shrink-0" />
          Erro ao carregar contas. Por favor, tente novamente.
        </div>
      )}

      {accounts.length === 0 ? (
        <EmptyState
          title="Nenhuma conta cadastrada"
          description="Cadastre uma conta corrente, poupança, carteira digital ou conta de investimentos para gerenciar seu patrimônio."
          icon={Landmark}
          actionLabel="Nova Conta"
          actionHref="/finance/accounts/new"
        />
      ) : (
        <div className="space-y-8">
          {activeAccounts.length > 0 && (
            <div className="space-y-4">
              <h2 className="text-xs font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-2">
                Contas Ativas
                <span className="h-4 px-1.5 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] flex items-center justify-center font-semibold border border-emerald-500/20">
                  {activeAccounts.length}
                </span>
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {activeAccounts.map((account, index) => {
                  const IconComponent = accountTypeIcons[account.accountType] ?? Landmark;
                  return (
                    <motion.div
                      key={account.id}
                      initial={{ opacity: 0, y: 15 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ duration: 0.3, delay: index * 0.05 }}
                      whileHover={{ y: -4, transition: { duration: 0.2 } }}
                      className={cn(
                        "relative overflow-hidden rounded-2xl p-5 transition-all duration-300",
                        "bg-[#0a130f]/60 backdrop-blur-md border border-zinc-800/80 hover:border-emerald-500/30 hover:shadow-xl hover:shadow-emerald-950/10"
                      )}
                    >
                      <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
                      
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex items-center gap-3">
                          <div className="p-2.5 rounded-xl bg-zinc-900/80 text-zinc-100 border border-zinc-800/60 flex items-center justify-center">
                            <IconComponent className="h-5 w-5 text-[#00D4AA]" />
                          </div>
                          <div>
                            <h3 className="font-semibold text-zinc-100 leading-none">{account.name}</h3>
                            <span className="text-xs text-zinc-400 mt-1 block">
                              {accountTypeLabels[account.accountType]}
                            </span>
                          </div>
                        </div>
                        
                        <div className="flex items-center gap-1.5">
                          <span className="h-1.5 w-1.5 rounded-full bg-emerald-400 animate-pulse" />
                          <span className="text-[10px] uppercase font-bold tracking-wider text-emerald-400">Ativa</span>
                        </div>
                      </div>
                      
                      <div className="mt-6">
                        <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-500 block mb-0.5">Saldo Disponível</span>
                        <span className={cn(
                          "text-2xl font-bold tracking-tight tabular-nums",
                          account.balance >= 0 ? "text-emerald-400" : "text-rose-400"
                        )}>
                          {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(account.balance)}
                        </span>
                      </div>
                      
                      <div className="flex items-center justify-end gap-2 mt-4 pt-3 border-t border-zinc-800/50">
                        <Link href={`/finance/accounts/edit?id=${account.id}`} className="p-2 rounded-lg text-zinc-400 hover:text-emerald-400 hover:bg-zinc-900/50 transition-colors">
                          <Pencil className="h-4 w-4" />
                          <span className="sr-only">Editar</span>
                        </Link>
                        <button
                          onClick={() => handleDelete(account.id)}
                          disabled={deleteMutation.isPending}
                          className="p-2 rounded-lg text-zinc-400 hover:text-rose-400 hover:bg-zinc-900/50 transition-colors disabled:opacity-50"
                        >
                          <Trash2 className="h-4 w-4" />
                          <span className="sr-only">Excluir</span>
                        </button>
                      </div>
                    </motion.div>
                  );
                })}
              </div>
            </div>
          )}

          {inactiveAccounts.length > 0 && (
            <div className="space-y-4 pt-4 border-t border-zinc-800/60">
              <h2 className="text-xs font-bold uppercase tracking-wider text-zinc-500 flex items-center gap-2">
                Contas Inativas
                <span className="h-4 px-1.5 rounded-full bg-zinc-800 text-zinc-400 text-[10px] flex items-center justify-center font-semibold border border-zinc-700/20">
                  {inactiveAccounts.length}
                </span>
              </h2>
              
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 opacity-60">
                {inactiveAccounts.map((account, index) => {
                  const IconComponent = accountTypeIcons[account.accountType] ?? Landmark;
                  return (
                    <motion.div
                      key={account.id}
                      initial={{ opacity: 0, y: 15 }}
                      animate={{ opacity: 1, y: 0 }}
                      transition={{ duration: 0.3, delay: index * 0.05 }}
                      whileHover={{ y: -2 }}
                      className={cn(
                        "relative overflow-hidden rounded-2xl p-5 transition-all duration-300",
                        "bg-[#0a130f]/30 border border-zinc-900 hover:border-zinc-800 hover:shadow-md"
                      )}
                    >
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex items-center gap-3">
                          <div className="p-2.5 rounded-xl bg-zinc-950 text-zinc-400 border border-zinc-900 flex items-center justify-center">
                            <IconComponent className="h-5 w-5" />
                          </div>
                          <div>
                            <h3 className="font-semibold text-zinc-300 leading-none">{account.name}</h3>
                            <span className="text-xs text-zinc-500 mt-1 block">
                              {accountTypeLabels[account.accountType]}
                            </span>
                          </div>
                        </div>
                        
                        <div className="flex items-center gap-1.5">
                          <span className="h-1.5 w-1.5 rounded-full bg-zinc-600" />
                          <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-500">Inativa</span>
                        </div>
                      </div>
                      
                      <div className="mt-6">
                        <span className="text-[10px] uppercase font-bold tracking-wider text-zinc-600 block mb-0.5">Saldo Disponível</span>
                        <span className="text-xl font-bold tracking-tight tabular-nums text-zinc-400">
                          {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(account.balance)}
                        </span>
                      </div>
                      
                      <div className="flex items-center justify-end gap-2 mt-4 pt-3 border-t border-zinc-900/50">
                        <Link href={`/finance/accounts/edit?id=${account.id}`} className="p-2 rounded-lg text-zinc-500 hover:text-zinc-300 hover:bg-zinc-900/50 transition-colors">
                          <Pencil className="h-4 w-4" />
                          <span className="sr-only">Editar</span>
                        </Link>
                        <button
                          onClick={() => handleDelete(account.id)}
                          disabled={deleteMutation.isPending}
                          className="p-2 rounded-lg text-zinc-500 hover:text-rose-500 hover:bg-zinc-900/50 transition-colors disabled:opacity-50"
                        >
                          <Trash2 className="h-4 w-4" />
                          <span className="sr-only">Excluir</span>
                        </button>
                      </div>
                    </motion.div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
