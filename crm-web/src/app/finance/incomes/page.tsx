'use client';

import { Button } from '@/components/ui/button';
import { financeService, Income, PaymentMethod } from '@/services/finance';
import { formatDisplayDate } from '@/lib/date-utils';
import { Edit, Plus, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';

function DeleteModal({ isOpen, onClose, onConfirm, loading }: any) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white p-6 rounded-lg shadow-lg max-w-md w-full mx-4">
        <h3 className="text-lg font-bold text-gray-900 mb-2">Confirmar Exclusão</h3>
        <p className="text-gray-600 mb-6">Tem certeza que deseja excluir esta receita? Esta ação não pode ser desfeita.</p>
        <div className="flex justify-end gap-3">
          <button onClick={onClose} className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-md">Cancelar</button>
          <button
            onClick={onConfirm}
            disabled={loading}
            className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
          >
            {loading ? 'Excluindo...' : 'Excluir'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default function IncomesPage() {
  const [incomes, setIncomes] = useState<Income[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const totalIncome = incomes.reduce((sum, income) => sum + income.amount, 0);

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

  const handleDelete = async () => {
    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await financeService.deleteIncome(deleteId);
      setIncomes(incomes.filter(i => i.id !== deleteId));
      setDeleteId(null);
    } catch (err) {
      alert('Erro ao excluir receita.');
    } finally {
      setIsDeleting(false);
    }
  };

  if (loading) return <div className="p-8">Carregando...</div>;
  if (error) return <div className="p-8 text-red-600">{error}</div>;

  return (
    <div className="p-8">
      <DeleteModal
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        onConfirm={handleDelete}
        loading={isDeleting}
      />

      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-800">Receitas</h1>
        <Link href="/finance/incomes/new">
            <Button className="bg-green-600 hover:bg-green-700 text-white gap-2">
                <Plus size={16} /> Nova Receita
            </Button>
        </Link>
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden border border-gray-100">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Descrição</th>
              <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Valor</th>
              <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Data</th>
              <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Categoria</th>
              <th className="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Método</th>
              <th className="px-6 py-3 text-right text-xs font-semibold text-gray-600 uppercase">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {incomes.map((income) => (
              <tr key={income.id} className="hover:bg-gray-50 transition-colors">
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 font-medium">{income.description}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-green-600 font-bold">
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(income.amount)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {formatDisplayDate(income.date)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <span className="px-2 py-1 bg-gray-100 rounded-full text-xs">{income.incomeCategoryName || 'Geral'}</span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{PaymentMethod[income.paymentMethod]}</td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <div className="flex justify-end gap-2">
                    <Link href={`/finance/incomes/edit?id=${income.id}`}>
                      <button className="p-2 text-blue-600 hover:bg-blue-50 rounded-md transition-colors" title="Editar">
                        <Edit size={18} />
                      </button>
                    </Link>
                    <button
                      onClick={() => setDeleteId(income.id)}
                      className="p-2 text-red-600 hover:bg-red-50 rounded-md transition-colors"
                      title="Excluir"
                    >
                      <Trash2 size={18} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            <tr className="bg-emerald-50">
              <td className="px-6 py-4 whitespace-nowrap text-sm font-semibold text-emerald-900">Total</td>
              <td className="px-6 py-4 whitespace-nowrap text-sm font-bold text-emerald-700">
                {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(totalIncome)}
              </td>
              <td className="px-6 py-4" colSpan={4}></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
