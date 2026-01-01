'use client';

import { financeService, Income, PaymentMethod } from '@/services/finance';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export default function IncomesPage() {
  const [incomes, setIncomes] = useState<Income[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadIncomes();
  }, []);

  const loadIncomes = async () => {
    try {
      const data = await financeService.getIncomes();
      setIncomes(data);
    } catch (err) {
      setError('Erro ao carregar receitas');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div className="p-8">Carregando...</div>;
  if (error) return <div className="p-8 text-red-600">{error}</div>;

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Receitas</h1>
        <Link href="/finance/incomes/new" className="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700">
          Nova Receita
        </Link>
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Descrição</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Valor</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Data</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Categoria</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Método</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {incomes.map((income) => (
              <tr key={income.id}>
                <td className="px-6 py-4 whitespace-nowrap">{income.description}</td>
                <td className="px-6 py-4 whitespace-nowrap text-green-600 font-medium">
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(income.amount)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  {new Date(income.date).toLocaleDateString('pt-BR')}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">{income.category || '-'}</td>
                <td className="px-6 py-4 whitespace-nowrap">{PaymentMethod[income.paymentMethod]}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
