'use client';

import { CustomerFormDialog } from '@/components/customers/CustomerFormDialog';
import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
import { EmailTimeline } from '@/components/EmailTimeline';
import { EngagementBadge } from '@/components/EngagementBadge';
import { ContactProfilePanel } from '@/components/customers/ContactProfilePanel';
import { CustomerTicketsPanel } from '@/components/helpdesk/CustomerTicketsPanel';
import {
  Avatar,
  ChannelIcons,
  FilterChip,
  GridColumn,
  PerfectGrid,
  SEGMENT_FILTER_OPTIONS,
  SegmentBadge,
  SOURCE_FILTER_OPTIONS,
  SourceLabel,
  StatusBadge,
  useDebounce,
} from '@/components/data-table/PerfectGrid';
import { TableActions } from '@/components/data-table/TableActions';
import { LeadTimeline } from '@/components/customers/LeadTimeline';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { exportToCSV } from '@/lib/export';
import { navigateToWhatsAppSend, normalizePhoneBR } from '@/lib/whatsapp-navigation';
import {
  Customer,
  CustomerStatus,
  deleteCustomer,
  getCustomers,
  updateCustomer,
} from '@/services/customers';
import {
  Activity,
  ChevronDown,
  ChevronUp,
  Download,
  Edit2,
  Filter,
  Loader2,
  Mail,
  MessageCircle,
  Plus,
  Search,
  Trash2,
  X,
} from 'lucide-react';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';

// ── Status Chips Config ──────────────────────────────────────────────────────

const CUSTOMER_STATUS_CHIPS = [
  { value: 'all', label: 'Todos' },
  { value: '4', label: 'Cliente' },
  { value: '5', label: 'Inativo' },
  { value: '6', label: 'Churn' },
];

// ── Page Component ───────────────────────────────────────────────────────────

export default function CustomersPage() {
  const router = useRouter();

  const queryClient = useQueryClient();

  // List state
  const [selectedRows, setSelectedRows] = useState<Customer[]>([]);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  // Filters
  const [searchInput, setSearchInput] = useState('');
  const debouncedSearch = useDebounce(searchInput, 300);
  const [statusFilter, setStatusFilter] = useState('all');

  // Advanced Filters
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [hasEmailFilter, setHasEmailFilter] = useState<boolean | undefined>(undefined);
  const [hasWhatsAppFilter, setHasWhatsAppFilter] = useState<boolean | undefined>(undefined);
  const [personTypeFilter, setPersonTypeFilter] = useState<number | undefined>(undefined);
  const [sourceFilter, setSourceFilter] = useState<number | undefined>(undefined);
  const [segmentFilter, setSegmentFilter] = useState<number | undefined>(undefined);

  // Sorting (server-side)
  const [sortBy, setSortBy] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  // Modal / Form
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isComposerOpen, setIsComposerOpen] = useState(false);
  const [composerRecipients, setComposerRecipients] = useState<
    Array<{ id: string; name: string; email: string }>
  >([]);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const { showConfirm, confirmDialogNode } = useConfirmDialog();

  // Timeline panel
  const [timelineCustomer, setTimelineCustomer] = useState<Customer | null>(null);

  // ── Data Fetching ────────────────────────────────────────────────────────

  const queryParams = {
    page,
    pageSize,
    search: debouncedSearch || undefined,
    status: statusFilter === 'all' ? undefined : (Number(statusFilter) as CustomerStatus),
    sortBy: sortBy || undefined,
    sortDescending: sortDirection === 'desc',
    hasEmail: hasEmailFilter,
    hasWhatsApp: hasWhatsAppFilter,
    personType: personTypeFilter,
    source: sourceFilter,
    segment: segmentFilter,
  };

  const { data: customersData, isLoading: loading } = useQuery({
    queryKey: ['customers', queryParams],
    queryFn: () => getCustomers(queryParams),
  });

  const customers = customersData?.items ?? [];
  const totalPages = customersData?.totalPages ?? 1;
  const totalCount = customersData?.totalCount ?? 0;

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter, hasEmailFilter, hasWhatsAppFilter, personTypeFilter, sourceFilter, segmentFilter]);

  // Clear selection on page change
  useEffect(() => {
    setSelectedRows([]);
  }, [page, pageSize]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['customers'] });

  // ── Handlers ─────────────────────────────────────────────────────────────

  const advancedFilterCount =
    (hasEmailFilter !== undefined ? 1 : 0) +
    (hasWhatsAppFilter !== undefined ? 1 : 0) +
    (personTypeFilter !== undefined ? 1 : 0) +
    (sourceFilter !== undefined ? 1 : 0) +
    (segmentFilter !== undefined ? 1 : 0);

  const clearFilters = () => {
    setSearchInput('');
    setStatusFilter('all');
    setSortBy(null);
    setSortDirection('asc');
    setHasEmailFilter(undefined);
    setHasWhatsAppFilter(undefined);
    setPersonTypeFilter(undefined);
    setSourceFilter(undefined);
    setSegmentFilter(undefined);
  };

  const filtersActive =
    searchInput !== '' ||
    statusFilter !== 'all' ||
    sortBy !== null ||
    hasEmailFilter !== undefined ||
    hasWhatsAppFilter !== undefined ||
    personTypeFilter !== undefined ||
    sourceFilter !== undefined ||
    segmentFilter !== undefined;

  const handleSort = (columnId: string, direction: 'asc' | 'desc') => {
    setSortBy(columnId);
    setSortDirection(direction);
    setPage(1);
  };

  const handleOpenCreate = () => {
    setEditingCustomer(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (customer: Customer) => {
    setEditingCustomer(customer);
    setIsModalOpen(true);
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCustomer(id),
    onSuccess: () => invalidate(),
    onError: () => toast.error('Erro ao excluir cliente.'),
  });

  const handleDelete = (id: string) => {
    showConfirm('Tem certeza que deseja excluir este cliente?', () => deleteMutation.mutate(id));
  };

  const handleBulkDelete = () => {
    showConfirm(`Deletar ${selectedRows.length} cliente(s)?`, async () => {
      try {
        for (const customer of selectedRows) {
          await deleteCustomer(customer.id);
        }
        invalidate();
        setSelectedRows([]);
      } catch {
        toast.error('Erro ao deletar clientes.');
      }
    });
  };

  const handleWhatsApp = (customer: Customer) => {
    if (!customer.phone && !customer.whatsApp) {
      toast.warning('Este cliente não possui número de telefone/WhatsApp cadastrado.');
      return;
    }
    navigateToWhatsAppSend(router, {
      contactId: customer.id,
      contactName: customer.name,
      contactPhone: customer.whatsApp || customer.phone || '',
      contactEmail: customer.email,
      contactCompany: customer.companyName,
    });
  };

  const handleEmail = (customer: Customer) => {
    setComposerRecipients([
      { id: customer.id, name: customer.name, email: customer.email },
    ]);
    setIsComposerOpen(true);
  };

  const handleBulkEmail = () => {
    if (selectedRows.length === 0) return;
    setComposerRecipients(
      selectedRows.map((c) => ({ id: c.id, name: c.name, email: c.email }))
    );
    setIsComposerOpen(true);
  };

  const handleExport = () => {
    const dataToExport = selectedRows.length > 0 ? selectedRows : customers;
    exportToCSV(
      dataToExport.map((c) => ({
        Nome: c.name,
        Email: c.email || '',
        Telefone: c.phone || '',
        WhatsApp: c.whatsApp || '',
        Empresa: c.companyName || '',
        Documento: c.document || '',
        Status: CustomerStatus[c.status],
        'Tipo Pessoa': c.personType === 0 ? 'Física' : 'Jurídica',
        'Criado em': new Date(c.createdAt).toLocaleDateString('pt-BR'),
      })),
      `clientes-${new Date().toISOString().split('T')[0]}`
    );
  };

  // ── Column Definitions ───────────────────────────────────────────────────

  const columns: GridColumn<Customer>[] = useMemo(
    () => [
      {
        id: 'name',
        header: 'Cliente',
        sortable: true,
        cell: (row) => (
          <div className="flex items-center gap-3">
            <Avatar name={row.name} />
            <div className="min-w-0">
              <p className="font-medium text-zinc-100 truncate">{row.name}</p>
              {row.document && (
                <p className="text-xs text-zinc-400 truncate">{row.document}</p>
              )}
            </div>
          </div>
        ),
      },
      {
        id: 'email',
        header: 'Email',
        sortable: true,
        cell: (row) => (
          <span className="text-sm text-zinc-300 truncate block max-w-[220px]">
            {row.email || <span className="text-zinc-500 italic">Sem email</span>}
          </span>
        ),
      },
      {
        id: 'phone',
        header: 'Telefone',
        cell: (row) => (
          <div>
            <span className="text-sm text-zinc-300">{normalizePhoneBR(row.phone) || '–'}</span>
            {row.whatsApp && (
              <span className="text-xs text-emerald-400 flex items-center gap-1 mt-0.5">
                <span className="inline-block w-1.5 h-1.5 bg-emerald-500 rounded-full animate-pulse" />
                WA: {normalizePhoneBR(row.whatsApp)}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'companyName',
        header: 'Empresa',
        sortable: true,
        cell: (row) => (
          <span className="text-sm text-zinc-300">
            {row.companyName || '–'}
          </span>
        ),
      },
      {
        id: 'segment',
        header: 'Segmento',
        sortable: true,
        cell: (row) => <SegmentBadge segment={row.segment} />,
      },
      {
        id: 'channels',
        header: 'Canais',
        cell: (row) => (
          <ChannelIcons
            hasEmail={!!row.email}
            hasWhatsApp={!!(row.whatsApp || row.phone)}
            emailOptOut={row.emailOptOut}
            whatsAppOptOut={row.whatsAppOptOut}
          />
        ),
      },
      {
        id: 'engagement',
        header: 'Engajamento',
        cell: (row) => (
          <EngagementBadge
            customerId={row.id}
            hasEmail={!!row.email && !row.emailOptOut}
            compact={true}
          />
        ),
      },
      {
        id: 'source',
        header: 'Origem',
        sortable: true,
        cell: (row) => <SourceLabel source={row.source} sourceDescription={row.sourceDescription} />,
      },
      {
        id: 'status',
        header: 'Status',
        sortable: true,
        cell: (row) => <StatusBadge status={row.status} />,
      },
      {
        id: 'createdAt',
        header: 'Criado em',
        sortable: true,
        cell: (row) => (
          <span className="text-sm text-zinc-400">
            {new Date(row.createdAt).toLocaleDateString('pt-BR')}
          </span>
        ),
      },
      {
        id: 'lastActivity',
        header: 'Última Atividade',
        sortable: true,
        cell: (row) => {
          const lastActivity = row.lastContactAt || row.lastEmailSentAt;
          if (!lastActivity) {
            return <span className="text-sm text-zinc-500">Nunca</span>;
          }
          const lastActivityDate = new Date(lastActivity);
          const now = new Date();
          const diffDays = Math.floor((now.getTime() - lastActivityDate.getTime()) / 86400000);

          let displayText = '';
          if (diffDays === 0) displayText = 'Hoje';
          else if (diffDays === 1) displayText = 'Ontem';
          else if (diffDays < 7) displayText = `há ${diffDays}d`;
          else if (diffDays < 30) displayText = `há ${Math.floor(diffDays/7)}sem`;
          else displayText = `há ${Math.floor(diffDays/30)}mês`;

          return (
            <div className="text-sm" title={lastActivityDate.toLocaleString('pt-BR')}>
              <span className="text-zinc-300">📧 {displayText}</span>
            </div>
          );
        },
      },
      {
        id: 'actions',
        header: '',
        headerClassName: 'w-[1%]',
        className: 'w-[1%] whitespace-nowrap',
        cell: (row) => (
          <div className="flex items-center gap-0.5" onClick={(e) => e.stopPropagation()}>
            <button
              onClick={() => handleOpenEdit(row)}
              className="p-1.5 hover:bg-white/5 rounded-lg text-zinc-400 hover:text-zinc-100 transition-colors"
              title="Editar"
            >
              <Edit2 className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleWhatsApp(row)}
              disabled={!row.phone && !row.whatsApp}
              className="p-1.5 hover:bg-emerald-500/10 rounded-lg text-emerald-400 hover:text-emerald-300 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
              title="Enviar WhatsApp"
            >
              <MessageCircle className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleEmail(row)}
              className="p-1.5 hover:bg-blue-500/10 rounded-lg text-blue-400 hover:text-blue-300 transition-colors"
              title="Enviar Email"
            >
              <Mail className="h-4 w-4" />
            </button>
            <button
              onClick={() => setTimelineCustomer(row)}
              className="p-1.5 hover:bg-indigo-500/10 rounded-lg text-indigo-400 hover:text-indigo-300 transition-colors"
              title="Histórico"
            >
              <Activity className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleDelete(row.id)}
              className="p-1.5 hover:bg-red-500/10 rounded-lg text-red-400 hover:text-red-300 transition-colors"
              title="Excluir"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        ),
      },
    ],
    []
  );

  // ── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="space-y-4 px-4 py-5 sm:px-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-zinc-100">
            Clientes
          </h1>
          <p className="text-zinc-400">Gerencie sua base de clientes.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleExport} className="border-white/10 text-zinc-300 hover:bg-white/5">
            <Download className="mr-2 h-4 w-4" /> Exportar
          </Button>
          <Button onClick={handleOpenCreate} className="bg-[#00D4AA] hover:bg-[#00B894] text-[#0B1410] font-semibold">
            <Plus className="mr-2 h-4 w-4" /> Novo Cliente
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="space-y-3">
        <div className="flex gap-3 items-center">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
            <Input
              placeholder="Buscar por nome, email ou documento..."
              className="pl-10 h-10 border-white/10 bg-white/5 text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-[#00D4AA]"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <Button
            variant="outline"
            onClick={() => setShowAdvancedFilters((v) => !v)}
            className={`shrink-0 transition-colors ${
              advancedFilterCount > 0 
                ? 'border-[#00D4AA] text-[#00D4AA] bg-[#00D4AA]/5 hover:bg-[#00D4AA]/10' 
                : 'border-white/10 text-zinc-300 hover:bg-white/5'
            }`}
          >
            <Filter className="h-4 w-4 mr-1.5" />
            Filtros
            {advancedFilterCount > 0 && (
              <span className="ml-1.5 bg-[#00D4AA] text-[#0B1410] text-xs font-semibold rounded-full h-5 w-5 flex items-center justify-center">
                {advancedFilterCount}
              </span>
            )}
            {showAdvancedFilters ? (
              <ChevronUp className="h-4 w-4 ml-1" />
            ) : (
              <ChevronDown className="h-4 w-4 ml-1" />
            )}
          </Button>
          {filtersActive && (
            <Button
              variant="ghost"
              onClick={clearFilters}
              className="text-zinc-400 hover:text-zinc-200 shrink-0 hover:bg-white/5"
            >
              <X className="h-4 w-4 mr-1.5" />
              Limpar
            </Button>
          )}
        </div>

        {/* Status Chips */}
        <div className="flex gap-2 flex-wrap">
          {CUSTOMER_STATUS_CHIPS.map((chip) => (
            <FilterChip
              key={chip.value}
              label={chip.label}
              active={statusFilter === chip.value}
              onClick={() => setStatusFilter(chip.value)}
            />
          ))}
        </div>

        {/* Advanced Filters Panel */}
        {showAdvancedFilters && (
          <div className="bg-white/[0.02] border border-white/10 rounded-lg p-4 space-y-4 animate-in slide-in-from-top-2 duration-200">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {/* Has Email */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-zinc-300">Possui Email</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Qualquer"
                    active={hasEmailFilter === undefined}
                    onClick={() => setHasEmailFilter(undefined)}
                  />
                  <FilterChip
                    label="Sim"
                    active={hasEmailFilter === true}
                    onClick={() => setHasEmailFilter(true)}
                  />
                  <FilterChip
                    label="Não"
                    active={hasEmailFilter === false}
                    onClick={() => setHasEmailFilter(false)}
                  />
                </div>
              </div>

              {/* Has WhatsApp */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-zinc-300">Possui WhatsApp</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Qualquer"
                    active={hasWhatsAppFilter === undefined}
                    onClick={() => setHasWhatsAppFilter(undefined)}
                  />
                  <FilterChip
                    label="Sim"
                    active={hasWhatsAppFilter === true}
                    onClick={() => setHasWhatsAppFilter(true)}
                  />
                  <FilterChip
                    label="Não"
                    active={hasWhatsAppFilter === false}
                    onClick={() => setHasWhatsAppFilter(false)}
                  />
                </div>
              </div>

              {/* Person Type */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-zinc-300">Tipo Pessoa</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Todos"
                    active={personTypeFilter === undefined}
                    onClick={() => setPersonTypeFilter(undefined)}
                  />
                  <FilterChip
                    label="Física"
                    active={personTypeFilter === 0}
                    onClick={() => setPersonTypeFilter(0)}
                  />
                  <FilterChip
                    label="Jurídica"
                    active={personTypeFilter === 1}
                    onClick={() => setPersonTypeFilter(1)}
                  />
                </div>
              </div>
            </div>

            {/* Origem + Segmento filters */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-white/10">
              {/* Origem */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-zinc-300">Origem</label>
                <div className="flex gap-2 flex-wrap">
                  {SOURCE_FILTER_OPTIONS.map((opt) => (
                    <FilterChip
                      key={opt.label}
                      label={opt.label}
                      active={sourceFilter === opt.value}
                      onClick={() => setSourceFilter(opt.value)}
                    />
                  ))}
                </div>
              </div>

              {/* Segmento */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-zinc-300">Segmento</label>
                <div className="flex gap-2 flex-wrap">
                  {SEGMENT_FILTER_OPTIONS.map((opt) => (
                    <FilterChip
                      key={opt.label}
                      label={opt.label}
                      active={segmentFilter === opt.value}
                      onClick={() => setSegmentFilter(opt.value)}
                    />
                  ))}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Bulk Actions */}
      <TableActions
        selectedCount={selectedRows.length}
        selectedRows={selectedRows}
        onDelete={handleBulkDelete}
        onExport={handleExport}
        onClearSelection={() => setSelectedRows([])}
        customActions={
          <Button
            size="sm"
            onClick={handleBulkEmail}
            className="h-8 bg-blue-600 hover:bg-blue-700 text-white"
          >
            <Mail className="h-3.5 w-3.5 mr-1.5" />
            Enviar E-mail
          </Button>
        }
      />

      {/* Grid */}
      <PerfectGrid
        columns={columns}
        data={customers}
        loading={loading}
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPage(1);
        }}
        sortColumn={sortBy}
        sortDirection={sortDirection}
        onSort={handleSort}
        selectable
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        onRowClick={(customer) => setTimelineCustomer(customer)}
        getRowId={(c) => c.id}
        itemLabel="clientes"
        emptyTitle="Nenhum cliente encontrado"
        emptyDescription="Tente limpar os filtros ou buscar por outro termo."
      />

      {/* Customer Form Dialog */}
      <CustomerFormDialog
        open={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSuccess={invalidate}
        editingCustomer={editingCustomer}
      />

      {/* Email Composer */}
      <EmailCampaignComposerModal
        open={isComposerOpen}
        onOpenChange={setIsComposerOpen}
        recipients={composerRecipients}
        title="Composer Profissional - Clientes"
        onQueued={(count) => {
          toast.success(`Campanha enfileirada com sucesso para ${count} cliente(s).`);
          setSelectedRows([]);
        }}
      />

      {/* Timeline Sheet */}
      <Sheet
        open={!!timelineCustomer}
        onOpenChange={(open) => {
          if (!open) setTimelineCustomer(null);
        }}
      >
        <SheetContent className="w-full sm:max-w-3xl overflow-y-auto">
          <SheetHeader className="mb-4">
            {timelineCustomer && (
              <div>
                <SheetTitle>Detalhes do Cliente</SheetTitle>
                <div className="flex items-center gap-3 mt-3">
                  <Avatar name={timelineCustomer.name} size="sm" />
                  <div>
                    <p className="text-sm font-medium text-zinc-100">
                      {timelineCustomer.name}
                    </p>
                    <p className="text-xs text-zinc-400">
                      {timelineCustomer.email || timelineCustomer.companyName || ''}
                    </p>
                  </div>
                </div>
              </div>
            )}
          </SheetHeader>
          {timelineCustomer && (
            <Tabs defaultValue="profile" className="w-full">
              <TabsList className="grid w-full grid-cols-4">
                <TabsTrigger value="profile">Perfil</TabsTrigger>
                <TabsTrigger value="activities">Atividades</TabsTrigger>
                <TabsTrigger value="emails">Emails</TabsTrigger>
                <TabsTrigger value="tickets">Suporte</TabsTrigger>
              </TabsList>
              <TabsContent value="profile" className="mt-4">
                <ContactProfilePanel
                  customer={timelineCustomer}
                  onEdit={() => {
                    setEditingCustomer(timelineCustomer);
                    setTimelineCustomer(null);
                  }}
                  onSendEmail={() => {
                    setComposerRecipients([timelineCustomer]);
                    setIsComposerOpen(true);
                    setTimelineCustomer(null);
                  }}
                  onStatusChange={async (newStatus) => {
                    try {
                      await updateCustomer(timelineCustomer.id, {
                        ...timelineCustomer,
                        status: newStatus,
                      });
                      setTimelineCustomer({ ...timelineCustomer, status: newStatus });
                      invalidate();
                    } catch (error) {
                      toast.error('Erro ao atualizar status');
                    }
                  }}
                />
              </TabsContent>
              <TabsContent value="activities" className="mt-4">
                <LeadTimeline customerId={timelineCustomer.id} />
              </TabsContent>
              <TabsContent value="emails" className="mt-4">
                <EmailTimeline customerId={timelineCustomer.id} days={90} />
              </TabsContent>
              <TabsContent value="tickets" className="mt-4">
                <CustomerTicketsPanel customerId={timelineCustomer.id} customerName={timelineCustomer.name} />
              </TabsContent>
            </Tabs>
          )}
        </SheetContent>
      </Sheet>

      {confirmDialogNode}
    </div>
  );
}
