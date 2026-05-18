'use client';

import { useCreditCards } from '@/hooks/finance';
import Link from 'next/link';

export default function CreditCardsPage() {
  const { data: creditCards = [], isLoading, isError } = useCreditCards();

  if (isLoading) return <div className="p-8">Carregando...</div>;
  if (isError) return <div className="p-8 text-red-600">Erro ao carregar cartões de crédito</div>;

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold">Cartões de Crédito</h1>
        <Link href="/finance/credit-cards/new" className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
          Novo Cartão
        </Link>
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Nome</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Final</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Limite</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Fechamento</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Vencimento</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {creditCards.map((card) => (
              <tr key={card.id} className="hover:bg-gray-50 transition-colors cursor-pointer">
                <td className="px-6 py-4 whitespace-nowrap">
                  <Link href={`/finance/credit-cards/details?id=${card.id}`} className="text-blue-600 hover:text-blue-800 font-medium block">
                    {card.name}
                  </Link>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">**** {card.lastFourDigits}</td>
                <td className="px-6 py-4 whitespace-nowrap text-blue-600 font-medium">
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(card.limit)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">Dia {card.closingDay}</td>
                <td className="px-6 py-4 whitespace-nowrap">Dia {card.dueDay}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
