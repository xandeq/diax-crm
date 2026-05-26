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
      <div className="rounded-xl overflow-hidden" style={{ border: '1px solid rgba(255,255,255,0.08)', background: 'rgba(255,255,255,0.03)' }}>
        <Table>
          <TableHeader style={{ background: 'rgba(255,255,255,0.04)' }}>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id} className="hover:bg-transparent" style={{ borderBottom: '1px solid rgba(255,255,255,0.07)' }}>
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead key={header.id} className="font-semibold py-4" style={{ color: '#9CA3AF' }}>
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
                   <div className="flex flex-col items-center justify-center gap-2 text-muted-foreground">
                      <Loader2 className="h-8 w-8 animate-spin text-accent" />
                      <p>Carregando dados...</p>
                   </div>
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  className="transition-colors last:border-0"
                  style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id} className="py-4">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={columns.length} className="h-64 text-center text-muted-foreground">
                  Nenhum registro encontrado.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2 py-2">
        <div className="text-sm font-medium" style={{ color: '#9CA3AF' }}>
           Página <span style={{ color: '#F9FAFB' }}>{page}</span> de <span style={{ color: '#F9FAFB' }}>{pageCount || 1}</span>
        </div>

        <div className="flex items-center gap-6">
          <div className="flex items-center gap-2">
            <p className="text-sm font-medium" style={{ color: '#9CA3AF' }}>Linhas por página</p>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="h-9 w-16 rounded-lg px-2 py-1 text-sm outline-none transition-all"
              style={{ border: '1px solid rgba(255,255,255,0.12)', background: 'rgba(255,255,255,0.06)', color: '#D1D5DB' }}
            >
              {[10, 20, 50].map((size) => (
                <option key={size} value={size}>
                  {size}
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              className="h-9 w-9 p-0 rounded-lg"
              onClick={() => onPageChange(page - 1)}
              disabled={page <= 1 || loading}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-9 w-9 p-0 rounded-lg"
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
