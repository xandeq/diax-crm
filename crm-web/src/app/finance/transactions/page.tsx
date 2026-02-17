'use client';

import { FinancialGrid } from '@/components/finance/FinancialGrid';
import { FinancialToolbar } from '@/components/finance/FinancialToolbar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDisplayDate } from '@/lib/date-utils';
import { formatCurrency } from '@/lib/utils';
import {
    financeService,
    FinancialAccount,
    PagedResponse,
    Transaction,
    TransactionCategory,
    TransactionFilters,
    TransactionStatus,
    TransactionType
} from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowDownCircle, ArrowRightLeft, ArrowUpCircle, ArrowUpDown, Edit, EyeOff, Plus, Receipt, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';

const typeLabels: Record<TransactionType, string> = {
  [TransactionType.Income]: 'Receita',
  [TransactionType.Expense]: 'Despesa',
  [TransactionType.Transfer]: 'Transferência',
  [TransactionType.Ignored]: 'Ignorada',
};

const typeColors: Record<TransactionType, string> = {
  [TransactionType.Income]: 'text-green-600',
  [TransactionType.Expense]: 'text-red-600',
  [TransactionType.Transfer]: 'text-blue-600',
  [TransactionType.Ignored]: 'text-gray-400',
};

const typeBadgeColors: Record<TransactionType, string> = {
  [TransactionType.Income]: 'bg-green-50 border-green-200 text-green-700',
  [TransactionType.Expense]: 'bg-red-50 border-red-200 text-red-700',
  [TransactionType.Transfer]: 'bg-blue-50 border-blue-200 text-blue-700',
  [TransactionType.Ignored]: 'bg-gray-50 border-gray-200 text-gray-500',
};

const TypeIcon = ({ type }: { type: TransactionType }) => {
  switch (type) {
    case TransactionType.Income: return <ArrowUpCircle className="h-4 w-4 text-green-500" />;
    case TransactionType.Expense: return <ArrowDownCircle className="h-4 w-4 text-red-500" />;
    case TransactionType.Transfer: return <ArrowRightLeft className="h-4 w-4 text-blue-500" />;
    case TransactionType.Ignored: return <EyeOff className="h-4 w-4 text-gray-400" />;
  }
};

function DeleteModal({ isOpen, onClose, onConfirm, loading, count }: any) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white p-8 rounded-2xl shadow-2xl max-w-md w-full mx-4 border border-gray-100">
        <h3 className="text-xl font-bold text-gray-900 mb-2">Confirmar Exclusão</h3>
        <p className="text-gray-600 mb-8">
          Tem certeza que deseja excluir {count > 1 ? `estas ${count} transações` : 'esta transação'}?
          Esta ação pode impactar seu saldo bancário e não pode ser desfeita.
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={loading} className="px-6 rounded-xl">Cancelar</Button>
          <Button onClick={onConfirm} disabled={loading} variant="destructive" className="px-6 rounded-xl">
            {loading ? 'Excluindo...' : 'Sim, Excluir'}
          </Button>
        </div>
      </div>
    </div>
  );
}

function TypeFilterTabs({ activeType, onChange }: { activeType: TransactionType | null; onChange: (t: TransactionType | null) => void }) {
  const tabs = [
    { label: 'Todas', value: null, icon: Receipt },
    { label: 'Receitas', value: TransactionType.Income, icon: ArrowUpCircle },
    { label: 'Despesas', value: TransactionType.Expense, icon: ArrowDownCircle },
    { label: 'Transferências', value: TransactionType.Transfer, icon: ArrowRightLeft },
    { label: 'Ignoradas', value: TransactionType.Ignored, icon: EyeOff },
  ];

  return (
    <div className="flex flex-wrap gap-2">
      {tabs.map(tab => {
        const isActive = activeType === tab.value;
        return (
          <Button
            key={tab.label}
            variant={isActive ? 'default' : 'outline'}
            size="sm"
            onClick={() => onChange(tab.value)}
            className={`gap-2 rounded-lg ${isActive ? 'bg-accent text-white' : ''}`}
          >
            <tab.icon className="h-4 w-4" />
            {tab.label}
          </Button>
        );
      })}
    </div>
  );
}

export default function TransactionsPage() {
  const [data, setData] = useState<PagedResponse<Transaction> | null>(null);
  const [filters, setFilters] = useState<TransactionFilters>({
    page: 1,
    pageSize: 15,
    sortBy: 'date',
    sortDescending: true,
  });
  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<TransactionCategory[]>([]);
  const [accounts, setAccounts] = useState<FinancialAccount[]>([]);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({});
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  const selectedIds = useMemo(() => {
    return Object.keys(rowSelection).filter(key => rowSelection[key]);
  }, [rowSelection]);

  useEffect(() => {
    Promise.all([
      financeService.getAllTransactionCategories(),
      financeService.getFinancialAccounts(),
    ]).then(([cats, accs]) => {
      setCategories(cats);
      setAccounts(accs);
    }).catch(console.error);
  }, []);

  useEffect(() => {
    loadTransactions();
    setRowSelection({});
  }, [filters]);

  const loadTransactions = async () => {
    setLoading(true);
    try {
      const response = await financeService.getTransactions(filters);
      setData(response);
    } catch (err) {
      console.error('Erro ao carregar transações:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (selectedIds.length > 0) {
      setIsDeleting(true);
      try {
        const response = await financeService.deleteTransactionsBulk(selectedIds);
        setRowSelection({});
        setIsBulkDeleting(false);
        if (response.failedCount > 0) {
          alert(`${response.deletedCount} transação(ões) excluída(s). ${response.failedCount} não encontrada(s).`);
        }
      } catch (err: any) {
        alert('Erro ao excluir transações em massa.');
      } finally {
        setIsDeleting(false);
        loadTransactions();
      }
      return;
    }

    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await financeService.deleteTransaction(deleteId);
    } catch (err: any) {
      alert(err?.status === 404
        ? 'Transação não encontrada. A lista será atualizada.'
        : 'Erro ao excluir transação.');
    } finally {
      setDeleteId(null);
      setIsDeleting(false);
      loadTransactions();
    }
  };

  const columns = useMemo<ColumnDef<Transaction>[]>(() => [
    {
      id: 'selection',
      header: ({ table }) => (
        <input
          type="checkbox"
          className="h-4 w-4 rounded border-gray-300 text-accent focus:ring-accent"
          checked={table.getIsAllPageRowsSelected()}
          onChange={table.getToggleAllPageRowsSelectedHandler()}
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          className="h-4 w-4 rounded border-gray-300 text-accent focus:ring-accent"
          checked={row.getIsSelected()}
          onChange={row.getToggleSelectedHandler()}
        />
      ),
    },
    {
      accessorKey: 'type',
      header: 'Tipo',
      cell: ({ row }) => (
        <Badge variant="outline" className={`${typeBadgeColors[row.original.type]} rounded-lg font-medium gap-1.5`}>
          <TypeIcon type={row.original.type} />
          {typeLabels[row.original.type]}
        </Badge>
      ),
    },
    {
      accessorKey: 'date',
      header: () => (
        <Button
          variant="ghost"
          onClick={() => setFilters(f => ({ ...f, sortBy: 'date', sortDescending: f.sortBy === 'date' ? !f.sortDescending : true }))}
          className="hover:bg-transparent p-0 flex items-center gap-2"
        >
          Data
          <ArrowUpDown className="h-4 w-4" />
        </Button>
      ),
      cell: ({ row }) => <span className="font-medium text-gray-600">{formatDisplayDate(row.original.date)}</span>,
    },
    {
      accessorKey: 'description',
      header: 'Descrição',
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-semibold text-gray-900">{row.original.description}</span>
          {row.original.categoryName && (
            <span className="text-xs text-gray-500 uppercase tracking-wider font-medium">{row.original.categoryName}</span>
          )}
          {row.original.rawDescription && row.original.rawDescription !== row.original.description && (
            <span className="text-xs text-gray-400 italic truncate max-w-[250px]" title={row.original.rawDescription}>
              {row.original.rawDescription}
            </span>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'financialAccountName',
      header: 'Conta',
      cell: ({ row }) => (
        row.original.financialAccountName ? (
          <Badge variant="outline" className="bg-blue-50/50 border-blue-100 text-blue-700 font-medium rounded-lg">
            {row.original.financialAccountName}
          </Badge>
        ) : row.original.creditCardName ? (
          <Badge variant="outline" className="bg-purple-50/50 border-purple-100 text-purple-700 font-medium rounded-lg">
            💳 {row.original.creditCardName}
          </Badge>
        ) : <span className="text-gray-400">—</span>
      ),
    },
    {
      accessorKey: 'amount',
      header: () => (
        <Button
          variant="ghost"
          onClick={() => setFilters(f => ({ ...f, sortBy: 'amount', sortDescending: f.sortBy === 'amount' ? !f.sortDescending : true }))}
          className="hover:bg-transparent p-0 flex items-center gap-2"
        >
          Valor
          <ArrowUpDown className="h-4 w-4" />
        </Button>
      ),
      cell: ({ row }) => (
        <span className={`font-bold text-lg ${typeColors[row.original.type]}`}>
          {row.original.type === TransactionType.Expense ? '- ' : ''}{formatCurrency(row.original.amount)}
        </span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        if (row.original.type !== TransactionType.Expense) return null;
        return row.original.status === TransactionStatus.Paid ? (
          <Badge className="bg-green-100 text-green-700 border-green-200 rounded-lg">Pago</Badge>
        ) : (
          <Badge className="bg-yellow-100 text-yellow-700 border-yellow-200 rounded-lg">Pendente</Badge>
        );
      },
    },
    {
      id: 'actions',
      header: 'Ações',
      cell: ({ row }) => (
        <div className="flex items-center gap-2 text-right">
          <Link href={`/finance/transactions/edit?id=${row.original.id}`}>
            <Button variant="ghost" size="icon" className="h-9 w-9 text-gray-400 hover:text-accent hover:bg-accent/10 rounded-lg">
              <Edit className="h-4 w-4" />
            </Button>
          </Link>
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
            onClick={() => setDeleteId(row.original.id)}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ], [filters]);

  // Map categories to the format expected by FinancialToolbar
  const toolbarCategories = useMemo(() => {
    return categories.map(c => ({ id: c.id, name: c.name, isActive: c.isActive }));
  }, [categories]);

  return (
    <div className="p-8 max-w-[1600px] mx-auto space-y-8">
      <DeleteModal
        isOpen={!!deleteId || isBulkDeleting}
        onClose={() => { setDeleteId(null); setIsBulkDeleting(false); }}
        onConfirm={handleDelete}
        loading={isDeleting}
        count={selectedIds.length > 0 ? selectedIds.length : 1}
      />

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight flex items-center gap-3">
            <div className="bg-indigo-100 p-2 rounded-xl">
              <Receipt className="h-8 w-8 text-indigo-600" />
            </div>
            Transações
          </h1>
          <p className="text-gray-500 mt-1">
            Visão unificada de receitas, despesas, transferências e movimentações ignoradas.
          </p>
        </div>

        <div className="flex items-center gap-3">
          {selectedIds.length > 0 && (
            <Button
              variant="destructive"
              onClick={() => setIsBulkDeleting(true)}
              className="gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl font-bold"
            >
              <Trash2 size={20} />
              Excluir {selectedIds.length}
            </Button>
          )}

          <Link href="/finance/transactions/new">
            <Button className="bg-accent hover:bg-accent-secondary text-white gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl">
              <Plus size={20} strokeWidth={3} />
              <span className="font-bold">Nova Transação</span>
            </Button>
          </Link>
        </div>
      </div>

      {/* Type filter tabs */}
      <TypeFilterTabs
        activeType={filters.type ?? null}
        onChange={(type) => setFilters(f => ({ ...f, type: type ?? undefined, page: 1 }))}
      />

      <FinancialToolbar
        filters={filters}
        onFilterChange={setFilters}
        categories={toolbarCategories}
        accounts={accounts}
      />

      <FinancialGrid
        columns={columns}
        data={data?.items || []}
        pageCount={data?.totalPages || 0}
        page={filters.page || 1}
        pageSize={filters.pageSize || 15}
        onPageChange={(page) => setFilters(f => ({ ...f, page }))}
        onPageSizeChange={(pageSize) => setFilters(f => ({ ...f, pageSize, page: 1 }))}
        loading={loading}
        rowSelection={rowSelection}
        onRowSelectionChange={setRowSelection}
        getRowId={(row: Transaction) => row.id}
      />
    </div>
  );
}
