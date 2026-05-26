'use client';
import { TransactionForm } from '@/components/finance/transaction-form';

export default function NewTransactionPage() {
  return (
    <div className="p-8 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">Nova Transação</h1>
      <div className="rounded-lg p-6" style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.09)' }}>
        <TransactionForm />
      </div>
    </div>
  );
}
