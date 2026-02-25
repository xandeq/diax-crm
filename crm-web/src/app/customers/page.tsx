'use client';

import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
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
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { exportToCSV } from '@/lib/export';
import { navigateToWhatsAppSend } from '@/lib/whatsapp-navigation';
import {
  createCustomer,
  Customer,
  CustomerStatus,
  deleteCustomer,
  getCustomers,
  updateCustomer,
} from '@/services/customers';
import { zodResolver } from '@hookform/resolvers/zod';
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
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

// ── Schema ───────────────────────────────────────────────────────────────────

const customerSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido'),
  phone: z.string().optional(),
  companyName: z.string().optional(),
  personType: z.any(),
  document: z.string().optional(),
  whatsApp: z.string().optional(),
  website: z.string().optional(),
  notes: z.string().optional(),
  tags: z.string().optional(),
});

type CustomerFormValues = z.infer<typeof customerSchema>;

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

  // List state
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedRows, setSelectedRows] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
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
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Timeline panel
  const [timelineCustomer, setTimelineCustomer] = useState<Customer | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CustomerFormValues>({
    resolver: zodResolver(customerSchema),
    defaultValues: {
      name: '',
      email: '',
      phone: '',
      companyName: '',
      personType: 0,
      document: '',
      whatsApp: '',
      website: '',
      notes: '',
      tags: '',
    },
  });

  const personTypeWatch = watch('personType');

  // ── Data Fetching ────────────────────────────────────────────────────────

  const fetchCustomers = async () => {
    setLoading(true);
    try {
      const status =
        statusFilter === 'all' ? undefined : (Number(statusFilter) as CustomerStatus);
      const data = await getCustomers({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        status,
        sortBy: sortBy || undefined,
        sortDescending: sortDirection === 'desc',
        hasEmail: hasEmailFilter,
        hasWhatsApp: hasWhatsAppFilter,
        personType: personTypeFilter,
        source: sourceFilter,
        segment: segmentFilter,
      });
      setCustomers(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch {
      setCustomers([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, [page, pageSize, debouncedSearch, statusFilter, sortBy, sortDirection, hasEmailFilter, hasWhatsAppFilter, personTypeFilter, sourceFilter, segmentFilter]);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter, hasEmailFilter, hasWhatsAppFilter, personTypeFilter, sourceFilter, segmentFilter]);

  // Clear selection on page change
  useEffect(() => {
    setSelectedRows([]);
  }, [page, pageSize]);

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
    reset({
      name: '',
      email: '',
      phone: '',
      companyName: '',
      personType: 0,
      document: '',
      whatsApp: '',
      website: '',
      notes: '',
      tags: '',
    });
    setFormError(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (customer: Customer) => {
    setEditingCustomer(customer);
    setValue('name', customer.name);
    setValue('email', customer.email || '');
    setValue('phone', customer.phone || '');
    setValue('companyName', customer.companyName || '');
    setValue('personType', customer.personType);
    setValue('document', customer.document || '');
    setValue('whatsApp', customer.whatsApp || '');
    setValue('website', customer.website || '');
    setValue('notes', customer.notes || '');
    setValue('tags', customer.tags || '');
    setFormError(null);
    setIsModalOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este cliente?')) return;
    try {
      await deleteCustomer(id);
      fetchCustomers();
    } catch {
      alert('Erro ao excluir cliente.');
    }
  };

  const handleBulkDelete = async () => {
    if (!confirm(`Deletar ${selectedRows.length} cliente(s)?`)) return;
    try {
      for (const customer of selectedRows) {
        await deleteCustomer(customer.id);
      }
      fetchCustomers();
      setSelectedRows([]);
    } catch {
      alert('Erro ao deletar clientes.');
    }
  };

  const handleWhatsApp = (customer: Customer) => {
    if (!customer.phone && !customer.whatsApp) {
      alert('Este cliente nao possui numero de telefone/WhatsApp cadastrado.');
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

  const onSubmit = async (data: CustomerFormValues) => {
    setSubmitting(true);
    setFormError(null);
    try {
      const payload = { ...data, personType: Number(data.personType) };
      if (editingCustomer) {
        await updateCustomer(editingCustomer.id, payload);
      } else {
        await createCustomer(payload);
      }
      setIsModalOpen(false);
      fetchCustomers();
    } catch (err) {
      if (err instanceof Error) {
        setFormError(err.message);
      } else {
        setFormError('Erro ao salvar cliente. Verifique os dados.');
      }
    } finally {
      setSubmitting(false);
    }
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
              <p className="font-medium text-slate-900 truncate">{row.name}</p>
              {row.document && (
                <p className="text-xs text-slate-500 truncate">{row.document}</p>
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
          <span className="text-sm text-slate-600 truncate block max-w-[220px]">
            {row.email || <span className="text-slate-400 italic">Sem email</span>}
          </span>
        ),
      },
      {
        id: 'phone',
        header: 'Telefone',
        cell: (row) => (
          <div>
            <span className="text-sm text-slate-600">{row.phone || '–'}</span>
            {row.whatsApp && (
              <span className="text-xs text-green-600 flex items-center gap-1 mt-0.5">
                <span className="inline-block w-1.5 h-1.5 bg-green-500 rounded-full" />
                WA: {row.whatsApp}
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
          <span className="text-sm text-slate-600">
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
          <span className="text-sm text-slate-500">
            {new Date(row.createdAt).toLocaleDateString('pt-BR')}
          </span>
        ),
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
              className="p-1.5 hover:bg-slate-100 rounded-lg text-slate-500 hover:text-slate-700 transition-colors"
              title="Editar"
            >
              <Edit2 className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleWhatsApp(row)}
              disabled={!row.phone && !row.whatsApp}
              className="p-1.5 hover:bg-green-50 rounded-lg text-green-600 hover:text-green-700 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
              title="Enviar WhatsApp"
            >
              <MessageCircle className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleEmail(row)}
              className="p-1.5 hover:bg-blue-50 rounded-lg text-blue-600 hover:text-blue-700 transition-colors"
              title="Enviar Email"
            >
              <Mail className="h-4 w-4" />
            </button>
            <button
              onClick={() => setTimelineCustomer(row)}
              className="p-1.5 hover:bg-indigo-50 rounded-lg text-indigo-600 hover:text-indigo-700 transition-colors"
              title="Histórico"
            >
              <Activity className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleDelete(row.id)}
              className="p-1.5 hover:bg-red-50 rounded-lg text-red-500 hover:text-red-700 transition-colors"
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
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">
            Clientes
          </h1>
          <p className="text-slate-500">Gerencie sua base de clientes.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleExport}>
            <Download className="mr-2 h-4 w-4" /> Exportar
          </Button>
          <Button onClick={handleOpenCreate}>
            <Plus className="mr-2 h-4 w-4" /> Novo Cliente
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="space-y-3">
        <div className="flex gap-3 items-center">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Buscar por nome, email ou documento..."
              className="pl-10 h-10"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <Button
            variant="outline"
            onClick={() => setShowAdvancedFilters((v) => !v)}
            className={`shrink-0 ${advancedFilterCount > 0 ? 'border-slate-900 text-slate-900' : ''}`}
          >
            <Filter className="h-4 w-4 mr-1.5" />
            Filtros
            {advancedFilterCount > 0 && (
              <span className="ml-1.5 bg-slate-900 text-white text-xs rounded-full h-5 w-5 flex items-center justify-center">
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
              className="text-slate-500 hover:text-slate-700 shrink-0"
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
          <div className="bg-slate-50 border border-slate-200 rounded-lg p-4 space-y-4 animate-in slide-in-from-top-2 duration-200">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {/* Has Email */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Possui Email</label>
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
                <label className="text-sm font-medium text-slate-700">Possui WhatsApp</label>
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
                <label className="text-sm font-medium text-slate-700">Tipo Pessoa</label>
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
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-200">
              {/* Origem */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Origem</label>
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
                <label className="text-sm font-medium text-slate-700">Segmento</label>
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
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              {editingCustomer ? 'Editar Cliente' : 'Novo Cliente'}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Tipo de Pessoa */}
              <div className="sm:col-span-2">
                <Label className="mb-2 block">Tipo de Pessoa</Label>
                <div className="flex gap-4">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="radio"
                      value="0"
                      {...register('personType')}
                      className="text-slate-900 focus:ring-slate-900"
                    />
                    <span className="text-sm text-slate-700">Pessoa Física</span>
                  </label>
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="radio"
                      value="1"
                      {...register('personType')}
                      className="text-slate-900 focus:ring-slate-900"
                    />
                    <span className="text-sm text-slate-700">Pessoa Jurídica</span>
                  </label>
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="name">Nome Completo *</Label>
                <Input
                  id="name"
                  {...register('name')}
                  placeholder="Nome do cliente"
                />
                {errors.name && (
                  <span className="text-xs text-red-500">{errors.name.message}</span>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="email">Email *</Label>
                <Input
                  id="email"
                  type="email"
                  {...register('email')}
                  placeholder="email@exemplo.com"
                />
                {errors.email && (
                  <span className="text-xs text-red-500">{errors.email.message}</span>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="document">
                  {Number(personTypeWatch) === 1 ? 'CNPJ' : 'CPF'}
                </Label>
                <Input
                  id="document"
                  {...register('document')}
                  placeholder={
                    Number(personTypeWatch) === 1
                      ? '00.000.000/0000-00'
                      : '000.000.000-00'
                  }
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="companyName">Empresa</Label>
                <Input
                  id="companyName"
                  {...register('companyName')}
                  placeholder="Nome da empresa"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="phone">Telefone</Label>
                <Input
                  id="phone"
                  {...register('phone')}
                  placeholder="(00) 0000-0000"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="whatsApp">WhatsApp</Label>
                <Input
                  id="whatsApp"
                  {...register('whatsApp')}
                  placeholder="(00) 00000-0000"
                />
              </div>

              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="website">Website</Label>
                <Input
                  id="website"
                  {...register('website')}
                  placeholder="https://www.exemplo.com"
                />
              </div>

              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="tags">Tags (separadas por vírgula)</Label>
                <Input
                  id="tags"
                  {...register('tags')}
                  placeholder="vip, recorrente, indicação"
                />
              </div>

              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="notes">Observações</Label>
                <textarea
                  id="notes"
                  {...register('notes')}
                  className="flex min-h-[80px] w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-slate-400 focus:border-transparent"
                  placeholder="Observações gerais sobre o cliente..."
                />
              </div>
            </div>

            {formError && (
              <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">
                {formError}
              </div>
            )}

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsModalOpen(false)}
                disabled={submitting}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting && <Loader2 className="h-4 w-4 animate-spin mr-2" />}
                {editingCustomer ? 'Salvar' : 'Criar Cliente'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Email Composer */}
      <EmailCampaignComposerModal
        open={isComposerOpen}
        onOpenChange={setIsComposerOpen}
        recipients={composerRecipients}
        title="Composer Profissional - Clientes"
        onQueued={(count) => {
          alert(`Campanha enfileirada com sucesso para ${count} cliente(s).`);
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
        <SheetContent className="w-full sm:max-w-md overflow-y-auto">
          <SheetHeader className="mb-4">
            <SheetTitle>Histórico de Atividades</SheetTitle>
            {timelineCustomer && (
              <div className="flex items-center gap-3 mt-2">
                <Avatar name={timelineCustomer.name} size="sm" />
                <div>
                  <p className="text-sm font-medium text-slate-900">
                    {timelineCustomer.name}
                  </p>
                  <p className="text-xs text-slate-500">
                    {timelineCustomer.email || timelineCustomer.companyName || ''}
                  </p>
                </div>
              </div>
            )}
          </SheetHeader>
          {timelineCustomer && <LeadTimeline customerId={timelineCustomer.id} />}
        </SheetContent>
      </Sheet>
    </div>
  );
}
