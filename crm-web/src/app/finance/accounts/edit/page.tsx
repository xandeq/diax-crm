'use client';

import { AccountType, financeService } from '@/services/finance';
import { zodResolver } from '@hookform/resolvers/zod';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

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
      alert('Erro ao carregar conta financeira');
      router.push('/finance/accounts');
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (data: EditAccountForm) => {
    if (!id) return;
    try {
      await financeService.updateFinancialAccount(id, data);
      router.push('/finance/accounts');
    } catch (err) {
      console.error(err);
      alert('Erro ao atualizar conta financeira');
    }
  };

  if (!id) {
    return <div className="p-8 text-red-600">ID da conta não informado</div>;
  }

  if (loading) return <div className="p-8">Carregando...</div>;

  return (
    <div className="p-8 max-w-2xl">
      <h1 className="text-2xl font-bold mb-6">Editar Conta Financeira</h1>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Nome</label>
          <input
            type="text"
            {...register('name')}
            className="w-full border rounded p-2"
          />
          {errors.name && <p className="text-red-600 text-sm">{errors.name.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Tipo de Conta</label>
          <select {...register('accountType', { valueAsNumber: true })} className="w-full border rounded p-2">
            {Object.entries(accountTypeLabels).map(([value, label]) => (
              <option key={value} value={value}>{label}</option>
            ))}
          </select>
          {errors.accountType && <p className="text-red-600 text-sm">{errors.accountType.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Saldo Atual</label>
          <input
            type="text"
            value={new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(currentBalance)}
            disabled
            className="w-full border rounded p-2 bg-gray-100"
          />
          <p className="text-sm text-gray-500 mt-1">O saldo é atualizado automaticamente pelas transações</p>
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="isActive"
            {...register('isActive')}
            className="rounded"
          />
          <label htmlFor="isActive" className="text-sm font-medium">Conta Ativa</label>
        </div>

        <div className="flex gap-4">
          <button
            type="submit"
            disabled={isSubmitting}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 disabled:opacity-50"
          >
            {isSubmitting ? 'Salvando...' : 'Salvar'}
          </button>
          <button
            type="button"
            onClick={() => router.push('/finance/accounts')}
            className="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400"
          >
            Cancelar
          </button>
        </div>
      </form>
    </div>
  );
}

export default function EditAccountPage() {
  return (
    <Suspense fallback={<div className="p-8">Carregando...</div>}>
      <EditAccountContent />
    </Suspense>
  );
}
