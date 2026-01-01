'use client';

import Link from 'next/link';

export default function FinancePage() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold mb-6">Financeiro</h1>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Link href="/finance/incomes" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <h2 className="text-xl font-semibold mb-2 text-green-600">Receitas</h2>
          <p className="text-gray-600">Gerenciar entradas financeiras</p>
        </Link>
        <Link href="/finance/expenses" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <h2 className="text-xl font-semibold mb-2 text-red-600">Despesas</h2>
          <p className="text-gray-600">Gerenciar saídas financeiras</p>
        </Link>
        <Link href="/finance/credit-cards" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <h2 className="text-xl font-semibold mb-2 text-blue-600">Cartões de Crédito</h2>
          <p className="text-gray-600">Gerenciar cartões e faturas</p>
        </Link>
      </div>
    </div>
  );
}
