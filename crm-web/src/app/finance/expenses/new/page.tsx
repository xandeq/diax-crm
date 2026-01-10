'use client';

import { ExpenseForm } from '@/components/finance/expense-form';

export default function NewExpensePage() {
  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Nova Despesa</h1>
      <div className="bg-white shadow rounded-lg p-6">
        <ExpenseForm />
      </div>
    </div>
  );
}
