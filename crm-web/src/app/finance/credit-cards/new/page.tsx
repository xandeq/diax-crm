'use client';

import { financeService } from '@/services/finance';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

export default function NewCreditCardPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: '',
    lastFourDigits: '',
    closingDay: 1,
    dueDay: 10,
    limit: 0
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await financeService.createCreditCard({
        ...formData,
        limit: Number(formData.limit),
        closingDay: Number(formData.closingDay),
        dueDay: Number(formData.dueDay)
      });
      router.push('/finance/credit-cards');
    } catch (err) {
      setError('Erro ao criar cartão de crédito');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Novo Cartão de Crédito</h1>

      {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-6">{error}</div>}

      <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6 space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">Nome do Cartão</label>
          <input
            type="text"
            required
            placeholder="Ex: Nubank, Visa Platinum"
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            value={formData.name}
            onChange={e => setFormData({...formData, name: e.target.value})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Últimos 4 dígitos</label>
          <input
            type="text"
            required
            maxLength={4}
            pattern="\d{4}"
            placeholder="1234"
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            value={formData.lastFourDigits}
            onChange={e => setFormData({...formData, lastFourDigits: e.target.value})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Limite</label>
          <input
            type="number"
            step="0.01"
            required
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            value={formData.limit}
            onChange={e => setFormData({...formData, limit: Number(e.target.value)})}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Dia de Fechamento</label>
            <input
              type="number"
              min="1"
              max="31"
              required
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              value={formData.closingDay}
              onChange={e => setFormData({...formData, closingDay: Number(e.target.value)})}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Dia de Vencimento</label>
            <input
              type="number"
              min="1"
              max="31"
              required
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              value={formData.dueDay}
              onChange={e => setFormData({...formData, dueDay: Number(e.target.value)})}
            />
          </div>
        </div>

        <div className="flex justify-end pt-4">
          <button
            type="button"
            onClick={() => router.back()}
            className="mr-4 px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={loading}
            className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50"
          >
            {loading ? 'Salvando...' : 'Salvar'}
          </button>
        </div>
      </form>
    </div>
  );
}
