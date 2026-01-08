'use client';

import { IncomeForm } from '@/components/finance/income-form';
import { financeService, Income } from '@/services/finance';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';

export default function EditIncomePage({ params }: { params: { id: string } }) {
  const [income, setIncome] = useState<Income | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadIncome();
  }, []);

  const loadIncome = async () => {
    try {
      const data = await financeService.getIncomeById(params.id);
      setIncome(data);
    } catch (err) {
      setError('Não foi possível carregar os dados da receita.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="p-8 flex justify-center"><Loader2 className="h-8 w-8 animate-spin text-green-600" /></div>;
  if (error) return <div className="p-8 text-red-600 font-medium">{error}</div>;

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Editar Receita</h1>
      <div className="bg-white shadow rounded-lg p-6">
        {income && <IncomeForm initialData={income} isEditing />}
      </div>
    </div>
  );
}
