'use client';

import { Banknote, Building2, CreditCard, FolderOpen, Settings, TrendingDown, TrendingUp } from 'lucide-react';
import Link from 'next/link';

export default function FinancePage() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold mb-6">Financeiro</h1>

      {/* Main Modules */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <Link href="/finance/incomes" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow border-l-4 border-green-500">
          <div className="flex items-center gap-3 mb-2">
            <TrendingUp className="h-6 w-6 text-green-600" />
            <h2 className="text-xl font-semibold text-green-600">Receitas</h2>
          </div>
          <p className="text-gray-600">Gerenciar entradas financeiras</p>
        </Link>
        <Link href="/finance/expenses" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow border-l-4 border-red-500">
          <div className="flex items-center gap-3 mb-2">
            <TrendingDown className="h-6 w-6 text-red-600" />
            <h2 className="text-xl font-semibold text-red-600">Despesas</h2>
          </div>
          <p className="text-gray-600">Gerenciar saídas financeiras</p>
        </Link>
        <Link href="/finance/credit-cards" className="block p-6 bg-white rounded-lg shadow hover:shadow-md transition-shadow border-l-4 border-blue-500">
          <div className="flex items-center gap-3 mb-2">
            <CreditCard className="h-6 w-6 text-blue-600" />
            <h2 className="text-xl font-semibold text-blue-600">Cartões de Crédito</h2>
          </div>
          <p className="text-gray-600">Gerenciar cartões e faturas</p>
        </Link>
      </div>

      {/* Management Section */}
      <h2 className="text-lg font-semibold mb-4 text-gray-700 flex items-center gap-2">
        <Settings className="h-5 w-5" />
        Gerenciamento
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Link href="/finance/accounts" className="block p-4 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <div className="flex items-center gap-3">
            <Building2 className="h-5 w-5 text-indigo-600" />
            <div>
              <h3 className="font-medium text-gray-900">Contas Financeiras</h3>
              <p className="text-sm text-gray-500">Bancos, carteiras e investimentos</p>
            </div>
          </div>
        </Link>
        <Link href="/finance/categories/income" className="block p-4 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <div className="flex items-center gap-3">
            <FolderOpen className="h-5 w-5 text-green-600" />
            <div>
              <h3 className="font-medium text-gray-900">Categorias de Receita</h3>
              <p className="text-sm text-gray-500">Classificar receitas</p>
            </div>
          </div>
        </Link>
        <Link href="/finance/categories/expense" className="block p-4 bg-white rounded-lg shadow hover:shadow-md transition-shadow">
          <div className="flex items-center gap-3">
            <Banknote className="h-5 w-5 text-red-600" />
            <div>
              <h3 className="font-medium text-gray-900">Categorias de Despesa</h3>
              <p className="text-sm text-gray-500">Classificar despesas</p>
            </div>
          </div>
        </Link>
      </div>
    </div>
  );
}
