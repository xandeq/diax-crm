'use client';

import { Download, Trash2, X } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface TableActionsProps<TData> {
  selectedCount: number;
  selectedRows: TData[];
  onDelete?: () => void;
  onExport?: () => void;
  onClearSelection: () => void;
  customActions?: React.ReactNode;
}

export function TableActions<TData>({
  selectedCount,
  selectedRows,
  onDelete,
  onExport,
  onClearSelection,
  customActions,
}: TableActionsProps<TData>) {
  if (selectedCount === 0) return null;

  return (
    <div className="flex items-center justify-between px-4 py-3 bg-blue-50 border border-blue-200 rounded-lg mb-4 animate-in slide-in-from-top-2 duration-200">
      <div className="flex items-center gap-4">
        <span className="text-sm font-medium text-blue-900">
          {selectedCount} {selectedCount === 1 ? 'item selecionado' : 'itens selecionados'}
        </span>

        <div className="flex items-center gap-2">
          {onDelete && (
            <Button
              variant="outline"
              size="sm"
              onClick={onDelete}
              className="h-8 border-red-200 bg-white hover:bg-red-50 text-red-600"
            >
              <Trash2 className="h-3.5 w-3.5 mr-1.5" />
              Deletar
            </Button>
          )}

          {onExport && (
            <Button
              variant="outline"
              size="sm"
              onClick={onExport}
              className="h-8 border-blue-200 bg-white hover:bg-blue-50 text-blue-600"
            >
              <Download className="h-3.5 w-3.5 mr-1.5" />
              Exportar
            </Button>
          )}

          {customActions}
        </div>
      </div>

      <Button
        variant="ghost"
        size="sm"
        onClick={onClearSelection}
        className="h-8 text-slate-500 hover:text-slate-700"
      >
        <X className="h-4 w-4" />
      </Button>
    </div>
  );
}
