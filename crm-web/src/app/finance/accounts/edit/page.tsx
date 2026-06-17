'use client';

import { FinanceNav } from '@/components/finance/FinanceNav';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { AccountType, financeService } from '@/services/finance';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Loader2, Landmark } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import { motion } from 'framer-motion';

const editAccountSchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório'),
  accountType: z.nativeEnum(AccountType),
  isActive: z.boolean(),
});

type EditAccountForm = z.infer<typeof editAccountSchema>;

const accountTypeLabels: Record<AccountType, string> = {
  [AccountType.Checking]: 'Conta Corrente',
  [AccountType.Business]: 'Conta Empresarial',
  [AccountType.Savings]: 'Poupança',
  [AccountType.Cash]: 'Dinheiro',
  [AccountType.Investment]: 'Investimento',
  [AccountType.DigitalWallet]: 'Carteira Digital',
};

function EditAccountContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const id = searchParams.get('id');
  const [loading, setLoading] = useState(true);
  const [currentBalance, setCurrentBalance] = useState<number>(0);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<EditAccountForm>({
    resolver: zodResolver(editAccountSchema),
    defaultValues: {
      isActive: true,
    },
  });

  useEffect(() => {
    if (id) {
      loadAccount();
    }
  }, [id]);

  const loadAccount = async () => {
    if (!id) return;
    try {
      const account = await financeService.getFinancialAccountById(id);
      setValue('name', account.name);
      setValue('accountType', account.accountType);
      setValue('isActive', account.isActive);
      setCurrentBalance(account.balance);
    } catch (err) {
      console.error(err);
      toast.error('Erro ao carregar conta financeira');
      router.push('/finance/accounts');
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (data: EditAccountForm) => {
    if (!id) return;
    try {
      await financeService.updateFinancialAccount(id, data);
      toast.success('Conta financeira atualizada com sucesso');
      router.push('/finance/accounts');
    } catch (err) {
      console.error(err);
      toast.error('Erro ao atualizar conta financeira');
    }
  };

  if (!id) {
    return (
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <FinanceNav />
        <div className="max-w-xl mx-auto p-6 rounded-2xl border border-rose-500/20 bg-rose-500/5 text-rose-400 text-sm font-semibold">
          ID da conta não informado
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <FinanceNav />
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
          <p className="text-zinc-400 text-sm animate-pulse">Carregando dados da conta...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-6 max-w-7xl">
      <FinanceNav />

      <div className="max-w-xl mx-auto space-y-6">
        <div className="flex items-center gap-3">
          <Button 
            variant="ghost" 
            size="icon" 
            onClick={() => router.back()} 
            className="rounded-full text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50"
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-xl font-bold tracking-tight text-zinc-100 flex items-center gap-2">
              <Landmark className="h-5 w-5 text-emerald-400" />
              Editar Conta Financeira
            </h1>
            <p className="text-xs text-zinc-400">Atualize as informações da sua conta bancária</p>
          </div>
        </div>

        <motion.div 
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="rounded-2xl p-6 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5 relative z-10">
            <div className="space-y-1.5">
              <Label htmlFor="name" className="text-zinc-300 font-medium text-xs">Nome</Label>
              <Input 
                id="name" 
                {...register('name')} 
                className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
              />
              {errors.name && (
                <p className="text-rose-400 text-[11px] font-medium">{errors.name.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-zinc-300 font-medium text-xs">Tipo de Conta</Label>
              <Select
                onValueChange={(value) => setValue('accountType', Number(value))}
                value={String(watch('accountType'))}
              >
                <SelectTrigger className="bg-zinc-950/80 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm">
                  <SelectValue placeholder="Selecione o tipo..." />
                </SelectTrigger>
                <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100 rounded-xl">
                  {Object.entries(accountTypeLabels).map(([value, label]) => (
                    <SelectItem key={value} value={value}>{label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.accountType && (
                <p className="text-rose-400 text-[11px] font-medium">{errors.accountType.message}</p>
              )}
            </div>

            <div className="space-y-1.5 font-medium text-xs">
              <Label className="text-zinc-300">Saldo Atual (Somente Leitura)</Label>
              <div className="relative">
                <Input
                  type="text"
                  value={new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(currentBalance)}
                  disabled
                  className="bg-zinc-950/50 border-zinc-900 text-zinc-400 rounded-xl h-10 text-sm cursor-not-allowed border-dashed select-none"
                />
              </div>
              <p className="text-[10px] text-zinc-500 mt-1 leading-relaxed">
                * O saldo desta conta é recalculado dinamicamente com base nas receitas, despesas e transferências associadas.
              </p>
            </div>

            <div className="flex items-center gap-3 p-3 rounded-xl bg-zinc-950/40 border border-zinc-900/60 w-fit select-none">
              <input
                type="checkbox"
                id="isActive"
                {...register('isActive')}
                className="rounded border-zinc-800 bg-zinc-950 text-emerald-400 focus:ring-[#00D4AA] focus:ring-offset-0 focus:ring-1 h-4.5 w-4.5 cursor-pointer transition-colors"
              />
              <Label htmlFor="isActive" className="text-zinc-300 font-medium text-xs cursor-pointer select-none">
                Esta conta está ativa
              </Label>
            </div>

            <div className="flex justify-end pt-4 gap-3">
              <Button 
                variant="outline" 
                type="button" 
                onClick={() => router.push('/finance/accounts')}
                className="border-zinc-800 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
              >
                Cancelar
              </Button>
              <Button 
                type="submit" 
                disabled={isSubmitting} 
                className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 rounded-xl"
              >
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Salvar Alterações
              </Button>
            </div>
          </form>
        </motion.div>
      </div>
    </div>
  );
}

export default function EditAccountPage() {
  return (
    <Suspense fallback={
      <div className="container mx-auto px-4 py-6 max-w-7xl">
        <FinanceNav />
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <Loader2 className="h-8 w-8 animate-spin text-emerald-400" />
          <p className="text-zinc-400 text-sm">Preparando edição de conta...</p>
        </div>
      </div>
    }>
      <EditAccountContent />
    </Suspense>
  );
}
