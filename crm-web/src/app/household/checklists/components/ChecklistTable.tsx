'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from '@/components/ui/input';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import { formatCurrency } from '@/lib/utils';
import { checklistService } from '@/services/checklistService';
import {
    ChecklistCategory,
    ChecklistItem,
    ChecklistItemsQuery,
    ChecklistItemStatus,
    ChecklistPriority,
    PagedResponse
} from '@/types/household';
import {
    AlertTriangle,
    Archive,
    ArrowUpDown,
    Calendar,
    CheckCircle2,
    ChevronLeft,
    ChevronRight,
    Circle,
    ExternalLink,
    FilterX,
    Flame,
    FolderInput,
    MoreVertical,
    Search,
    Tags,
    Trash2,
    TrendingDown
} from 'lucide-react';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useState } from 'react';
import { ChecklistDialog } from './ChecklistDialog';

interface ChecklistTableProps {
  categoryId: string | null;
  refreshTrigger: number;
  onRefresh: () => void;
  categories: ChecklistCategory[];
}

export function ChecklistTable({ categoryId, refreshTrigger, onRefresh, categories }: ChecklistTableProps) {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const [data, setData] = useState<PagedResponse<ChecklistItem> | null>(null);
  const [loading, setLoading] = useState(true);

  const [query, setQuery] = useState<ChecklistItemsQuery>(() => ({
    page: Number(searchParams.get('page')) || 1,
    pageSize: Number(searchParams.get('pageSize')) || 20,
    sortBy: searchParams.get('sortBy') || 'createdAt',
    sortDir: (searchParams.get('sortDir') as 'asc' | 'desc') || 'desc',
    includeArchived: searchParams.get('includeArchived') === 'true',
    q: searchParams.get('q') || '',
    status: searchParams.get('status') !== null ? Number(searchParams.get('status')) as ChecklistItemStatus : undefined,
    priority: searchParams.get('priority') !== null ? Number(searchParams.get('priority')) as ChecklistPriority : undefined,
    dateFrom: searchParams.get('dateFrom') || undefined,
    dateTo: searchParams.get('dateTo') || undefined,
  }));

  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [editingItem, setEditingItem] = useState<ChecklistItem | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(searchParams.toString());
    if (query.page && query.page !== 1) params.set('page', query.page.toString()); else params.delete('page');
    if (query.sortBy && query.sortBy !== 'createdAt') params.set('sortBy', query.sortBy); else params.delete('sortBy');
    if (query.sortDir && query.sortDir !== 'desc') params.set('sortDir', query.sortDir); else params.delete('sortDir');
    if (query.includeArchived) params.set('includeArchived', 'true'); else params.delete('includeArchived');
    if (query.q) params.set('q', query.q); else params.delete('q');
    if (query.status !== undefined) params.set('status', query.status.toString()); else params.delete('status');
    if (query.priority !== undefined) params.set('priority', query.priority.toString()); else params.delete('priority');
    if (query.dateFrom) params.set('dateFrom', query.dateFrom); else params.delete('dateFrom');
    if (query.dateTo) params.set('dateTo', query.dateTo); else params.delete('dateTo');

    const newPath = `${pathname}?${params.toString()}`;
    if (newPath !== `${pathname}?${searchParams.toString()}`) {
      router.replace(newPath);
    }
  }, [query, pathname, router, searchParams]);

  useEffect(() => {
    loadItems();
  }, [categoryId, query, refreshTrigger]);

  const loadItems = async () => {
    setLoading(true);
    try {
      const response = await checklistService.getItems({
        ...query,
        categoryId: categoryId || undefined
      });
      setData(response);
    } catch (error) {
      console.error('Erro ao carregar itens:', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleSort = (field: string) => {
    setQuery(prev => ({
      ...prev,
      sortBy: field,
      sortDir: prev.sortBy === field && prev.sortDir === 'asc' ? 'desc' : 'asc',
      page: 1
    }));
  };

  const handleToggleStatus = async (item: ChecklistItem) => {
    try {
      if (item.status === ChecklistItemStatus.ToBuy || item.status === ChecklistItemStatus.PartiallyPaid) {
        await checklistService.markBought(item.id);
      } else {
        await checklistService.reactivate(item.id);
      }
      onRefresh();
    } catch (error) {
      alert('Erro ao atualizar status');
    }
  };

  const handleBulkAction = async (action: any, extraData?: any) => {
    if (selectedIds.length === 0) return;
    try {
      await checklistService.bulkAction({
        ids: selectedIds,
        action: action,
        ...extraData
      });
      setSelectedIds([]);
      onRefresh();
    } catch (error) {
      alert('Erro ao executar ação em massa');
    }
  };

  const handleSelectAll = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.checked && data) {
      setSelectedIds(data.items.map(i => i.id));
    } else {
      setSelectedIds([]);
    }
  };

  const handleSelectOne = (id: string) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
    );
  };

  const getPriorityIcon = (priority: ChecklistPriority) => {
    switch (priority) {
      case ChecklistPriority.Urgent: return <Flame className="h-4 w-4 text-red-600" />;
      case ChecklistPriority.High: return <AlertTriangle className="h-4 w-4 text-orange-500" />;
      case ChecklistPriority.Medium: return <TrendingDown className="h-4 w-4 text-blue-500 rotate-180" />;
      case ChecklistPriority.Low: return <TrendingDown className="h-4 w-4 text-slate-400" />;
    }
  };

  const getStatusBadge = (status: ChecklistItemStatus) => {
    switch (status) {
      case ChecklistItemStatus.ToBuy: return <Badge variant="outline" className="text-slate-500 border-slate-200 bg-slate-50">Pendente</Badge>;
      case ChecklistItemStatus.PartiallyPaid: return <Badge variant="outline" className="text-amber-600 border-amber-200 bg-amber-50">Parcial</Badge>;
      case ChecklistItemStatus.Bought: return <Badge variant="outline" className="text-green-600 border-green-200 bg-green-50">Pago</Badge>;
      case ChecklistItemStatus.Canceled: return <Badge variant="outline" className="text-red-500 border-red-200 bg-red-50">Cancelado</Badge>;
      case ChecklistItemStatus.Archived: return <Badge variant="outline" className="text-slate-400 border-slate-200 bg-slate-100">Arquivado</Badge>;
    }
  };

  const currentTotalPaid = data?.items.reduce((acc, item) => acc + (item.paidAmount || (item.status === ChecklistItemStatus.Bought ? item.estimatedPrice || 0 : 0)), 0) || 0;
  const currentTotalPending = data?.items.reduce((acc, item) => acc + Math.max(0, (item.estimatedPrice || 0) - (item.paidAmount || (item.status === ChecklistItemStatus.Bought ? item.estimatedPrice || 0 : 0))), 0) || 0;

  return (
    <div className="flex flex-col">
      <div className="p-4 border-b border-slate-100 flex flex-wrap items-center justify-between gap-4 bg-slate-50/50">
        <div className="flex flex-wrap items-center gap-2 flex-1 min-w-0">
          <div className="relative w-full md:w-64">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Buscar itens..."
              className="pl-10 bg-white h-10"
              value={query.q || ''}
              onChange={(e) => setQuery(prev => ({ ...prev, q: e.target.value, page: 1 }))}
            />
          </div>

          <Select
            value={query.status?.toString() || "all"}
            onValueChange={(val) => setQuery(prev => ({ ...prev, status: val === "all" ? undefined : Number(val), page: 1 }))}
          >
            <SelectTrigger className="w-full md:w-40 bg-white h-10">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todos Status</SelectItem>
              <SelectItem value={ChecklistItemStatus.ToBuy.toString()}>Pendente</SelectItem>
              <SelectItem value={ChecklistItemStatus.PartiallyPaid.toString()}>Parcialmente Pago</SelectItem>
              <SelectItem value={ChecklistItemStatus.Bought.toString()}>Pago</SelectItem>
              <SelectItem value={ChecklistItemStatus.Canceled.toString()}>Cancelado</SelectItem>
              <SelectItem value={ChecklistItemStatus.Archived.toString()}>Arquivado</SelectItem>
            </SelectContent>
          </Select>

          <Select
            value={query.priority?.toString() || "all"}
            onValueChange={(val) => setQuery(prev => ({ ...prev, priority: val === "all" ? undefined : Number(val), page: 1 }))}
          >
            <SelectTrigger className="w-full md:w-36 bg-white h-10">
              <SelectValue placeholder="Prioridade" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Todas Prioridades</SelectItem>
              <SelectItem value={ChecklistPriority.Low.toString()}>Baixa</SelectItem>
              <SelectItem value={ChecklistPriority.Medium.toString()}>Média</SelectItem>
              <SelectItem value={ChecklistPriority.High.toString()}>Alta</SelectItem>
              <SelectItem value={ChecklistPriority.Urgent.toString()}>Urgente</SelectItem>
            </SelectContent>
          </Select>

          <div className="flex items-center gap-1 bg-white border rounded-md px-2 h-10 text-slate-600">
            <Calendar className="h-4 w-4 text-slate-400" />
            <input
              type="date"
              className="text-xs md:text-sm outline-none bg-transparent"
              value={query.dateFrom || ''}
              onChange={(e) => setQuery(prev => ({ ...prev, dateFrom: e.target.value, page: 1 }))}
            />
            <span className="text-slate-300">-</span>
            <input
              type="date"
              className="text-xs md:text-sm outline-none bg-transparent"
              value={query.dateTo || ''}
              onChange={(e) => setQuery(prev => ({ ...prev, dateTo: e.target.value, page: 1 }))}
            />
          </div>

          {(query.q || query.status !== undefined || query.priority !== undefined || query.dateFrom || query.dateTo) && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setQuery(prev => ({
                ...prev,
                q: '',
                status: undefined,
                priority: undefined,
                dateFrom: undefined,
                dateTo: undefined,
                page: 1
              }))}
              className="h-10 text-slate-500 hover:text-red-500"
            >
              <FilterX className="h-4 w-4 mr-1" /> Limpar
            </Button>
          )}
        </div>

        <div className="flex items-center gap-2">
          {selectedIds.length > 0 && (
            <div className="flex items-center gap-2 pr-4 border-r border-slate-200 mr-2">
              <span className="text-xs font-medium text-slate-500">{selectedIds.length} selecionados</span>
              <Button size="sm" variant="outline" className="h-8" onClick={() => handleBulkAction('markbought')}>Comprados</Button>
              <Button size="sm" variant="outline" className="h-8" onClick={() => handleBulkAction('archive')}>Arquivar</Button>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button size="sm" variant="outline" className="h-8 gap-1">
                    <Tags className="h-3.5 w-3.5" /> Status
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  <div className="px-2 py-1.5 text-xs font-semibold text-slate-500">Alterar status para...</div>
                  <DropdownMenuItem onClick={() => handleBulkAction('changestatus', { targetStatus: 0 })}>
                    Pendente
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleBulkAction('changestatus', { targetStatus: 4 })}>
                    Parcialmente Pago
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleBulkAction('changestatus', { targetStatus: 1 })}>
                    Pago
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleBulkAction('changestatus', { targetStatus: 2 })}>
                    Cancelado
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleBulkAction('changestatus', { targetStatus: 3 })}>
                    Arquivado
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button size="sm" variant="outline" className="h-8 gap-1">
                    <FolderInput className="h-3.5 w-3.5" /> Mover
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48 max-h-64 overflow-y-auto">
                  <div className="px-2 py-1.5 text-xs font-semibold text-slate-500">Mover para...</div>
                  {categories.map(cat => (
                    <DropdownMenuItem
                      key={cat.id}
                      onClick={() => handleBulkAction('changecategory', { targetCategoryId: cat.id })}
                    >
                      {cat.name}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <Button size="sm" variant="destructive" className="h-8" onClick={() => handleBulkAction('delete')}><Trash2 className="h-3.5 w-3.5" /></Button>
            </div>
          )}

          <Button
            size="sm"
            variant="ghost"
            onClick={() => setQuery(prev => ({ ...prev, includeArchived: !prev.includeArchived, page: 1 }))}
            className={query.includeArchived ? "text-blue-600 bg-blue-50" : "text-slate-500"}
          >
            <Archive className="mr-2 h-4 w-4" /> {query.includeArchived ? "Ocultar Arquivados" : "Ver Arquivados"}
          </Button>
        </div>
      </div>

      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead className="w-12 text-center">
                <input
                  type="checkbox"
                  className="rounded border-slate-300"
                  onChange={handleSelectAll}
                  checked={data?.items.length ? selectedIds.length === data.items.length : false}
                />
              </TableHead>
              <TableHead className="w-10"></TableHead>
              <TableHead className="cursor-pointer hover:text-slate-900" onClick={() => toggleSort('title')}>
                Título <ArrowUpDown className="inline h-3 w-3 ml-1" />
              </TableHead>
              <TableHead>Categoria</TableHead>
              <TableHead className="cursor-pointer hover:text-slate-900" onClick={() => toggleSort('priority')}>
                Prioridade <ArrowUpDown className="inline h-3 w-3 ml-1" />
              </TableHead>
              <TableHead className="cursor-pointer text-right hover:text-slate-900" onClick={() => toggleSort('estimatedPrice')}>
                Total <ArrowUpDown className="inline h-3 w-3 ml-1" />
              </TableHead>
              <TableHead className="text-right">Pago</TableHead>
              <TableHead className="text-right">Restante</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Ações</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={8} className="animate-pulse h-12 bg-slate-50/50"></TableCell>
                </TableRow>
              ))
            ) : data?.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} className="h-32 text-center text-slate-400">
                  Nenhum item encontrado.
                </TableCell>
              </TableRow>
            ) : (
              data?.items.map((item) => (
                <TableRow key={item.id} className={item.isArchived ? "opacity-60 grayscale-[0.5]" : ""}>
                  <TableCell className="text-center">
                    <input
                      type="checkbox"
                      className="rounded border-slate-300"
                      checked={selectedIds.includes(item.id)}
                      onChange={() => handleSelectOne(item.id)}
                    />
                  </TableCell>
                  <TableCell>
                    <button
                      onClick={() => handleToggleStatus(item)}
                      className="hover:scale-110 transition-transform"
                    >
                      {item.status === ChecklistItemStatus.Bought ? (
                        <CheckCircle2 className="h-5 w-5 text-green-500 fill-green-50" />
                      ) : (
                        <Circle className="h-5 w-5 text-slate-300" />
                      )}
                    </button>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-col">
                      <span className={`font-medium ${item.status === ChecklistItemStatus.Bought ? "line-through text-slate-400" : "text-slate-900"}`}>
                        {item.title}
                        {item.quantity > 1 && <span className="ml-2 px-1.5 py-0.5 bg-slate-100 text-slate-500 rounded text-[10px] font-bold">x{item.quantity}</span>}
                      </span>
                      {item.description && <span className="text-xs text-slate-500 truncate max-w-[200px]">{item.description}</span>}
                    </div>
                  </TableCell>
                  <TableCell>
                    <span className="text-xs bg-slate-100 px-2 py-1 rounded text-slate-600">
                      {item.categoryName}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5">
                      {getPriorityIcon(item.priority)}
                      <span className="text-xs text-slate-600">
                        {ChecklistPriority[item.priority]}
                      </span>
                    </div>
                  </TableCell>
                  <TableCell className="text-right">
                    <span className="text-sm font-mono text-slate-600">
                      {item.estimatedPrice ? formatCurrency(item.estimatedPrice) : "-"}
                    </span>
                  </TableCell>
                  <TableCell className="text-right">
                    <span className="text-sm font-mono text-green-600">
                      {item.paidAmount || item.status === ChecklistItemStatus.Bought ? formatCurrency(item.paidAmount || item.estimatedPrice || 0) : "-"}
                    </span>
                  </TableCell>
                  <TableCell className="text-right">
                    <span className="text-sm font-mono text-slate-500">
                      {item.estimatedPrice ? formatCurrency(Math.max(0, item.estimatedPrice - (item.paidAmount || (item.status === ChecklistItemStatus.Bought ? item.estimatedPrice : 0)))) : "-"}
                    </span>
                  </TableCell>
                  <TableCell>
                    {getStatusBadge(item.status)}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      {item.storeOrLink && (
                        <a href={item.storeOrLink} target="_blank" rel="noreferrer" className="p-2 hover:bg-slate-100 rounded-lg text-slate-400">
                          <ExternalLink className="h-4 w-4" />
                        </a>
                      )}
                      <Button variant="ghost" size="icon" onClick={() => setEditingItem(item)}>
                        <MoreVertical className="h-4 w-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {data && (
        <div className="p-4 border-t border-slate-100 flex flex-wrap items-center justify-between gap-4 bg-slate-50/30">
          <div className="flex items-center gap-3">
            <span className="text-xs text-slate-500 font-medium">
              {data.totalCount} itens
            </span>
            <div className="h-4 w-px bg-slate-300 mx-1"></div>
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2">
                <span className="text-xs text-slate-500">Total Pago:</span>
                <span className="text-sm font-medium text-green-600">{formatCurrency(currentTotalPaid)}</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="text-xs text-slate-500">Restante:</span>
                <span className="text-sm font-medium text-blue-600">{formatCurrency(currentTotalPending)}</span>
              </div>
            </div>
            <div className="h-4 w-px bg-slate-300 mx-1"></div>
            <div className="flex items-center gap-2">
              <span className="text-xs text-slate-500">Exibir:</span>
              <Select
                value={query.pageSize?.toString() || '20'}
                onValueChange={(v) => setQuery(prev => ({ ...prev, pageSize: parseInt(v), page: 1 }))}
              >
                <SelectTrigger className="h-8 w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="20">20</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                  <SelectItem value="100">100</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          {data.totalPages > 1 && (
            <div className="flex items-center gap-1">
              <Button
                variant="outline"
                size="icon"
                className="h-8 w-8"
                disabled={data.page === 1}
                onClick={() => setQuery(prev => ({ ...prev, page: prev.page! - 1 }))}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              {(() => {
                const pages: (number | string)[] = [];
                const current = data.page;
                const total = data.totalPages;
                const delta = 2;
                const range: number[] = [];
                for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
                  range.push(i);
                }
                if (current - delta > 2) {
                  pages.push(1, '...');
                } else {
                  pages.push(1);
                }
                pages.push(...range);
                if (current + delta < total - 1) {
                  pages.push('...', total);
                } else if (total > 1) {
                  pages.push(total);
                }
                return pages.map((p, idx) =>
                  typeof p === 'string' ? (
                    <span key={`ellipsis-${idx}`} className="px-2 text-slate-400">...</span>
                  ) : (
                    <Button
                      key={p}
                      variant={p === current ? 'default' : 'outline'}
                      size="icon"
                      className="h-8 w-8"
                      onClick={() => setQuery(prev => ({ ...prev, page: p }))}
                    >
                      {p}
                    </Button>
                  )
                );
              })()}
              <Button
                variant="outline"
                size="icon"
                className="h-8 w-8"
                disabled={data.page === data.totalPages}
                onClick={() => setQuery(prev => ({ ...prev, page: prev.page! + 1 }))}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          )}
        </div>
      )}

      {editingItem && (
        <ChecklistDialog
          isOpen={!!editingItem}
          onClose={() => setEditingItem(null)}
          onSave={() => {
            setEditingItem(null);
            onRefresh();
          }}
          categories={categories}
          itemToEdit={editingItem}
        />
      )}
    </div>
  );
}
