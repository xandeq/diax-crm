'use client';

import { TransactionForm } from '@/components/finance/transaction-form';
import { financeService, Transaction } from '@/services/finance';
import { Loader2 } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

function EditTransactionContent() {
  const searchParams = useSearchParams();
  const id = searchParams.get('id');
  const [transaction, setTransaction] = useState<Transaction | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (id) {
      loadTransaction(id);
    } else {
      setLoading(false);
      setError('ID não fornecido');
    }
  }, [id]);

  const loadTransaction = async (txId: string) => {
    try {
      const data = await financeService.getTransactionById(txId);
      setTransaction(data);
    } catch (err) {
      setError('Não foi possível carregar os dados da transação.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="p-8 flex justify-center"><Loader2 className="h-8 w-8 animate-spin text-indigo-600" /></div>;
  if (error) return <div className="p-8 text-red-600 font-medium">{error}</div>;

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Editar Transação</h1>
      <div className="bg-white shadow rounded-lg p-6">
        {transaction && <TransactionForm initialData={transaction} isEditing />}
      </div>
    </div>
  );
}

export default function EditTransactionPage() {
  return (
    <Suspense fallback={<div className="p-8 flex justify-center"><Loader2 className="h-8 w-8 animate-spin text-indigo-600" /></div>}>
      <EditTransactionContent />
    </Suspense>
  );
}
