'use client';

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
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

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
      setError('Erro ao criar conta');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-lg mx-auto">
      <h1 className="text-2xl font-bold mb-6">Nova Conta Financeira</h1>

      <div className="bg-white shadow rounded-lg p-6">
        {error && <div className="bg-red-100 text-red-700 p-3 rounded mb-4 text-sm">{error}</div>}

        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Nome da Conta</Label>
            <Input id="name" {...form.register('name')} placeholder="Ex: Nubank, Itaú, Carteira..." />
            {form.formState.errors.name && (
              <p className="text-red-500 text-xs">{form.formState.errors.name.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label>Tipo de Conta</Label>
            <Select
              onValueChange={(value) => form.setValue('accountType', Number(value))}
              value={String(form.watch('accountType'))}
            >
              <SelectTrigger>
                <SelectValue placeholder="Selecione o tipo..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(AccountType.Checking)}>Conta Corrente</SelectItem>
                <SelectItem value={String(AccountType.Business)}>Conta Empresarial</SelectItem>
                <SelectItem value={String(AccountType.Savings)}>Poupança</SelectItem>
                <SelectItem value={String(AccountType.Cash)}>Dinheiro</SelectItem>
                <SelectItem value={String(AccountType.Investment)}>Investimento</SelectItem>
                <SelectItem value={String(AccountType.DigitalWallet)}>Carteira Digital</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="initialBalance">Saldo Inicial</Label>
            <Input
              id="initialBalance"
              type="number"
              step="0.01"
              {...form.register('initialBalance', { valueAsNumber: true })}
              placeholder="0.00"
            />
            {form.formState.errors.initialBalance && (
              <p className="text-red-500 text-xs">{form.formState.errors.initialBalance.message}</p>
            )}
          </div>

          <div className="flex justify-end pt-4 gap-3">
            <Button variant="outline" type="button" onClick={() => router.back()}>
              Cancelar
            </Button>
            <Button type="submit" disabled={loading} className="bg-blue-600 hover:bg-blue-700">
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Salvar Conta
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
