'use client';

import {
    ColumnDef,
    flexRender,
    getCoreRowModel,
    useReactTable,
} from "@tanstack/react-table";

import { Button } from "@/components/ui/button";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { ChevronLeft, ChevronRight, Loader2 } from "lucide-react";

interface FinancialGridProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  pageCount: number;
  page: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  loading?: boolean;
  rowSelection?: Record<string, boolean>;
  onRowSelectionChange?: (selection: any) => void;
  getRowId?: (row: TData) => string;
}

const getHeaderLabel = (column: any) => {
  const header = column.columnDef.header;
  if (typeof header === 'string') return header;
  
  switch (column.id) {
    case 'selection': return 'Selecionar';
    case 'date': return 'Data';
    case 'amount': return 'Valor';
    case 'actions': return 'Ações';
    default: return '';
  }
};

export function FinancialGrid<TData, TValue>({
  columns,
  data,
  pageCount,
  page,
  pageSize,
  onPageChange,
  onPageSizeChange,
  loading,
  rowSelection,
  onRowSelectionChange,
  getRowId,
}: FinancialGridProps<TData, TValue>) {
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    pageCount: pageCount,
    onRowSelectionChange: onRowSelectionChange,
    getRowId: getRowId,
    state: {
      rowSelection: rowSelection || {},
    },
  });

  return (
    <div className="space-y-4">
      <div className="rounded-xl overflow-hidden border border-white/5 bg-white/[0.03]">
        <Table className="responsive-table">
          <TableHeader className="bg-white/[0.04]">
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent border-b border-white/5">
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead key={header.id} className="font-bold py-4 text-zinc-400">
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext()
                          )}
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-64">
                   <div className="flex flex-col items-center justify-center gap-2 text-zinc-400">
                      <Loader2 className="h-8 w-8 animate-spin text-[#00D4AA]" />
                      <p className="font-semibold text-sm">Carregando dados...</p>
                   </div>
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  className="transition-colors border-b border-white/5 last:border-0 hover:bg-white/[0.02]"
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell
                      key={cell.id}
                      className="py-4"
                      data-label={getHeaderLabel(cell.column)}
                      data-checkbox={cell.column.id === 'selection' ? 'true' : undefined}
                    >
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-64 text-center text-zinc-400 font-semibold">
                  Nenhum registro encontrado.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2 py-2">
        <div className="text-sm font-semibold text-zinc-400">
           Página <span className="text-zinc-200">{page}</span> de <span className="text-zinc-200">{pageCount || 1}</span>
        </div>

        <div className="flex items-center gap-6">
          <div className="flex items-center gap-2">
            <p className="text-sm font-semibold text-zinc-400">Linhas por página</p>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="h-9 w-16 rounded-lg px-2 py-1 text-sm outline-none transition-all border border-white/10 bg-white/5 text-zinc-300"
            >
              {[10, 20, 50].map((size) => (
                <option key={size} value={size} className="bg-[#0B1510] text-zinc-300">
                  {size}
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              className="h-9 w-9 p-0 rounded-lg border-white/10 bg-white/5 hover:bg-white/10 text-zinc-300 hover:text-zinc-100"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1 || loading}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-9 w-9 p-0 rounded-lg border-white/10 bg-white/5 hover:bg-white/10 text-zinc-300 hover:text-zinc-100"
              onClick={() => onPageChange(page + 1)}
              disabled={page >= pageCount || loading}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
