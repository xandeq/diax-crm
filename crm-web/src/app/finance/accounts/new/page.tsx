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
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';
import { motion } from 'framer-motion';

const formSchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório').max(200, 'Nome deve ter no máximo 200 caracteres'),
  accountType: z.number(),
  initialBalance: z.number(),
});

type FormValues = z.infer<typeof formSchema>;

export default function NewFinancialAccountPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      accountType: AccountType.Checking,
      initialBalance: 0,
    },
  });

  const onSubmit = async (data: FormValues) => {
    setLoading(true);
    setError(null);

    try {
      await financeService.createFinancialAccount(data);
      router.push('/finance/accounts');
      router.refresh();
    } catch (err) {
      setError('Erro ao criar conta financeira. Por favor, verifique os dados.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

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
              Nova Conta Financeira
            </h1>
            <p className="text-xs text-zinc-400">Cadastre uma nova conta para começar a registrar transações</p>
          </div>
        </div>

        <motion.div 
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.3 }}
          className="rounded-2xl p-6 border border-zinc-800/80 bg-[#0a130f]/60 backdrop-blur-md relative overflow-hidden"
        >
          <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />

          {error && (
            <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-3 rounded-xl mb-4 text-xs font-semibold">
              {error}
            </div>
          )}

          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5 relative z-10">
            <div className="space-y-1.5">
              <Label htmlFor="name" className="text-zinc-300 font-medium text-xs">Nome da Conta</Label>
              <Input 
                id="name" 
                {...form.register('name')} 
                placeholder="Ex: Nubank, Itaú, Carteira, Investimentos..." 
                className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm"
              />
              {form.formState.errors.name && (
                <p className="text-rose-400 text-[11px] font-medium">{form.formState.errors.name.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-zinc-300 font-medium text-xs">Tipo de Conta</Label>
              <Select
                onValueChange={(value) => form.setValue('accountType', Number(value))}
                value={String(form.watch('accountType'))}
              >
                <SelectTrigger className="bg-zinc-950/80 border-zinc-800 text-zinc-100 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 text-sm">
                  <SelectValue placeholder="Selecione o tipo..." />
                </SelectTrigger>
                <SelectContent className="bg-zinc-900 border-zinc-800 text-zinc-100 rounded-xl">
                  <SelectItem value={String(AccountType.Checking)}>Conta Corrente</SelectItem>
                  <SelectItem value={String(AccountType.Business)}>Conta Empresarial</SelectItem>
                  <SelectItem value={String(AccountType.Savings)}>Poupança</SelectItem>
                  <SelectItem value={String(AccountType.Cash)}>Dinheiro</SelectItem>
                  <SelectItem value={String(AccountType.Investment)}>Investimento</SelectItem>
                  <SelectItem value={String(AccountType.DigitalWallet)}>Carteira Digital</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="initialBalance" className="text-zinc-300 font-medium text-xs">Saldo Inicial</Label>
              <div className="relative">
                <span className="absolute left-3.5 top-2.5 text-xs text-zinc-500 font-bold">R$</span>
                <Input
                  id="initialBalance"
                  type="number"
                  step="0.01"
                  {...form.register('initialBalance', { valueAsNumber: true })}
                  placeholder="0,00"
                  className="bg-zinc-950/80 border-zinc-800 text-zinc-100 placeholder-zinc-500 focus:border-[#00D4AA] focus:ring-1 focus:ring-[#00D4AA] rounded-xl h-10 pl-9 text-sm"
                />
              </div>
              {form.formState.errors.initialBalance && (
                <p className="text-rose-400 text-[11px] font-medium">{form.formState.errors.initialBalance.message}</p>
              )}
            </div>

            <div className="flex justify-end pt-4 gap-3">
              <Button 
                variant="outline" 
                type="button" 
                onClick={() => router.back()}
                className="border-zinc-800 text-zinc-400 hover:text-zinc-100 hover:bg-zinc-900/50 rounded-xl"
              >
                Cancelar
              </Button>
              <Button 
                type="submit" 
                disabled={loading} 
                className="bg-[#00D4AA] text-[#0A130F] hover:bg-[#00B894] font-semibold transition-all duration-300 rounded-xl"
              >
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Salvar Conta
              </Button>
            </div>
          </form>
        </motion.div>
      </div>
    </div>
  );
}
