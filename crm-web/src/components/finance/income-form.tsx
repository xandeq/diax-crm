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
import {
    financeService,
    FinancialAccount,
    Income,
    IncomeCategory,
    PaymentMethod
} from '@/services/finance';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

const formSchema = z.object({
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres'),
  amount: z.number().min(0.01, 'Valor deve ser maior que zero'),
  date: z.string().min(1, 'Data é obrigatória'),
  incomeCategoryId: z.string().min(1, 'Categoria é obrigatória'),
  financialAccountId: z.string().min(1, 'Conta financeira é obrigatória'),
  paymentMethod: z.number(),
  isRecurring: z.boolean(),
});

type FormValues = z.infer<typeof formSchema>;

interface IncomeFormProps {
  initialData?: Income;
  isEditing?: boolean;
}

export function IncomeForm({ initialData, isEditing = false }: IncomeFormProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [categories, setCategories] = useState<IncomeCategory[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
        try {
            const [categoriesData, accountsData] = await Promise.all([
                financeService.getIncomeCategories(),
                financeService.getActiveFinancialAccounts()
            ]);
            setCategories(categoriesData);
            setAccounts(accountsData);
        } catch (err) {
            console.error(err);
            setError('Falha ao carregar dados.');
        } finally {
            setLoadingCategories(false);
            setLoadingAccounts(false);
        }
    }
    loadData();
  }, []);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      description: initialData?.description || '',
      amount: initialData?.amount || 0,
      date: initialData?.date ? new Date(initialData.date).toISOString().split('T')[0] : new Date().toISOString().split('T')[0],
      incomeCategoryId: initialData?.incomeCategoryId || '',
      financialAccountId: initialData?.financialAccountId || '',
      paymentMethod: initialData?.paymentMethod ?? PaymentMethod.Pix,
      isRecurring: initialData?.isRecurring || false,
    },
  });


  const onSubmit = async (data: FormValues) => {
    setLoading(true);
    setError(null);

    try {
      if (isEditing && initialData) {
        await financeService.updateIncome(initialData.id, {
            ...data,
            incomeCategoryId: data.incomeCategoryId,
            financialAccountId: data.financialAccountId
        });
      } else {
        await financeService.createIncome({
            ...data,
            incomeCategoryId: data.incomeCategoryId,
            financialAccountId: data.financialAccountId
        });
      }
      router.push('/finance/incomes');
      router.refresh();
    } catch (err) {
      setError('Ocorreu um erro ao salvar. Tente novamente.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      {error && <div className="bg-red-100 text-red-700 p-3 rounded text-sm">{error}</div>}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label htmlFor="description">Descrição</Label>
          <Input id="description" {...form.register('description')} />
          {form.formState.errors.description && (
            <p className="text-red-500 text-xs">{form.formState.errors.description.message}</p>
          )}
        </div>

        <div className="space-y-2">
            <Label htmlFor="amount">Valor</Label>
            <Input id="amount" type="number" step="0.01" {...form.register('amount', { valueAsNumber: true })} />
            {form.formState.errors.amount && (
                <p className="text-red-500 text-xs">{form.formState.errors.amount.message}</p>
            )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
            <Label htmlFor="date">Data</Label>
            <Input id="date" type="date" {...form.register('date')} />
            {form.formState.errors.date && (
                <p className="text-red-500 text-xs">{form.formState.errors.date.message}</p>
            )}
        </div>

        <div className="space-y-2">
            <Label htmlFor="incomeCategoryId">Categoria</Label>
            <Select
                onValueChange={(value) => form.setValue('incomeCategoryId', value)}
                value={form.watch('incomeCategoryId')}
                disabled={loadingCategories}
            >
                <SelectTrigger>
                    <SelectValue placeholder={loadingCategories ? "Carregando..." : "Selecione..."} />
                </SelectTrigger>
                <SelectContent>
                    {categories.map((cat) => (
                        <SelectItem key={cat.id} value={cat.id}>
                            {cat.name}
                        </SelectItem>
                    ))}
                </SelectContent>
            </Select>
            {form.formState.errors.incomeCategoryId && (
                <p className="text-red-500 text-xs">{form.formState.errors.incomeCategoryId.message}</p>
            )}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="financialAccountId">Conta Financeira</Label>
        <Select
            onValueChange={(value) => form.setValue('financialAccountId', value)}
            value={form.watch('financialAccountId')}
            disabled={loadingAccounts}
        >
            <SelectTrigger>
                <SelectValue placeholder={loadingAccounts ? "Carregando..." : "Selecione a conta..."} />
            </SelectTrigger>
            <SelectContent>
                {accounts.map((account) => (
                    <SelectItem key={account.id} value={account.id}>
                        {account.name}
                    </SelectItem>
                ))}
            </SelectContent>
        </Select>
        {form.formState.errors.financialAccountId && (
            <p className="text-red-500 text-xs">{form.formState.errors.financialAccountId.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label>Método de Pagamento</Label>
        <Select
          onValueChange={(value) => form.setValue('paymentMethod', Number(value))}
          defaultValue={String(form.getValues('paymentMethod') ?? PaymentMethod.Pix)}
          value={String(form.watch('paymentMethod'))}
        >
          <SelectTrigger>
            <SelectValue placeholder="Selecione..." />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={String(PaymentMethod.CreditCard)}>Cartão de Crédito</SelectItem>
            <SelectItem value={String(PaymentMethod.DebitCard)}>Cartão de Débito</SelectItem>
            <SelectItem value={String(PaymentMethod.Pix)}>Pix</SelectItem>
            <SelectItem value={String(PaymentMethod.Cash)}>Dinheiro</SelectItem>
            <SelectItem value={String(PaymentMethod.BankTransfer)}>Transferência</SelectItem>
            <SelectItem value={String(PaymentMethod.Boleto)}>Boleto</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center space-x-2 pt-2">
        <input
            type="checkbox"
            id="isRecurring"
            className="rounded border-gray-300 text-green-600 focus:ring-green-500"
            {...form.register('isRecurring')}
        />
        <Label htmlFor="isRecurring" className="cursor-pointer">É uma receita recorrente?</Label>
      </div>

      <div className="flex justify-end pt-4 gap-3">
        <Button variant="outline" type="button" onClick={() => router.back()}>Cancelar</Button>
        <Button type="submit" disabled={loading} className="bg-green-600 hover:bg-green-700">
          {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isEditing ? 'Atualizar Receita' : 'Salvar Receita'}
        </Button>
      </div>
    </form>
  );
}
