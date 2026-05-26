'use client';

import { useCreditCards } from '@/hooks/finance';
import Link from 'next/link';

export default function CreditCardsPage() {
  const { data: creditCards = [], isLoading, isError } = useCreditCards();

  if (isLoading) return <div className="p-8" style={{ color: '#9CA3AF' }}>Carregando...</div>;
  if (isError) return <div className="p-8 text-red-400">Erro ao carregar cartões de crédito</div>;

  return (
    <div className="p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold" style={{ color: '#F9FAFB' }}>Cartões de Crédito</h1>
        <Link href="/finance/credit-cards/new" className="px-4 py-2 rounded font-medium text-sm" style={{ background: 'rgba(59,130,246,0.15)', color: '#60a5fa', border: '1px solid rgba(59,130,246,0.25)' }}>
          Novo Cartão
        </Link>
      </div>

      <div className="rounded-lg overflow-hidden" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
        <table className="min-w-full">
          <thead style={{ background: 'rgba(255,255,255,0.04)', borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Nome</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Final</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Limite</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Fechamento</th>
              <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider" style={{ color: '#9CA3AF' }}>Vencimento</th>
            </tr>
          </thead>
          <tbody>
            {creditCards.map((card) => (
              <tr
                key={card.id}
                className="transition-colors cursor-pointer"
                style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}
                onMouseEnter={e => (e.currentTarget.style.background = 'rgba(255,255,255,0.03)')}
                onMouseLeave={e => (e.currentTarget.style.background = '')}
              >
                <td className="px-6 py-4 whitespace-nowrap">
                  <Link href={`/finance/credit-cards/details?id=${card.id}`} className="font-medium block" style={{ color: '#60a5fa' }}>
                    {card.name}
                  </Link>
                </td>
                <td className="px-6 py-4 whitespace-nowrap" style={{ color: '#D1D5DB' }}>**** {card.lastFourDigits}</td>
                <td className="px-6 py-4 whitespace-nowrap font-medium" style={{ color: '#60a5fa' }}>
                  {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(card.limit)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap" style={{ color: '#D1D5DB' }}>Dia {card.closingDay}</td>
                <td className="px-6 py-4 whitespace-nowrap" style={{ color: '#D1D5DB' }}>Dia {card.dueDay}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
