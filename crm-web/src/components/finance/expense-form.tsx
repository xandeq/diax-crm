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
    CreditCard,
    Expense,
    ExpenseCategory,
    ExpenseStatus,
    financeService,
    FinancialAccount,
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
  expenseCategoryId: z.string().min(1, 'Categoria é obrigatória'),
  paymentMethod: z.number(),
  financialAccountId: z.string().optional(),
  creditCardId: z.string().optional(),
  isRecurring: z.boolean(),
  status: z.number(),
}).refine((data) => {
    // Se método de pagamento é Cartão de Crédito, creditCardId é obrigatório
    if (data.paymentMethod === PaymentMethod.CreditCard) {
        return !!data.creditCardId && data.creditCardId.length > 0;
    }
    return true;
}, {
    message: 'Selecione um cartão de crédito',
    path: ['creditCardId'],
}).refine((data) => {
    // Se método de pagamento NÃO é Cartão de Crédito, financialAccountId é obrigatório
    if (data.paymentMethod !== PaymentMethod.CreditCard) {
        return !!data.financialAccountId && data.financialAccountId.length > 0;
    }
    return true;
}, {
    message: 'Selecione uma conta financeira',
    path: ['financialAccountId'],
});

type FormValues = z.infer<typeof formSchema>;

interface ExpenseFormProps {
  initialData?: Expense;
  isEditing?: boolean;
}

export function ExpenseForm({ initialData, isEditing = false }: ExpenseFormProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [categories, setCategories] = useState<ExpenseCategory[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [loadingCreditCards, setLoadingCreditCards] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
        try {
            const [categoriesData, accountsData, creditCardsData] = await Promise.all([
                financeService.getExpenseCategories(),
                financeService.getActiveFinancialAccounts(),
                financeService.getCreditCards()
            ]);
            setCategories(categoriesData);
            setAccounts(accountsData);
            setCreditCards(creditCardsData.filter(cc => cc.isActive));
        } catch (err) {
            console.error(err);
            setError('Falha ao carregar dados.');
        } finally {
            setLoadingCategories(false);
            setLoadingAccounts(false);
            setLoadingCreditCards(false);
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
      expenseCategoryId: initialData?.expenseCategoryId || '',
      paymentMethod: initialData?.paymentMethod ?? PaymentMethod.Pix,
      financialAccountId: initialData?.financialAccountId || '',
      creditCardId: initialData?.creditCardId || '',
      isRecurring: initialData?.isRecurring || false,
      status: initialData?.status ?? ExpenseStatus.Pending,
    },
  });

  const selectedPaymentMethod = form.watch('paymentMethod');
  const isCreditCard = selectedPaymentMethod === PaymentMethod.CreditCard;

  // Limpar campos quando muda o método de pagamento
  useEffect(() => {
    if (isCreditCard) {
      form.setValue('financialAccountId', '');
    } else {
      form.setValue('creditCardId', '');
    }
  }, [isCreditCard, form]);

  const onSubmit = async (data: FormValues) => {
    setLoading(true);
    setError(null);

    try {
      const payload = {
        description: data.description,
        amount: data.amount,
        date: data.date,
        expenseCategoryId: data.expenseCategoryId,
        paymentMethod: data.paymentMethod,
        isRecurring: data.isRecurring,
        status: data.status,
        creditCardId: isCreditCard ? data.creditCardId : undefined,
        financialAccountId: !isCreditCard ? data.financialAccountId : undefined,
      };

      if (isEditing && initialData) {
        await financeService.updateExpense(initialData.id, payload);
      } else {
        await financeService.createExpense(payload);
      }
      router.push('/finance/expenses');
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
            <Label htmlFor="expenseCategoryId">Categoria</Label>
            <Select
                onValueChange={(value) => form.setValue('expenseCategoryId', value)}
                value={form.watch('expenseCategoryId')}
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
            {form.formState.errors.expenseCategoryId && (
                <p className="text-red-500 text-xs">{form.formState.errors.expenseCategoryId.message}</p>
            )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
            <Label>Método de Pagamento</Label>
            <Select
                onValueChange={(value) => form.setValue('paymentMethod', Number(value))}
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

        <div className="space-y-2">
            <Label>Status</Label>
            <Select
                onValueChange={(value) => form.setValue('status', Number(value))}
                value={String(form.watch('status'))}
            >
                <SelectTrigger>
                    <SelectValue placeholder="Selecione..." />
                </SelectTrigger>
                <SelectContent>
                    <SelectItem value={String(ExpenseStatus.Pending)}>Pendente</SelectItem>
                    <SelectItem value={String(ExpenseStatus.Paid)}>Pago</SelectItem>
                </SelectContent>
            </Select>
        </div>
      </div>

      {/* Conditional: Credit Card selector (when payment method is CreditCard) */}
      {isCreditCard && (
        <div className="space-y-2 p-4 bg-blue-50 rounded-lg border border-blue-200">
          <Label htmlFor="creditCardId">Cartão de Crédito *</Label>
          <Select
              onValueChange={(value) => form.setValue('creditCardId', value)}
              value={form.watch('creditCardId')}
              disabled={loadingCreditCards}
          >
              <SelectTrigger>
                  <SelectValue placeholder={loadingCreditCards ? "Carregando..." : "Selecione o cartão..."} />
              </SelectTrigger>
              <SelectContent>
                  {creditCards.map((card) => (
                      <SelectItem key={card.id} value={card.id}>
                          {card.name} (**** {card.lastFourDigits})
                      </SelectItem>
                  ))}
              </SelectContent>
          </Select>
          {form.formState.errors.creditCardId && (
              <p className="text-red-500 text-xs">{form.formState.errors.creditCardId.message}</p>
          )}
          <p className="text-xs text-blue-600">A despesa será vinculada à fatura do cartão selecionado.</p>
        </div>
      )}

      {/* Conditional: Financial Account selector (when payment method is NOT CreditCard) */}
      {!isCreditCard && (
        <div className="space-y-2 p-4 bg-green-50 rounded-lg border border-green-200">
          <Label htmlFor="financialAccountId">Conta Financeira *</Label>
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
          <p className="text-xs text-green-600">O valor será debitado desta conta quando a despesa for marcada como paga.</p>
        </div>
      )}

      <div className="flex items-center space-x-2 pt-2">
        <input
            type="checkbox"
            id="isRecurring"
            className="rounded border-gray-300 text-red-600 focus:ring-red-500"
            {...form.register('isRecurring')}
        />
        <Label htmlFor="isRecurring" className="cursor-pointer">É uma despesa recorrente?</Label>
      </div>

      <div className="flex justify-end pt-4 gap-3">
        <Button variant="outline" type="button" onClick={() => router.back()}>Cancelar</Button>
        <Button type="submit" disabled={loading} className="bg-red-600 hover:bg-red-700">
          {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isEditing ? 'Atualizar Despesa' : 'Salvar Despesa'}
        </Button>
      </div>
    </form>
  );
}
