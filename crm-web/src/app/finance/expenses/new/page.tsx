'use client';

import { CreditCard, financeService, PaymentMethod } from '@/services/finance';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function NewExpensePage() {
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [creditCards, setCreditCards] = useState<CreditCard[]>([]);

  const [formData, setFormData] = useState({
    description: '',
    amount: 0,
    date: new Date().toISOString().split('T')[0],
    paymentMethod: PaymentMethod.Pix,
    category: '',
    isRecurring: false,
    creditCardId: ''
  });

  useEffect(() => {
    financeService.getCreditCards().then(setCreditCards).catch(console.error);
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await financeService.createExpense({
        ...formData,
        amount: Number(formData.amount),
        paymentMethod: Number(formData.paymentMethod),
        creditCardId: formData.paymentMethod === PaymentMethod.CreditCard && formData.creditCardId ? formData.creditCardId : undefined
      });
      router.push('/finance/expenses');
    } catch (err) {
      setError('Erro ao criar despesa');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Nova Despesa</h1>

      {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-6">{error}</div>}

      <form onSubmit={handleSubmit} className="bg-white shadow rounded-lg p-6 space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">Descrição</label>
          <input
            type="text"
            required
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
            value={formData.description}
            onChange={e => setFormData({...formData, description: e.target.value})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Valor</label>
          <input
            type="number"
            step="0.01"
            required
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
            value={formData.amount}
            onChange={e => setFormData({...formData, amount: Number(e.target.value)})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Data</label>
          <input
            type="date"
            required
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
            value={formData.date}
            onChange={e => setFormData({...formData, date: e.target.value})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Categoria</label>
          <input
            type="text"
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
            value={formData.category}
            onChange={e => setFormData({...formData, category: e.target.value})}
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700">Método de Pagamento</label>
          <select
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
            value={formData.paymentMethod}
            onChange={e => setFormData({...formData, paymentMethod: Number(e.target.value)})}
          >
            <option value={PaymentMethod.CreditCard}>Cartão de Crédito</option>
            <option value={PaymentMethod.DebitCard}>Cartão de Débito</option>
            <option value={PaymentMethod.Pix}>Pix</option>
            <option value={PaymentMethod.Cash}>Dinheiro</option>
            <option value={PaymentMethod.BankTransfer}>Transferência Bancária</option>
            <option value={PaymentMethod.Boleto}>Boleto</option>
            <option value={PaymentMethod.Other}>Outro</option>
          </select>
        </div>

        {Number(formData.paymentMethod) === PaymentMethod.CreditCard && (
          <div>
            <label className="block text-sm font-medium text-gray-700">Cartão de Crédito</label>
            <select
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-red-500 focus:ring-red-500"
              value={formData.creditCardId}
              onChange={e => setFormData({...formData, creditCardId: e.target.value})}
              required
            >
              <option value="">Selecione um cartão</option>
              {creditCards.map(card => (
                <option key={card.id} value={card.id}>{card.name} (Final {card.lastFourDigits})</option>
              ))}
            </select>
          </div>
        )}

        <div className="flex items-center">
          <input
            type="checkbox"
            className="h-4 w-4 text-red-600 focus:ring-red-500 border-gray-300 rounded"
            checked={formData.isRecurring}
            onChange={e => setFormData({...formData, isRecurring: e.target.checked})}
          />
          <label className="ml-2 block text-sm text-gray-900">Recorrente</label>
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
            className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-red-600 hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 disabled:opacity-50"
          >
            {loading ? 'Salvando...' : 'Salvar'}
          </button>
        </div>
      </form>
    </div>
  );
}
