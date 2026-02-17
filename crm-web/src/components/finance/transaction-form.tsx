'use client';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { dateToInputString, getTodayInputString } from '@/lib/date-utils';
import {
    CategoryApplicableTo,
    CreditCard,
    financeService,
    FinancialAccount,
    PaymentMethod,
    Transaction,
    TransactionCategory,
    TransactionStatus,
    TransactionType,
} from '@/services/finance';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2 } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

const formSchema = z.object({
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres'),
  amount: z.number().min(0.01, 'Valor deve ser maior que zero'),
  date: z.string().min(1, 'Data é obrigatória'),
  type: z.number(),
  categoryId: z.string().optional(),
  financialAccountId: z.string().optional(),
  creditCardId: z.string().optional(),
  creditCardInvoiceId: z.string().optional(),
  paymentMethod: z.number(),
  isRecurring: z.boolean(),
  status: z.number().optional(),
  paidDate: z.string().optional(),
});

type FormValues = z.infer<typeof formSchema>;

interface TransactionFormProps {
  initialData?: Transaction;
  isEditing?: boolean;
  defaultType?: TransactionType;
}

export function TransactionForm({ initialData, isEditing = false, defaultType }: TransactionFormProps) {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);
  const [loadingData, setLoadingData] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        const [cats, accs, cards] = await Promise.all([
          financeService.getAllTransactionCategories(),
          financeService.getActiveFinancialAccounts(),
          financeService.getCreditCards(),
        ]);
        setCategories(cats);
        setAccounts(accs);
        setCreditCards(cards);
      } catch (err) {
        console.error(err);
        setError('Falha ao carregar dados.');
      } finally {
        setLoadingData(false);
      }
    }
    loadData();
  }, []);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      description: initialData?.description || '',
      amount: initialData?.amount || 0,
      date: initialData?.date ? dateToInputString(initialData.date) : getTodayInputString(),
      type: initialData?.type ?? defaultType ?? TransactionType.Income,
      categoryId: initialData?.categoryId || undefined,
      financialAccountId: initialData?.financialAccountId || undefined,
      creditCardId: initialData?.creditCardId || undefined,
      creditCardInvoiceId: initialData?.creditCardInvoiceId || undefined,
      paymentMethod: initialData?.paymentMethod ?? PaymentMethod.Pix,
      isRecurring: initialData?.isRecurring || false,
      status: initialData?.status ?? TransactionStatus.Pending,
      paidDate: initialData?.paidDate ? dateToInputString(initialData.paidDate) : undefined,
    },
  });

  const watchedType = form.watch('type');
  const watchedPaymentMethod = form.watch('paymentMethod');
  const isCreditCard = watchedPaymentMethod === PaymentMethod.CreditCard;
  const isExpense = watchedType === TransactionType.Expense;
  const isIncome = watchedType === TransactionType.Income;
  const isTransfer = watchedType === TransactionType.Transfer;

  // Filter categories based on the selected type
  const filteredCategories = useMemo(() => {
    const applicableTo = isIncome ? CategoryApplicableTo.Income : CategoryApplicableTo.Expense;
    return categories.filter(
      c => c.isActive && (c.applicableTo === applicableTo || c.applicableTo === CategoryApplicableTo.Both)
    );
  }, [categories, isIncome]);

  const onSubmit = async (data: FormValues) => {
    setLoading(true);
    setError(null);

    try {
      const payload = {
        description: data.description,
        amount: data.amount,
        date: `${data.date}T12:00:00Z`,
        paymentMethod: data.paymentMethod,
        categoryId: data.categoryId || undefined,
        isRecurring: data.isRecurring,
        financialAccountId: data.financialAccountId || undefined,
        creditCardId: isCreditCard ? data.creditCardId : undefined,
        creditCardInvoiceId: isCreditCard ? data.creditCardInvoiceId : undefined,
        status: isExpense ? (data.status ?? TransactionStatus.Pending) : undefined,
        paidDate: isExpense && data.paidDate ? `${data.paidDate}T12:00:00Z` : undefined,
      };

      if (isEditing && initialData) {
        await financeService.updateTransaction(initialData.id, payload);
      } else {
        await financeService.createTransaction({
          ...payload,
          type: data.type as TransactionType,
        });
      }

      router.push('/finance/transactions');
      router.refresh();
    } catch (err) {
      setError('Ocorreu um erro ao salvar. Tente novamente.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
      {error && <div className="bg-red-100 text-red-700 p-3 rounded text-sm">{error}</div>}

      {/* Tipo da transação — apenas no modo criação */}
      {!isEditing && (
        <div className="space-y-2">
          <Label>Tipo de Transação</Label>
          <Select
            onValueChange={(v) => form.setValue('type', Number(v))}
            value={String(form.watch('type'))}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={String(TransactionType.Income)}>💰 Receita</SelectItem>
              <SelectItem value={String(TransactionType.Expense)}>💸 Despesa</SelectItem>
              <SelectItem value={String(TransactionType.Transfer)}>🔄 Transferência</SelectItem>
              <SelectItem value={String(TransactionType.Ignored)}>👻 Ignorada</SelectItem>
            </SelectContent>
          </Select>
        </div>
      )}

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
          <Label>Categoria</Label>
          <Select
            onValueChange={(v) => form.setValue('categoryId', v)}
            value={form.watch('categoryId') || ''}
            disabled={loadingData}
          >
            <SelectTrigger>
              <SelectValue placeholder={loadingData ? 'Carregando...' : 'Selecione...'} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Sem categoria</SelectItem>
              {filteredCategories.map((cat) => (
                <SelectItem key={cat.id} value={cat.id}>{cat.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Conta financeira — para Income, Transfer e Expense (cash) */}
      {(isIncome || isTransfer || (isExpense && !isCreditCard)) && (
        <div className="space-y-2">
          <Label>Conta Financeira</Label>
          <Select
            onValueChange={(v) => form.setValue('financialAccountId', v)}
            value={form.watch('financialAccountId') || ''}
            disabled={loadingData}
          >
            <SelectTrigger>
              <SelectValue placeholder={loadingData ? 'Carregando...' : 'Selecione a conta...'} />
            </SelectTrigger>
            <SelectContent>
              {accounts.map((acc) => (
                <SelectItem key={acc.id} value={acc.id}>{acc.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {/* Método de pagamento */}
      {!isTransfer && (
        <div className="space-y-2">
          <Label>Método de Pagamento</Label>
          <Select
            onValueChange={(v) => form.setValue('paymentMethod', Number(v))}
            value={String(form.watch('paymentMethod'))}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={String(PaymentMethod.Pix)}>Pix</SelectItem>
              <SelectItem value={String(PaymentMethod.DebitCard)}>Cartão de Débito</SelectItem>
              <SelectItem value={String(PaymentMethod.BankTransfer)}>Transferência</SelectItem>
              <SelectItem value={String(PaymentMethod.Cash)}>Dinheiro</SelectItem>
              <SelectItem value={String(PaymentMethod.Boleto)}>Boleto</SelectItem>
              {isExpense && (
                <SelectItem value={String(PaymentMethod.CreditCard)}>Cartão de Crédito</SelectItem>
              )}
            </SelectContent>
          </Select>
        </div>
      )}

      {/* Cartão de crédito — apenas para despesas com CC */}
      {isExpense && isCreditCard && (
        <div className="space-y-2">
          <Label>Cartão de Crédito</Label>
          <Select
            onValueChange={(v) => form.setValue('creditCardId', v)}
            value={form.watch('creditCardId') || ''}
            disabled={loadingData}
          >
            <SelectTrigger>
              <SelectValue placeholder={loadingData ? 'Carregando...' : 'Selecione o cartão...'} />
            </SelectTrigger>
            <SelectContent>
              {creditCards.filter(c => c.isActive).map((card) => (
                <SelectItem key={card.id} value={card.id}>{card.name} (*{card.lastFourDigits})</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {/* Status — apenas para despesas */}
      {isExpense && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Status</Label>
            <Select
              onValueChange={(v) => form.setValue('status', Number(v))}
              value={String(form.watch('status') ?? TransactionStatus.Pending)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={String(TransactionStatus.Pending)}>Pendente</SelectItem>
                <SelectItem value={String(TransactionStatus.Paid)}>Pago</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {form.watch('status') === TransactionStatus.Paid && (
            <div className="space-y-2">
              <Label htmlFor="paidDate">Data de Pagamento</Label>
              <Input id="paidDate" type="date" {...form.register('paidDate')} />
            </div>
          )}
        </div>
      )}

      <div className="flex items-center space-x-2 pt-2">
        <input
          type="checkbox"
          id="isRecurring"
          className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
          {...form.register('isRecurring')}
        />
        <Label htmlFor="isRecurring" className="cursor-pointer">É uma transação recorrente?</Label>
      </div>

      <div className="flex justify-end pt-4 gap-3">
        <Button variant="outline" type="button" onClick={() => router.back()}>Cancelar</Button>
        <Button type="submit" disabled={loading} className="bg-indigo-600 hover:bg-indigo-700">
          {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isEditing ? 'Atualizar Transação' : 'Salvar Transação'}
        </Button>
      </div>
    </form>
  );
}
