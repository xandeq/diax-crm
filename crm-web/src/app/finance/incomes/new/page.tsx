'use client';
import { IncomeForm } from '@/components/finance/income-form';

export default function NewIncomePage() {
  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Nova Receita</h1>
      <div className="bg-white shadow rounded-lg p-6">
        <IncomeForm />
      </div>
    </div>
  );
}
