'use client';

import { FinancialGrid } from '@/components/finance/FinancialGrid';
import { FinancialToolbar } from '@/components/finance/FinancialToolbar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatDisplayDate } from '@/lib/date-utils';
import { formatCurrency } from '@/lib/utils';
import { financeService, FinancialAccount, FinancialFilters, Income, IncomeCategory, PagedResponse } from '@/services/finance';
import { ColumnDef } from '@tanstack/react-table';
import { ArrowUpDown, Edit, Plus, Receipt, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';

function DeleteModal({ isOpen, onClose, onConfirm, loading, count }: any) {
  if (!isOpen) return null;
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white p-8 rounded-2xl shadow-2xl max-w-md w-full mx-4 border border-gray-100">
        <h3 className="text-xl font-bold text-gray-900 mb-2">Confirmar Exclusão</h3>
        <p className="text-gray-600 mb-8">
          Tem certeza que deseja excluir {count > 1 ? `estes ${count} itens` : 'esta receita'}?
          Esta ação impactará seu saldo bancário e não pode ser desfeita.
        </p>
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose} disabled={loading} className="px-6 rounded-xl">Cancelar</Button>
          <Button
            onClick={onConfirm}
            disabled={loading}
            variant="destructive"
            className="px-6 rounded-xl"
          >
            {loading ? 'Excluindo...' : 'Sim, Excluir'}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function IncomesPage() {
  const [data, setData] = useState<PagedResponse<Income> | null>(null);
  const [filters, setFilters] = useState<FinancialFilters>({
    page: 1,
    pageSize: 10,
    sortBy: 'date',
    sortDescending: true
  });
  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<IncomeCategory[]>([]);
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
      financeService.getIncomeCategories(),
      financeService.getFinancialAccounts()
    ]).then(([cats, accs]) => {
      setCategories(cats);
      setAccounts(accs);
    }).catch(console.error);
  }, []);

  useEffect(() => {
    loadIncomes();
    setRowSelection({});
  }, [filters]);

  const loadIncomes = async () => {
    setLoading(true);
    try {
      const response = await financeService.getIncomes(filters);
      setData(response);
    } catch (err) {
      console.error('Erro ao carregar receitas:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (selectedIds.length > 0) {
      setIsDeleting(true);
      try {
        const response = await financeService.deleteIncomesBulk(selectedIds);
        setRowSelection({});
        setIsBulkDeleting(false);
        if (response.failedCount > 0) {
          alert(`${response.deletedCount} receita(s) excluída(s). ${response.failedCount} não encontrada(s) — dados foram atualizados.`);
        }
      } catch (err: any) {
        alert(err?.status === 404
          ? 'Registros não encontrados. A lista será atualizada.'
          : 'Erro ao excluir receitas em massa.');
      } finally {
        setIsDeleting(false);
        loadIncomes();
      }
      return;
    }

    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await financeService.deleteIncome(deleteId);
    } catch (err: any) {
      alert(err?.status === 404
        ? 'Receita não encontrada. A lista será atualizada.'
        : 'Erro ao excluir receita.');
    } finally {
      setDeleteId(null);
      setIsDeleting(false);
      loadIncomes();
    }
  };

  const columns = useMemo<ColumnDef<Income>[]>(() => [
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
          <span className="text-xs text-gray-500 uppercase tracking-wider font-medium">{row.original.incomeCategoryName}</span>
        </div>
      ),
    },
    {
      accessorKey: 'financialAccountName',
      header: 'Conta / Origem',
      cell: ({ row }) => (
        <Badge variant="outline" className="bg-blue-50/50 border-blue-100 text-blue-700 font-medium rounded-lg">
          {row.original.financialAccountName}
        </Badge>
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
      cell: ({ row }) => <span className="font-bold text-green-600 text-lg">{formatCurrency(row.original.amount)}</span>,
    },
    {
      id: 'actions',
      header: 'Ações',
      cell: ({ row }) => (
        <div className="flex items-center gap-2 text-right">
          <Link href={`/finance/incomes/edit?id=${row.original.id}`}>
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

  return (
    <div className="p-8 max-w-[1600px] mx-auto space-y-8">
      <DeleteModal
        isOpen={!!deleteId || isBulkDeleting}
        onClose={() => {
          setDeleteId(null);
          setIsBulkDeleting(false);
        }}
        onConfirm={handleDelete}
        loading={isDeleting}
        count={selectedIds.length > 0 ? selectedIds.length : 1}
      />

      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight flex items-center gap-3">
            <div className="bg-green-100 p-2 rounded-xl">
              <Receipt className="h-8 w-8 text-green-600" />
            </div>
            Receitas
          </h1>
          <p className="text-gray-500 mt-1">Gerencie suas entradas e acompanhe seu fluxo de caixa.</p>
        </div>

        <div className="flex items-center gap-3">
          {selectedIds.length > 0 && (
            <Button
              variant="destructive"
              onClick={() => setIsBulkDeleting(true)}
              className="gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl font-bold"
            >
              <Trash2 size={20} />
              Excluir {selectedIds.length} selecionados
            </Button>
          )}

          <Link href="/finance/incomes/new">
            <Button className="bg-accent hover:bg-accent-secondary text-white gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl">
              <Plus size={20} strokeWidth={3} />
              <span className="font-bold">Nova Receita</span>
            </Button>
          </Link>
        </div>
      </div>

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
        pageSize={filters.pageSize || 10}
        onPageChange={(page) => setFilters(f => ({ ...f, page }))}
        onPageSizeChange={(pageSize) => setFilters(f => ({ ...f, pageSize, page: 1 }))}
        loading={loading}
        rowSelection={rowSelection}
        onRowSelectionChange={setRowSelection}
        getRowId={(row: Income) => row.id}
      />
    </div>
  );
}
