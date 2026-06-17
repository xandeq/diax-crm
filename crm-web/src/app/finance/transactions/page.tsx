'use client';

import { FinancialGrid } from '@/components/finance/FinancialGrid';
import { FinancialToolbar } from '@/components/finance/FinancialToolbar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDisplayDate } from '@/lib/date-utils';
import { cn, formatCurrency } from '@/lib/utils';
import {
    useDeleteTransaction,
    useDeleteTransactionsBulk,
    useFinancialAccounts,
    useTransactionCategories,
    useTransactions,
} from '@/hooks/finance';
import {
    Transaction,
    TransactionFilters,
    TransactionStatus,
    TransactionType
} from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowDownCircle, ArrowRightLeft, ArrowUpCircle, ArrowUpDown, Edit, EyeOff, Plus, Receipt, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';


const typeLabels: Record<TransactionType, string> = {
  [TransactionType.Income]: 'Receita',
  [TransactionType.Expense]: 'Despesa',
  [TransactionType.Transfer]: 'Transferência',
  [TransactionType.Ignored]: 'Ignorada',
};

const typeColors: Record<TransactionType, string> = {
  [TransactionType.Income]: 'text-emerald-400',
  [TransactionType.Expense]: 'text-rose-400',
  [TransactionType.Transfer]: 'text-sky-400',
  [TransactionType.Ignored]: 'text-zinc-500',
};

const typeBadgeColors: Record<TransactionType, string> = {
  [TransactionType.Income]: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400',
  [TransactionType.Expense]: 'bg-rose-500/10 border-rose-500/20 text-rose-400',
  [TransactionType.Transfer]: 'bg-sky-500/10 border-sky-500/20 text-sky-400',
  [TransactionType.Ignored]: 'bg-zinc-800/50 border-zinc-700/50 text-zinc-400',
};

const TypeIcon = ({ type }: { type: TransactionType }) => {
  switch (type) {
    case TransactionType.Income: return <ArrowUpCircle className="h-4 w-4 text-emerald-400" />;
    case TransactionType.Expense: return <ArrowDownCircle className="h-4 w-4 text-rose-400" />;
    case TransactionType.Transfer: return <ArrowRightLeft className="h-4 w-4 text-sky-400" />;
    case TransactionType.Ignored: return <EyeOff className="h-4 w-4 text-zinc-500" />;
  }
};

interface DeleteModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  loading: boolean;
  count: number;
}

function DeleteModal({ isOpen, onClose, onConfirm, loading, count }: DeleteModalProps) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="p-8 rounded-2xl max-w-md w-full mx-4 border border-white/10 bg-gradient-to-br from-[#0B1510] to-[#0F1A14]">
        <h3 className="text-xl font-bold mb-2 text-zinc-100">Confirmar Exclusão</h3>
        <p className="mb-8 text-zinc-400">
          Tem certeza que deseja excluir {count > 1 ? `estas ${count} transações` : 'esta transação'}?
          Esta ação pode impactar seu saldo bancário e não pode ser desfeita.
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={loading} className="px-6 rounded-xl border border-white/5 hover:bg-white/5 text-zinc-300">Cancelar</Button>
          <Button onClick={onConfirm} disabled={loading} variant="destructive" className="px-6 rounded-xl font-semibold">
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
    <div className="flex flex-wrap gap-1.5 p-1 rounded-xl bg-white/[0.04] border border-white/5 w-fit">
      {tabs.map(tab => {
        const isActive = activeType === tab.value;
        return (
          <Button
            key={tab.label}
            variant="ghost"
            size="sm"
            onClick={() => onChange(tab.value)}
            className={cn(
              "gap-2 px-3.5 py-1.5 h-9 rounded-lg text-xs font-semibold transition-all duration-200",
              isActive
                ? "bg-emerald-500/15 border border-emerald-500/25 text-[#00D4AA] shadow-sm shadow-emerald-500/5 hover:bg-emerald-500/20 hover:text-[#00D4AA]"
                : "text-zinc-400 hover:text-zinc-200 hover:bg-white/5 border border-transparent"
            )}
          >
            <tab.icon className="h-3.5 w-3.5" />
            {tab.label}
          </Button>
        );
      })}
    </div>
  );
}

export default function TransactionsPage() {
  const [filters, setFilters] = useState<TransactionFilters>({
    page: 1,
    pageSize: 15,
    sortBy: 'date',
    sortDescending: true,
  });
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({});
  const [isBulkDeleting, setIsBulkDeleting] = useState(false);

  const { data, isLoading: loading } = useTransactions(filters);
  const { data: categories = [] } = useTransactionCategories();
  const { data: accounts = [] } = useFinancialAccounts();
  const deleteMutation = useDeleteTransaction();
  const bulkDeleteMutation = useDeleteTransactionsBulk();

  const selectedIds = useMemo(() => {
    return Object.keys(rowSelection).filter(key => rowSelection[key]);
  }, [rowSelection]);

  useEffect(() => {
    setRowSelection({});
  }, [filters]);

  const handleDelete = async () => {
    if (selectedIds.length > 0) {
      try {
        const response = await bulkDeleteMutation.mutateAsync(selectedIds);
        setRowSelection({});
        setIsBulkDeleting(false);
        if (response.failedCount > 0) {
          toast.warning(`${response.deletedCount} transação(ões) excluída(s). ${response.failedCount} não encontrada(s).`);
        } else {
          toast.success(`${response.deletedCount} transação(ões) excluída(s).`);
        }
      } catch {
        toast.error('Erro ao excluir transações em massa.');
      }
      return;
    }

    if (!deleteId) return;
    try {
      await deleteMutation.mutateAsync(deleteId);
      toast.success('Transação excluída.');
    } catch {
      toast.error('Erro ao excluir transação.');
    } finally {
      setDeleteId(null);
    }
  };

  const isDeleting = deleteMutation.isPending || bulkDeleteMutation.isPending;

  const columns = useMemo<ColumnDef<Transaction>[]>(() => [
    {
      id: 'selection',
      header: ({ table }) => (
        <input
          type="checkbox"
          className="h-4 w-4 rounded border-white/10 bg-white/5 text-accent focus:ring-accent focus:ring-offset-0"
          checked={table.getIsAllPageRowsSelected()}
          onChange={table.getToggleAllPageRowsSelectedHandler()}
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          className="h-4 w-4 rounded border-white/10 bg-white/5 text-accent focus:ring-accent focus:ring-offset-0"
          checked={row.getIsSelected()}
          onChange={row.getToggleSelectedHandler()}
        />
      ),
    },
    {
      accessorKey: 'type',
      header: 'Tipo',
      cell: ({ row }) => (
        <Badge variant="outline" className={`${typeBadgeColors[row.original.type]} rounded-lg font-semibold border gap-1.5`}>
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
          className="hover:bg-transparent p-0 flex items-center gap-2 text-zinc-400 font-semibold hover:text-zinc-200"
        >
          Data
          <ArrowUpDown className="h-4 w-4" />
        </Button>
      ),
      cell: ({ row }) => <span className="font-medium text-zinc-400">{formatDisplayDate(row.original.date)}</span>,
    },
    {
      accessorKey: 'description',
      header: 'Descrição',
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-semibold text-zinc-100">{row.original.description}</span>
          {row.original.categoryName && (
            <span className="text-xs text-zinc-400 uppercase tracking-wider font-semibold mt-0.5">{row.original.categoryName}</span>
          )}
          {row.original.rawDescription && row.original.rawDescription !== row.original.description && (
            <span className="text-xs text-zinc-500 italic truncate max-w-[250px]" title={row.original.rawDescription}>
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
          <Badge variant="outline" className="bg-sky-500/10 border-sky-500/20 text-sky-400 font-semibold rounded-lg">
            {row.original.financialAccountName}
          </Badge>
        ) : row.original.creditCardName ? (
          <Badge variant="outline" className="bg-purple-500/10 border-purple-500/20 text-purple-400 font-semibold rounded-lg">
            💳 {row.original.creditCardName}
          </Badge>
        ) : <span className="text-zinc-500">—</span>
      ),
    },
    {
      accessorKey: 'amount',
      header: () => (
        <Button
          variant="ghost"
          onClick={() => setFilters(f => ({ ...f, sortBy: 'amount', sortDescending: f.sortBy === 'amount' ? !f.sortDescending : true }))}
          className="hover:bg-transparent p-0 flex items-center gap-2 text-zinc-400 font-semibold hover:text-zinc-200"
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
          <Badge className="bg-emerald-500/10 text-emerald-400 border-emerald-500/20 rounded-lg font-semibold">Pago</Badge>
        ) : (
          <Badge className="bg-amber-500/10 text-amber-400 border-amber-500/20 rounded-lg font-semibold">Pendente</Badge>
        );
      },
    },
    {
      id: 'actions',
      header: 'Ações',
      cell: ({ row }) => (
        <div className="flex items-center gap-2 text-right">
          <Link href={`/finance/transactions/edit?id=${row.original.id}`}>
            <Button variant="ghost" size="icon" className="h-9 w-9 text-zinc-400 hover:text-[#00D4AA] hover:bg-emerald-500/10 rounded-lg border border-transparent hover:border-emerald-500/20 transition-all duration-200">
              <Edit className="h-4 w-4" />
            </Button>
          </Link>
          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 text-zinc-400 hover:text-rose-400 hover:bg-rose-500/10 rounded-lg border border-transparent hover:border-rose-500/20 transition-all duration-200"
            onClick={() => setDeleteId(row.original.id)}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ], [filters]);

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
          <h1 className="text-3xl font-extrabold text-zinc-100 tracking-tight flex items-center gap-3">
            <div className="bg-indigo-500/10 p-2 rounded-xl border border-indigo-500/20">
              <Receipt className="h-8 w-8 text-indigo-400" />
            </div>
            Transações
          </h1>
          <p className="text-zinc-400 mt-1.5">
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
            <Button className="bg-[#00D4AA] hover:bg-[#00D4AA]/80 text-black gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl font-bold">
              <Plus size={20} strokeWidth={3} />
              <span>Nova Transação</span>
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
        categories={categories}
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
