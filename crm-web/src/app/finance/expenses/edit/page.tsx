'use client';

import { ExpenseForm } from '@/components/finance/expense-form';
import { Expense, financeService } from '@/services/finance';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

function EditExpenseContent() {
  const searchParams = useSearchParams();
  const id = searchParams.get('id');
  const [expense, setExpense] = useState<Expense | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadExpense() {
      if (!id) return;
      try {
        const data = await financeService.getExpenseById(id);
        setExpense(data);
      } catch (err) {
        setError('Erro ao carregar despesa');
        console.error(err);
      } finally {
        setLoading(false);
      }
    }
    loadExpense();
  }, [id]);

  if (!id) {
    return <div className="p-8 text-red-600">ID da despesa não informado</div>;
  }

  if (loading) {
    return <div className="p-8">Carregando...</div>;
  }

  if (error || !expense) {
    return <div className="p-8 text-red-600">{error || 'Despesa não encontrada'}</div>;
  }

  return <ExpenseForm initialData={expense} isEditing />;
}

export default function EditExpensePage() {
  return (
    <Suspense fallback={<div className="p-8">Carregando...</div>}>
      <EditExpenseContent />
    </Suspense>
  );
}
